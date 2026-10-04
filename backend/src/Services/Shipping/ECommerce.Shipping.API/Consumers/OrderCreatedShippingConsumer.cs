using Confluent.Kafka;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Events;
using ECommerce.Shipping.API.Data;
using ECommerce.Shipping.API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.Shipping.API.Consumers;

public class OrderCreatedShippingConsumer : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderCreatedShippingConsumer> _logger;

    public OrderCreatedShippingConsumer(IServiceProvider sp, IConfiguration config, ILogger<OrderCreatedShippingConsumer> logger)
    {
        _sp = sp; _config = config; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = KafkaGroups.ShippingGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(KafkaTopics.OrderCreated);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(result.Message.Value);
                if (orderEvent != null)
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();

                    var shipment = new Shipment
                    {
                        OrderId = orderEvent.OrderId,
                        TrackingNumber = GenerateTrackingNumber(),
                        CarrierName = "ECommerce Express",
                        Status = ShipmentStatus.Preparing,
                        ShippingAddress = orderEvent.ShippingAddress,
                        EstimatedDelivery = DateTime.UtcNow.AddDays(3)
                    };

                    db.Shipments.Add(shipment);
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Shipment created for order {OrderId}", orderEvent.OrderId);
                }
                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Shipping consumer error"); }
        }
        consumer.Close();
    }

    private static string GenerateTrackingNumber() =>
        $"EC{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";
}
