using BLL.DTOs.Products;
using BLL.Interfaces;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BLL.Services;

public class ProductService : IProductService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public ProductService(
        ECommerceDbContext dbContext,
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    // Public catalog:
    // Returns only active products that have stock available.
    public async Task<List<ProductDto>> GetAllAsync(
        string? category,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = GetProductListCacheKey(category);

        if (_cache.TryGetValue(cacheKey, out List<ProductDto>? cachedProducts) &&
            cachedProducts is not null)
        {
            return cachedProducts;
        }

        var query = _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
           .Where(product => product.IsActive)
            .AsQueryable();

        // GET /api/products?category=Electronics
        if (!string.IsNullOrWhiteSpace(category))
        {
            string categoryName = category.Trim().ToLower();

            query = query.Where(product =>
                product.Category != null &&
                product.Category.Name.ToLower() == categoryName);
        }

        var products = await query
            .OrderBy(product => product.Name)
            .Select(product => new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,

                // SKU is extra; it is okay to keep it.
                SKU = product.SKU,

                CategoryId = product.CategoryId,
                CategoryName = product.Category != null
                    ? product.Category.Name
                    : string.Empty,

                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,

                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt
            })
            .ToListAsync(cancellationToken);

        _cache.Set(
            cacheKey,
            products,
            TimeSpan.FromMinutes(5));

        return products;
    }

    public async Task<ProductDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = GetProductCacheKey(id);

        if (_cache.TryGetValue(cacheKey, out ProductDto? cachedProduct) &&
            cachedProduct is not null)
        {
            return cachedProduct;
        }

        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product =>
                product.Id == id &&
                product.IsActive &&
                product.StockQuantity > 0)
            .Select(product => new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                SKU = product.SKU,

                CategoryId = product.CategoryId,
                CategoryName = product.Category != null
                    ? product.Category.Name
                    : string.Empty,

                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,

                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is not null)
        {
            _cache.Set(
                cacheKey,
                product,
                TimeSpan.FromMinutes(5));
        }

        return product;
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateProductValues(
            dto.Name,
            dto.Price,
            dto.SKU,
            dto.StockQuantity);

        string normalizedSku = dto.SKU.Trim().ToUpperInvariant();

        bool categoryExists = await _dbContext.Categories
            .AnyAsync(
                category => category.Id == dto.CategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            throw new KeyNotFoundException(
                "The selected category does not exist.");
        }

        bool skuAlreadyExists = await _dbContext.Products
            .AnyAsync(
                product => product.SKU == normalizedSku,
                cancellationToken);

        if (skuAlreadyExists)
        {
            throw new InvalidOperationException(
                "A product with this SKU already exists.");
        }

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            SKU = normalizedSku,

            CategoryId = dto.CategoryId,

            StockQuantity = dto.StockQuantity,
            ImageUrl = dto.ImageUrl?.Trim(),

            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        RemoveProductCache(product.Id);

        string categoryName = await _dbContext.Categories
            .Where(category => category.Id == product.CategoryId)
            .Select(category => category.Name)
            .FirstAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            SKU = product.SKU,

            CategoryId = product.CategoryId,
            CategoryName = categoryName,

            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,

            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt
        };
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateProductDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateProductValues(
            dto.Name,
            dto.Price,
            dto.SKU,
            dto.StockQuantity);

        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return false;
        }

        string normalizedSku = dto.SKU.Trim().ToUpperInvariant();

        bool categoryExists = await _dbContext.Categories
            .AnyAsync(
                category => category.Id == dto.CategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            throw new KeyNotFoundException(
                "The selected category does not exist.");
        }

        bool anotherProductHasSku = await _dbContext.Products
            .AnyAsync(
                item => item.Id != id &&
                        item.SKU == normalizedSku,
                cancellationToken);

        if (anotherProductHasSku)
        {
            throw new InvalidOperationException(
                "Another product already uses this SKU.");
        }

        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.Price = dto.Price;
        product.SKU = normalizedSku;

        product.CategoryId = dto.CategoryId;

        product.StockQuantity = dto.StockQuantity;
        product.ImageUrl = dto.ImageUrl?.Trim();

        product.IsActive = dto.IsActive;

        product.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        RemoveProductCache(product.Id);

        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return false;
        }

        // Soft delete:
        // Existing OrderItems can still retain product history.
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        RemoveProductCache(product.Id);

        return true;
    }

    private static void ValidateProductValues(
        string name,
        decimal price,
        string sku,
        int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Product name cannot exceed 200 characters.");
        }

        if (price <= 0)
        {
            throw new ArgumentException(
                "Product price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("Product SKU is required.");
        }

        if (sku.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Product SKU cannot exceed 100 characters.");
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentException(
                "Stock quantity cannot be negative.");
        }
    }

    private void RemoveProductCache(int productId)
    {
        _cache.Remove(GetProductCacheKey(productId));

        // Removes the standard unfiltered catalog cache.
        _cache.Remove(GetProductListCacheKey(null));
    }

    private static string GetProductCacheKey(int productId)
    {
        return $"product:{productId}";
    }

    private static string GetProductListCacheKey(string? category)
    {
        string normalizedCategory = string.IsNullOrWhiteSpace(category)
            ? "all"
            : category.Trim().ToLowerInvariant();

        return $"products:list:category:{normalizedCategory}";
    }
}