namespace ECommerce.Shipping.API.Entities;

public enum ShipmentStatus
{
    Preparing = 0,
    PickedUp = 1,
    InTransit = 2,
    OutForDelivery = 3,
    Delivered = 4,
    Failed = 5,
    Returned = 6
}

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Preparing;
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ShipmentTracking> Trackings { get; set; } = new();
}

public class ShipmentTracking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
