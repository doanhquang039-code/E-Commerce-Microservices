using ECommerce.Product.API.DTOs;
using ECommerce.Product.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Product.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) =>
        _productService = productService;

    /// <summary>Lấy danh sách sản phẩm với filter, sort, paging</summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterRequest filter)
    {
        var (items, total) = await _productService.GetProductsAsync(filter);
        return Ok(new
        {
            success = true,
            data = new
            {
                items,
                totalCount = total,
                pageNumber = filter.PageNumber,
                pageSize = filter.PageSize,
                totalPages = (int)Math.Ceiling(total / (double)filter.PageSize)
            }
        });
    }

    /// <summary>Lấy chi tiết sản phẩm theo ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound(new { success = false, message = "Product not found" });
        return Ok(new { success = true, data = product });
    }

    /// <summary>Lấy chi tiết sản phẩm theo Slug</summary>
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetProductBySlug(string slug)
    {
        var product = await _productService.GetProductBySlugAsync(slug);
        if (product == null) return NotFound(new { success = false, message = "Product not found" });
        return Ok(new { success = true, data = product });
    }

    /// <summary>Tạo sản phẩm mới (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateProductAsync(request);
        return CreatedAtAction(nameof(GetProductById), new { id = product.Id },
            new { success = true, data = product, message = "Product created" });
    }

    /// <summary>Cập nhật sản phẩm (Admin only)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _productService.UpdateProductAsync(id, request);
        if (product == null) return NotFound(new { success = false, message = "Product not found" });
        return Ok(new { success = true, data = product, message = "Product updated" });
    }

    /// <summary>Xóa sản phẩm (Admin only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await _productService.DeleteProductAsync(id);
        if (!result) return NotFound(new { success = false, message = "Product not found" });
        return Ok(new { success = true, message = "Product deleted" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IProductService _productService;

    public CategoriesController(IProductService productService) =>
        _productService = productService;

    /// <summary>Lấy tất cả danh mục</summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _productService.GetCategoriesAsync();
        return Ok(new { success = true, data = categories });
    }

    /// <summary>Tạo danh mục mới (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var category = await _productService.CreateCategoryAsync(request);
        return Ok(new { success = true, data = category, message = "Category created" });
    }
}
