namespace ECommerce.Shared.Constants;

public static class KafkaTopics
{
    public const string OrderCreated = "order.created";
    public const string OrderCancelled = "order.cancelled";
    public const string OrderShipped = "order.shipped";
    public const string ProductUpdated = "product.updated";
}

public static class KafkaGroups
{
    public const string InventoryGroup = "inventory-service";
    public const string ShippingGroup = "shipping-service";
    public const string NotificationGroup = "notification-service";
}
