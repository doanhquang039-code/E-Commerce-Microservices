using ECommerce.Shipping.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shipping.API.Data;

public class ShippingDbContext : DbContext
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options) { }
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.TrackingNumber).IsUnique();
            entity.HasMany(e => e.Trackings).WithOne(t => t.Shipment)
                  .HasForeignKey(t => t.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
