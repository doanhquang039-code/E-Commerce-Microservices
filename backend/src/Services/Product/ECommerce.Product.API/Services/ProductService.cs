using ECommerce.Product.API.Data;
using ECommerce.Product.API.DTOs;
using ECommerce.Product.API.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ECommerce.Product.API.Services;

public interface IProductService
{
    Task<(List<ProductSummaryDto> Items, int Total)> GetProductsAsync(ProductFilterRequest filter);
    Task<ProductDto?> GetProductByIdAsync(Guid id);
    Task<ProductDto?> GetProductBySlugAsync(string slug);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(Guid id);
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request);
}

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly IDatabase? _redis;
    private const string CachePrefix = "product:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public ProductService(ProductDbContext context, IConnectionMultiplexer? redis = null)
    {
        _context = context;
        _redis = redis?.GetDatabase();
    }

    public async Task<(List<ProductSummaryDto> Items, int Total)> GetProductsAsync(ProductFilterRequest filter)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Description.ToLower().Contains(search));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => (p.DiscountPrice ?? p.Price) <= filter.MaxPrice.Value);

        if (filter.IsFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == filter.IsFeatured.Value);

        query = filter.SortBy.ToLower() switch
        {
            "price" => filter.SortDesc ? query.OrderByDescending(p => p.DiscountPrice ?? p.Price) : query.OrderBy(p => p.DiscountPrice ?? p.Price),
            "rating" => filter.SortDesc ? query.OrderByDescending(p => p.Rating) : query.OrderBy(p => p.Rating),
            "sold" => filter.SortDesc ? query.OrderByDescending(p => p.SoldCount) : query.OrderBy(p => p.SoldCount),
            _ => filter.SortDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new ProductSummaryDto(
                p.Id, p.Name, p.Slug, p.Price, p.DiscountPrice,
                p.DiscountPrice ?? p.Price, p.ThumbnailUrl, p.Category.Name,
                p.Rating, p.ReviewCount, p.SoldCount, p.IsFeatured))
            .ToListAsync();

        return (items, total);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        if (_redis != null)
        {
            var cached = await _redis.StringGetAsync($"{CachePrefix}{id}");
            if (cached.HasValue)
                return JsonSerializer.Deserialize<ProductDto>(cached!);
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product == null) return null;

        var dto = MapToDto(product);

        if (_redis != null)
            await _redis.StringSetAsync($"{CachePrefix}{id}",
                JsonSerializer.Serialize(dto), CacheDuration);

        return dto;
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        var slug = GenerateSlug(request.Name);
        var existingSlug = await _context.Products.AnyAsync(p => p.Slug == slug);
        if (existingSlug) slug = $"{slug}-{Guid.NewGuid().ToString()[..8]}";

        var product = new Entities.Product
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            CategoryId = request.CategoryId,
            ThumbnailUrl = request.ThumbnailUrl,
            IsFeatured = request.IsFeatured,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return await GetProductByIdAsync(product.Id) ?? throw new Exception("Failed to create product");
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        product.Name = request.Name;
        product.Description = request.Description;
        product.ShortDescription = request.ShortDescription;
        product.Price = request.Price;
        product.DiscountPrice = request.DiscountPrice;
        product.CategoryId = request.CategoryId;
        product.ThumbnailUrl = request.ThumbnailUrl;
        product.IsFeatured = request.IsFeatured;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (_redis != null)
            await _redis.KeyDeleteAsync($"{CachePrefix}{id}");

        return await GetProductByIdAsync(id);
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        product.IsActive = false;
        await _context.SaveChangesAsync();

        if (_redis != null)
            await _redis.KeyDeleteAsync($"{CachePrefix}{id}");

        return true;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .Select(c => new CategoryDto(
                c.Id, c.Name, c.Description, c.ImageUrl, c.ParentId,
                c.Products.Count(p => p.IsActive)))
            .ToListAsync();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            ParentId = request.ParentId
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return new CategoryDto(category.Id, category.Name, category.Description,
            category.ImageUrl, category.ParentId, 0);
    }

    private static ProductDto MapToDto(Entities.Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.ShortDescription,
        p.Price, p.DiscountPrice, p.DiscountPrice ?? p.Price,
        p.ThumbnailUrl,
        new CategorySummaryDto(p.Category.Id, p.Category.Name),
        p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).ToList(),
        p.IsFeatured, p.SoldCount, p.Rating, p.ReviewCount, p.CreatedAt
    );

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLower().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        return slug;
    }
}
