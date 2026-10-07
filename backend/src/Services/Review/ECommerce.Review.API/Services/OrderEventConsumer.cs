using Confluent.Kafka;
using ECommerce.Review.API.Data;
using ECommerce.Review.API.Entities;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Events;
using System.Text.Json;

namespace ECommerce.Review.API.Services;

public class OrderEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderEventConsumer> _logger;

    public OrderEventConsumer(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<OrderEventConsumer> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "review-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(KafkaTopics.OrderCreated);

        _logger.LogInformation("Review Service listening to Kafka topics...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult?.Message != null)
                {
                    await ProcessMessageAsync(consumeResult.Message.Value);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Closing consumer...");
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(string messageValue)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(messageValue, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (@event != null && @event.Items != null)
            {
                _logger.LogInformation("Processing OrderCreatedEvent for Order: {OrderId}", @event.OrderId);
                
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();

                foreach (var item in @event.Items)
                {
                    var existing = dbContext.PurchaseHistories.FirstOrDefault(p => p.UserId == @event.UserId && p.ProductId == item.ProductId);
                    if (existing == null)
                    {
                        dbContext.PurchaseHistories.Add(new PurchaseHistory
                        {
                            UserId = @event.UserId,
                            ProductId = item.ProductId,
                            OrderId = @event.OrderId,
                            PurchaseDate = @event.CreatedAt
                        });
                    }
                }
                
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Kafka message");
        }
    }
}
