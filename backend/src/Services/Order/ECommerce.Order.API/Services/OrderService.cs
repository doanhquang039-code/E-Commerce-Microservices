using Confluent.Kafka;
using ECommerce.Order.API.Data;
using ECommerce.Order.API.DTOs;
using ECommerce.Order.API.Entities;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Events;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.Order.API.Services;

public interface IOrderService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddToCartAsync(Guid userId, AddToCartRequest request);
    Task<CartDto> UpdateCartItemAsync(Guid userId, Guid itemId, int quantity);
    Task<bool> RemoveFromCartAsync(Guid userId, Guid itemId);
    Task<bool> ClearCartAsync(Guid userId);
    Task<OrderDto> CreateOrderAsync(Guid userId, string userEmail, CreateOrderRequest request);
    Task<(List<OrderSummaryDto> Items, int Total)> GetOrdersAsync(Guid userId, int pageNumber, int pageSize);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, Guid userId);
    Task<bool> CancelOrderAsync(Guid orderId, Guid userId, string reason);
}

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly IProducer<string, string> _producer;

    public OrderService(OrderDbContext context, IProducer<string, string> producer)
    {
        _context = context;
        _producer = producer;
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return MapCartToDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartRequest request)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = request.ProductId,
                ProductName = request.ProductName,
                ProductThumbnail = request.ProductThumbnail,
                UnitPrice = request.UnitPrice,
                Quantity = request.Quantity,
                CartId = cart.Id
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapCartToDto(cart);
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid userId, Guid itemId, int quantity)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Cart item not found");

        if (quantity <= 0)
            cart.Items.Remove(item);
        else
            item.Quantity = quantity;

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapCartToDto(cart);
    }

    public async Task<bool> RemoveFromCartAsync(Guid userId, Guid itemId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return false;

        cart.Items.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ClearCartAsync(Guid userId)
    {
        var cart = await _context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) return false;

        cart.Items.Clear();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<OrderDto> CreateOrderAsync(Guid userId, string userEmail, CreateOrderRequest request)
    {
        var cart = await GetOrCreateCartAsync(userId);
        if (!cart.Items.Any()) throw new InvalidOperationException("Cart is empty");

        var order = new Entities.Order
        {
            UserId = userId,
            UserEmail = userEmail,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes,
            ShippingFee = 30000, // 30k VND shipping
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductThumbnail = i.ProductThumbnail,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.TotalPrice) + order.ShippingFee;

        _context.Orders.Add(order);
        cart.Items.Clear();
        await _context.SaveChangesAsync();

        // Publish Kafka event
        var orderEvent = new OrderCreatedEvent(
            order.Id, userId, userEmail,
            order.Items.Select(i => new OrderItemEvent(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            order.TotalAmount, order.ShippingAddress, order.CreatedAt);

        await _producer.ProduceAsync(KafkaTopics.OrderCreated,
            new Message<string, string>
            {
                Key = order.Id.ToString(),
                Value = JsonSerializer.Serialize(orderEvent)
            });

        return MapOrderToDto(order);
    }

    public async Task<(List<OrderSummaryDto> Items, int Total)> GetOrdersAsync(
        Guid userId, int pageNumber, int pageSize)
    {
        var query = _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.Id, o.Status.ToString(), o.TotalAmount,
                o.Items.Count, o.CreatedAt))
            .ToListAsync();
        return (items, total);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, Guid userId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        return order == null ? null : MapOrderToDto(order);
    }

    public async Task<bool> CancelOrderAsync(Guid orderId, Guid userId, string reason)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) return false;
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Cannot cancel order at this stage");

        order.Status = OrderStatus.Cancelled;
        order.CancelReason = reason;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var cancelEvent = new OrderCancelledEvent(orderId, userId, order.UserEmail, reason, DateTime.UtcNow);
        await _producer.ProduceAsync(KafkaTopics.OrderCancelled,
            new Message<string, string>
            {
                Key = orderId.ToString(),
                Value = JsonSerializer.Serialize(cancelEvent)
            });

        return true;
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid userId)
    {
        var cart = await _context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null) return cart;

        cart = new Cart { UserId = userId };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    private static CartDto MapCartToDto(Cart cart) => new(
        cart.Id,
        cart.Items.Select(i => new CartItemDto(
            i.Id, i.ProductId, i.ProductName, i.ProductThumbnail,
            i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity)).ToList(),
        cart.Items.Sum(i => i.UnitPrice * i.Quantity)
    );

    private static OrderDto MapOrderToDto(Entities.Order o) => new(
        o.Id, o.Status.ToString(), o.TotalAmount, o.ShippingFee,
        o.ShippingAddress, o.Notes, o.CancelReason, o.CreatedAt, o.UpdatedAt,
        o.Items.Select(i => new OrderItemDto(
            i.Id, i.ProductId, i.ProductName, i.ProductThumbnail,
            i.UnitPrice, i.Quantity, i.TotalPrice)).ToList()
    );
}
