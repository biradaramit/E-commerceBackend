using BLL.DTOs.Orders;
using BLL.Interfaces;
using DAL.Data;
using DAL.Entities;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class OrderService : IOrderService
{
    private readonly ECommerceDbContext _dbContext;

    public OrderService(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Creates an order from the logged-in user's cart.
    public async Task<OrderDto> CreateOrderFromCartAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var cart = await _dbContext.Carts
            .Include(cart => cart.CartItems)
                .ThenInclude(cartItem => cartItem.Product)
            .FirstOrDefaultAsync(
                cart => cart.UserId == userId,
                cancellationToken);

        if (cart is null || !cart.CartItems.Any())
        {
            throw new InvalidOperationException(
                "Your cart is empty. Add products before placing an order.");
        }

        // Validate all cart products and stock before changing anything.
        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product is null)
            {
                throw new KeyNotFoundException(
                    $"Product with Id {cartItem.ProductId} was not found.");
            }

            if (!cartItem.Product.IsActive)
            {
                throw new InvalidOperationException(
                    $"Product '{cartItem.Product.Name}' is not available.");
            }

            if (cartItem.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid quantity for product '{cartItem.Product.Name}'.");
            }

            if (cartItem.Quantity > cartItem.Product.StockQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for '{cartItem.Product.Name}'. " +
                    $"Only {cartItem.Product.StockQuantity} item(s) are available.");
            }
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            decimal totalAmount = 0;

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            foreach (var cartItem in cart.CartItems)
            {
                var product = cartItem.Product!;

                decimal itemTotal = product.Price * cartItem.Quantity;

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = product.Id,

                    // Snapshot values: these remain correct even if
                    // product name/price changes after order placement.
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = cartItem.Quantity,
                    TotalPrice = itemTotal
                });

                totalAmount += itemTotal;

                // Reduce product stock after successful validation.
                product.StockQuantity -= cartItem.Quantity;
            }

            order.TotalAmount = totalAmount;

            await _dbContext.Orders.AddAsync(order, cancellationToken);

            // Clear cart after successful order placement.
            _dbContext.CartItems.RemoveRange(cart.CartItems);

            cart.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return MapToDto(order);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync(cancellationToken);

        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(
        int orderId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(
                order => order.Id == orderId &&
                         order.UserId == userId,
                cancellationToken);

        return order is null ? null : MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,

            Items = order.OrderItems.Select(item => new OrderItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
    }
}