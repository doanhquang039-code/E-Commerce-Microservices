using ECommerce.Product.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Product.API.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Entities.Product> Products => Set<Entities.Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(300).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Images)
                  .WithOne(i => i.Product)
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Parent)
                  .WithMany(c => c.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed categories
        var electronicsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var clothingId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var booksId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = electronicsId, Name = "Electronics", Description = "Electronic devices and gadgets" },
            new Category { Id = clothingId, Name = "Clothing", Description = "Fashion and apparel" },
            new Category { Id = booksId, Name = "Books", Description = "Books and educational materials" }
        );
    }
}
