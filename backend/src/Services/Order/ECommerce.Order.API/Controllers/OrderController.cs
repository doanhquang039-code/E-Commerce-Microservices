using ECommerce.Order.API.DTOs;
using ECommerce.Order.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Order.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IOrderService _orderService;

    public CartController(IOrderService orderService) => _orderService = orderService;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await _orderService.GetCartAsync(GetUserId());
        return Ok(new { success = true, data = cart });
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        var cart = await _orderService.AddToCartAsync(GetUserId(), request);
        return Ok(new { success = true, data = cart, message = "Item added to cart" });
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateCartItem(Guid itemId, [FromBody] int quantity)
    {
        var cart = await _orderService.UpdateCartItemAsync(GetUserId(), itemId, quantity);
        return Ok(new { success = true, data = cart });
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveFromCart(Guid itemId)
    {
        await _orderService.RemoveFromCartAsync(GetUserId(), itemId);
        return Ok(new { success = true, message = "Item removed from cart" });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _orderService.ClearCartAsync(GetUserId());
        return Ok(new { success = true, message = "Cart cleared" });
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserEmail() => User.FindFirstValue(ClaimTypes.Email)!;

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(GetUserId(), GetUserEmail(), request);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id },
                new { success = true, data = order, message = "Order created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var (items, total) = await _orderService.GetOrdersAsync(GetUserId(), pageNumber, pageSize);
        return Ok(new { success = true, data = new { items, totalCount = total, pageNumber, pageSize } });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderByIdAsync(id, GetUserId());
        if (order == null) return NotFound(new { success = false, message = "Order not found" });
        return Ok(new { success = true, data = order });
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var result = await _orderService.CancelOrderAsync(id, GetUserId(), request.Reason);
            if (!result) return NotFound(new { success = false, message = "Order not found" });
            return Ok(new { success = true, message = "Order cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
