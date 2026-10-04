using Confluent.Kafka;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Events;
using System.Text.Json;

namespace ECommerce.Notification.API.Consumers;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<OrderCreatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = KafkaGroups.NotificationGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(kafkaConfig).Build();
        consumer.Subscribe(new[] {
            KafkaTopics.OrderCreated,
            KafkaTopics.OrderCancelled,
            KafkaTopics.OrderShipped
        });

        _logger.LogInformation("Notification consumer started, listening for events...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                _logger.LogInformation("Received event on topic: {Topic}", result.Topic);

                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                switch (result.Topic)
                {
                    case KafkaTopics.OrderCreated:
                        var orderCreated = JsonSerializer.Deserialize<OrderCreatedEvent>(result.Message.Value);
                        if (orderCreated != null)
                            await emailService.SendOrderConfirmationAsync(orderCreated);
                        break;

                    case KafkaTopics.OrderCancelled:
                        var orderCancelled = JsonSerializer.Deserialize<OrderCancelledEvent>(result.Message.Value);
                        if (orderCancelled != null)
                            await emailService.SendOrderCancellationAsync(orderCancelled);
                        break;

                    case KafkaTopics.OrderShipped:
                        var orderShipped = JsonSerializer.Deserialize<OrderShippedEvent>(result.Message.Value);
                        if (orderShipped != null)
                            await emailService.SendOrderShippedAsync(orderShipped);
                        break;
                }

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Notification consumer stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Kafka message");
            }
        }

        consumer.Close();
    }
}
