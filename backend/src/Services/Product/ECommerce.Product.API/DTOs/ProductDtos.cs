namespace ECommerce.Product.API.DTOs;

// Request DTOs
public record CreateProductRequest(
    string Name,
    string Description,
    string ShortDescription,
    decimal Price,
    decimal? DiscountPrice,
    Guid CategoryId,
    string? ThumbnailUrl,
    bool IsFeatured = false
);

public record UpdateProductRequest(
    string Name,
    string Description,
    string ShortDescription,
    decimal Price,
    decimal? DiscountPrice,
    Guid CategoryId,
    string? ThumbnailUrl,
    bool IsFeatured = false,
    bool IsActive = true
);

public record CreateCategoryRequest(
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ParentId
);

public record ProductFilterRequest
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? IsFeatured { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public bool SortDesc { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}

// Response DTOs
public record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string ShortDescription,
    decimal Price,
    decimal? DiscountPrice,
    decimal FinalPrice,
    string? ThumbnailUrl,
    CategorySummaryDto Category,
    List<string> ImageUrls,
    bool IsFeatured,
    int SoldCount,
    double Rating,
    int ReviewCount,
    DateTime CreatedAt
);

public record ProductSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    decimal? DiscountPrice,
    decimal FinalPrice,
    string? ThumbnailUrl,
    string CategoryName,
    double Rating,
    int ReviewCount,
    int SoldCount,
    bool IsFeatured
);

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ParentId,
    int ProductCount
);

public record CategorySummaryDto(
    Guid Id,
    string Name
);
