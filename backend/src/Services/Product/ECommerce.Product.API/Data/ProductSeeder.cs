using ECommerce.Product.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Product.API.Data;

public static class ProductSeeder
{
    public static async Task SeedAsync(ProductDbContext context)
    {
        if (await context.Categories.AnyAsync()) return; // Already seeded

        var categories = new List<Category>
        {
            new Category { Name = "Điện thoại", Description = "Smartphone chính hãng", IsActive = true },
            new Category { Name = "Laptop", Description = "Laptop học tập, văn phòng, gaming", IsActive = true },
            new Category { Name = "Máy tính bảng", Description = "Tablet iPad, Samsung", IsActive = true },
            new Category { Name = "Âm thanh", Description = "Tai nghe, loa bluetooth", IsActive = true }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        var phoneCategory = categories.First(c => c.Name == "Điện thoại");
        var laptopCategory = categories.First(c => c.Name == "Laptop");
        var audioCategory = categories.First(c => c.Name == "Âm thanh");

        var products = new List<Entities.Product>
        {
            new Entities.Product
            {
                Name = "iPhone 15 Pro Max 256GB",
                Slug = "iphone-15-pro-max-256gb",
                Description = "Siêu phẩm iPhone 15 Pro Max Titanium",
                ShortDescription = "iPhone 15 Pro Max",
                Price = 34990000,
                DiscountPrice = 29990000,
                ThumbnailUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-pro-max_3.png",
                CategoryId = phoneCategory.Id,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow
            },
            new Entities.Product
            {
                Name = "Samsung Galaxy S24 Ultra 5G 256GB",
                Slug = "samsung-galaxy-s24-ultra-256gb",
                Description = "Galaxy AI is here",
                ShortDescription = "S24 Ultra",
                Price = 33990000,
                DiscountPrice = 27990000,
                ThumbnailUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/s/ss-s24-ultra-xam-222.png",
                CategoryId = phoneCategory.Id,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow
            },
            new Entities.Product
            {
                Name = "MacBook Air M1 2020 8GB/256GB",
                Slug = "macbook-air-m1-2020",
                Description = "Laptop quốc dân cho học sinh sinh viên",
                ShortDescription = "MacBook Air M1",
                Price = 22990000,
                DiscountPrice = 18490000,
                ThumbnailUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/macbook-air-m1-2020-gray-600t-crop.png",
                CategoryId = laptopCategory.Id,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow
            },
            new Entities.Product
            {
                Name = "Tai nghe Bluetooth AirPods Pro 2",
                Slug = "tai-nghe-bluetooth-airpods-pro-2",
                Description = "Tai nghe chống ồn chủ động xuất sắc",
                ShortDescription = "AirPods Pro 2",
                Price = 6990000,
                DiscountPrice = 5990000,
                ThumbnailUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/a/p/airpods-pro-2-type-c.png",
                CategoryId = audioCategory.Id,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow
            },
            new Entities.Product
            {
                Name = "Laptop ASUS TUF Gaming F15",
                Slug = "laptop-asus-tuf-gaming-f15",
                Description = "Laptop gaming hiệu năng cao",
                ShortDescription = "ASUS TUF Gaming",
                Price = 25990000,
                DiscountPrice = 21990000,
                ThumbnailUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/l/a/laptop-asus-tuf-gaming-f15-fx506hf-hn014w.png",
                CategoryId = laptopCategory.Id,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
