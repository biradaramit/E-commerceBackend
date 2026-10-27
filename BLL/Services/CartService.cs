using BLL.DTOs.Cart;
using BLL.Interfaces;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class CartService : ICartService
{
    private readonly ECommerceDbContext _dbContext;

    public CartService(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CartDto> GetCartAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        // Reload cart with products and cart items for response.
        cart = await _dbContext.Carts
            .Include(item => item.CartItems)
                .ThenInclude(item => item.Product)
            .FirstAsync(item => item.Id == cart.Id, cancellationToken);

        return MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(
        string userId,
        AddCartItemDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.ProductId <= 0)
        {
            throw new ArgumentException("A valid product ID is required.");
        }

        if (dto.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                item => item.Id == dto.ProductId,
                cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException("Product was not found.");
        }

        if (!product.IsActive)
        {
            throw new InvalidOperationException(
                "This product is not available for purchase.");
        }

        if (product.StockQuantity <= 0)
        {
            throw new InvalidOperationException(
                "This product is out of stock.");
        }

        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        var existingItem = await _dbContext.CartItems
            .FirstOrDefaultAsync(
                item => item.CartId == cart.Id &&
                        item.ProductId == dto.ProductId,
                cancellationToken);

        if (existingItem is not null)
        {
            int newQuantity = existingItem.Quantity + dto.Quantity;

            if (newQuantity > product.StockQuantity)
            {
                throw new InvalidOperationException(
                    $"Only {product.StockQuantity} item(s) are available in stock.");
            }

            existingItem.Quantity = newQuantity;
        }
        else
        {
            if (dto.Quantity > product.StockQuantity)
            {
                throw new InvalidOperationException(
                    $"Only {product.StockQuantity} item(s) are available in stock.");
            }

            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = dto.Quantity
            };

            await _dbContext.CartItems.AddAsync(cartItem, cancellationToken);
        }

        cart.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCartAsync(userId, cancellationToken);
    }

    public async Task<CartDto> UpdateItemAsync(
        string userId,
        int cartItemId,
        UpdateCartItemDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero. Use DELETE to remove an item.");
        }

        var cartItem = await _dbContext.CartItems
            .Include(item => item.Cart)
            .Include(item => item.Product)
            .FirstOrDefaultAsync(
                item => item.Id == cartItemId,
                cancellationToken);

        if (cartItem is null)
        {
            throw new KeyNotFoundException("Cart item was not found.");
        }

        if (cartItem.Cart is null || cartItem.Cart.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to update this cart item.");
        }

        if (cartItem.Product is null ||
            !cartItem.Product.IsActive)
        {
            throw new InvalidOperationException(
                "This product is no longer available.");
        }

        if (dto.Quantity > cartItem.Product.StockQuantity)
        {
            throw new InvalidOperationException(
                $"Only {cartItem.Product.StockQuantity} item(s) are available in stock.");
        }

        cartItem.Quantity = dto.Quantity;
        cartItem.Cart.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCartAsync(userId, cancellationToken);
    }

    public async Task RemoveItemAsync(
        string userId,
        int cartItemId,
        CancellationToken cancellationToken = default)
    {
        var cartItem = await _dbContext.CartItems
            .Include(item => item.Cart)
            .FirstOrDefaultAsync(
                item => item.Id == cartItemId,
                cancellationToken);

        if (cartItem is null)
        {
            throw new KeyNotFoundException("Cart item was not found.");
        }

        if (cartItem.Cart is null || cartItem.Cart.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to remove this cart item.");
        }

        cartItem.Cart.UpdatedAt = DateTime.UtcNow;

        _dbContext.CartItems.Remove(cartItem);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Cart> GetOrCreateCartAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var cart = await _dbContext.Carts
            .FirstOrDefaultAsync(
                item => item.UserId == userId,
                cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Carts.AddAsync(cart, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return cart;
    }

    private static CartDto MapToDto(Cart cart)
    {
        var items = cart.CartItems
            .Where(item => item.Product is not null)
            .Select(item => new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product!.Name,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity,
                TotalPrice = item.Product.Price * item.Quantity,
                AvailableStockQuantity = item.Product.StockQuantity,
                ImageUrl = item.Product.ImageUrl
            })
            .ToList();

        return new CartDto
        {
            Id = cart.Id,
            Items = items,
            TotalAmount = items.Sum(item => item.TotalPrice)
        };
    }
}