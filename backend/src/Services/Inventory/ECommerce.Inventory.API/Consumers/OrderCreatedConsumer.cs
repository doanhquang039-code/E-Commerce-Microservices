using Confluent.Kafka;
using ECommerce.Inventory.API.Data;
using ECommerce.Inventory.API.Entities;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Events;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.Inventory.API.Consumers;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(IServiceProvider sp, IConfiguration config, ILogger<OrderCreatedConsumer> logger)
    {
        _serviceProvider = sp;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = KafkaGroups.InventoryGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(new[] { KafkaTopics.OrderCreated, KafkaTopics.OrderCancelled });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                if (result.Topic == KafkaTopics.OrderCreated)
                {
                    var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(result.Message.Value);
                    if (orderEvent != null)
                    {
                        foreach (var item in orderEvent.Items)
                        {
                            var inventory = await db.InventoryItems
                                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId, stoppingToken);
                            if (inventory != null)
                            {
                                inventory.Quantity -= item.Quantity;
                                inventory.UpdatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Deducted {Qty} units of product {ProductId}",
                                    item.Quantity, item.ProductId);
                            }
                        }
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                else if (result.Topic == KafkaTopics.OrderCancelled)
                {
                    var cancelEvent = JsonSerializer.Deserialize<OrderCancelledEvent>(result.Message.Value);
                    if (cancelEvent != null)
                    {
                        foreach (var item in cancelEvent.Items ?? Enumerable.Empty<OrderItemEvent>())
                        {
                            var inventory = await db.InventoryItems
                                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId, stoppingToken);
                            if (inventory != null)
                            {
                                inventory.Quantity += item.Quantity;
                                inventory.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }

                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Inventory consumer error"); }
        }
        consumer.Close();
    }
}
