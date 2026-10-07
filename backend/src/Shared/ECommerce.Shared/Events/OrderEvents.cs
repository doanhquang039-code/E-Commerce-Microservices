namespace ECommerce.Shared.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    Guid UserId,
    string UserEmail,
    List<OrderItemEvent> Items,
    decimal TotalAmount,
    string ShippingAddress,
    DateTime CreatedAt
);

public record OrderCancelledEvent(
    Guid OrderId,
    Guid UserId,
    string UserEmail,
    string Reason,
    List<OrderItemEvent> Items,
    DateTime CancelledAt
);

public record OrderShippedEvent(
    Guid OrderId,
    Guid UserId,
    string UserEmail,
    string TrackingNumber,
    string CarrierName,
    DateTime ShippedAt
);

public record OrderItemEvent(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
