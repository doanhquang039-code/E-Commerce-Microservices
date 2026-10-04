using ECommerce.Review.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.Review.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ReviewDbContext _db;
    public ReviewsController(ReviewDbContext db) => _db = db;

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Reviews.Where(r => r.ProductId == productId).OrderByDescending(r => r.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var avgRating = items.Any() ? items.Average(r => r.Rating) : 0;

        return Ok(new { success = true, data = new { items, total, avgRating, page, pageSize } });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "User";

        var existing = await _db.Reviews.AnyAsync(r => r.ProductId == dto.ProductId && r.UserId == userId);
        if (existing) return BadRequest(new { success = false, message = "You have already reviewed this product" });

        var review = new Entities.Review
        {
            ProductId = dto.ProductId, UserId = userId, UserName = userName,
            Rating = Math.Clamp(dto.Rating, 1, 5), Title = dto.Title, Content = dto.Content
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = review, message = "Review submitted" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        var review = await _db.Reviews.FindAsync(id);

        if (review == null) return NotFound();
        if (!isAdmin && review.UserId != userId) return Forbid();

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Review deleted" });
    }
}

public record CreateReviewDto(Guid ProductId, int Rating, string Title, string Content);
