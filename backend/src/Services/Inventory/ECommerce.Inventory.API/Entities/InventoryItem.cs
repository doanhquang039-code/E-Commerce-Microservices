namespace ECommerce.Inventory.API.Entities;

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public int ReservedQuantity { get; set; } = 0;
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public int MinStockLevel { get; set; } = 5;
    public bool IsLowStock => AvailableQuantity <= MinStockLevel;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
