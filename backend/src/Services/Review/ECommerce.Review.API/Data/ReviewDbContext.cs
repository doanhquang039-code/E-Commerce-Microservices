using ECommerce.Review.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Review.API.Data;

public class ReviewDbContext : DbContext
{
    public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options) { }

    public DbSet<Entities.Review> Reviews => Set<Entities.Review>();
    public DbSet<PurchaseHistory> PurchaseHistories => Set<PurchaseHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Review>(entity =>
        {
            entity.Property(e => e.Images)
                .HasConversion(
                    v => string.Join(";", v),
                    v => v.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.UserId, e.ProductId });
        });
    }
}
