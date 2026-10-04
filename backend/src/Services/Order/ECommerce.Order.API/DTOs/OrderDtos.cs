namespace ECommerce.Order.API.DTOs;

public record AddToCartRequest(
    Guid ProductId,
    string ProductName,
    string? ProductThumbnail,
    decimal UnitPrice,
    int Quantity
);

public record CreateOrderRequest(
    string ShippingAddress,
    string? Notes
);

public record CancelOrderRequest(string Reason);

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductThumbnail,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public record CartDto(
    Guid Id,
    List<CartItemDto> Items,
    decimal SubTotal
);

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductThumbnail,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public record OrderDto(
    Guid Id,
    string Status,
    decimal TotalAmount,
    decimal ShippingFee,
    string ShippingAddress,
    string? Notes,
    string? CancelReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderItemDto> Items
);

public record OrderSummaryDto(
    Guid Id,
    string Status,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt
);
