namespace ECommerce.Review.API.Entities;

public class PurchaseHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid OrderId { get; set; }
    public DateTime PurchaseDate { get; set; }
}
