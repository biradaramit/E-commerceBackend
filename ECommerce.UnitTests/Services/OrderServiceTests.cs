using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using ECommerce.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UnitTests.Services;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderFromCartAsync_ShouldCreateOrder_ReduceStock_AndClearCart()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.CreateDbContext();

        const string userId = "test-customer-user-id";

        var category = new Category
        {
            Name = "Electronics"
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var product = new Product
        {
            Name = "Wireless Mouse",
            SKU = "MOUSE-TEST-001",
            Price = 999,
            CategoryId = category.Id,
            IsActive = true,
            StockQuantity = 10,
            ImageUrl = "https://example.com/mouse.jpg"
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var cart = new Cart
        {
            UserId = userId
        };

        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();

        var cartItem = new CartItem
        {
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = 2
        };

        dbContext.CartItems.Add(cartItem);
        await dbContext.SaveChangesAsync();

        var service = new OrderService(dbContext);

        // Act
        var result = await service.CreateOrderFromCartAsync(userId);

        // Assert: returned order
        Assert.NotEqual(0, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal(1998, result.TotalAmount);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Quantity);

        // Assert: Product stock is reduced: 10 - 2 = 8
        var updatedProduct = await dbContext.Products
            .FirstAsync(product => product.Id == product.Id);

        Assert.Equal(8, updatedProduct.StockQuantity);

        // Assert: CartItems are cleared after successful order.
        Assert.Equal(0, await dbContext.CartItems.CountAsync());

        // Assert: Order is stored.
        Assert.Equal(1, await dbContext.Orders.CountAsync());
        Assert.Equal(1, await dbContext.OrderItems.CountAsync());
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_ShouldThrow_WhenCartIsEmpty()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.CreateDbContext();

        const string userId = "customer-with-empty-cart";

        var cart = new Cart
        {
            UserId = userId
        };

        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();

        var service = new OrderService(dbContext);

        // Act and Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderFromCartAsync(userId));

        Assert.Contains("cart is empty", exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(0, await dbContext.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_ShouldThrow_WhenStockIsInsufficient()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.CreateDbContext();

        const string userId = "customer-insufficient-stock";

        var category = new Category
        {
            Name = "Books"
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var product = new Product
        {
            Name = "C# Book",
            SKU = "BOOK-TEST-001",
            Price = 500,
            CategoryId = category.Id,
            IsActive = true,
            StockQuantity = 2
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var cart = new Cart
        {
            UserId = userId
        };

        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();

        dbContext.CartItems.Add(new CartItem
        {
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = 5
        });

        await dbContext.SaveChangesAsync();

        var service = new OrderService(dbContext);

        // Act and Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderFromCartAsync(userId));

        Assert.Contains("Insufficient stock", exception.Message);

        // No order should be created.
        Assert.Equal(0, await dbContext.Orders.CountAsync());

        // Stock must not change.
        var unchangedProduct = await dbContext.Products
            .FirstAsync(item => item.Id == product.Id);

        Assert.Equal(2, unchangedProduct.StockQuantity);
    }
}