using ECommerce.Inventory.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.API.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(e => e.ProductId).IsUnique();
        });
    }
}
