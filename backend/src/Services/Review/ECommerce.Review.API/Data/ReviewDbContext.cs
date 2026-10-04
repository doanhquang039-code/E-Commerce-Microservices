using ECommerce.Review.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Review.API.Data;

public class ReviewDbContext : DbContext
{
    public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options) { }

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
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
