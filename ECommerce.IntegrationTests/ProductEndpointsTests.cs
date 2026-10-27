using System.Net;
using System.Net.Http.Json;
using DAL.Data;
using DAL.Entities;
using ECommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests;

public class ProductEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ShouldReturnCreatedProduct()
    {
        // Arrange: insert test data directly into the TEST database.
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ECommerceDbContext>();

            var category = new Category
            {
                Name = $"Test Category {Guid.NewGuid()}"
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            var product = new Product
            {
                Name = "Integration Test Mouse",
                Description = "Created by integration test",
                SKU = $"TEST-MOUSE-{Guid.NewGuid()}",
                Price = 999,
                CategoryId = category.Id,
                IsActive = true,
                StockQuantity = 10,
                ImageUrl = "https://example.com/test-mouse.jpg"
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            Assert.Equal(1, await dbContext.Products.CountAsync());
        }

        // Act
        var response = await _client.GetAsync("/api/Products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Integration Test Mouse", content);
    }

    [Fact]
    public async Task GetProductById_ShouldReturn404_WhenProductDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/Products/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}