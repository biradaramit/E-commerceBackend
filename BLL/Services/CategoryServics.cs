using BLL.DTOs.Categories;
using BLL.Interfaces;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class CategoryService : ICategoryService
{
    private readonly ECommerceDbContext _dbContext;

    public CategoryService(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Category name is required.");
        }

        var normalizedName = dto.Name.Trim();

        bool categoryAlreadyExists = await _dbContext.Categories
            .AnyAsync(
                category => category.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken);

        if (categoryAlreadyExists)
        {
            throw new InvalidOperationException("A category with this name already exists.");
        }

        var category = new Category
        {
            Name = normalizedName,
            Description = dto.Description?.Trim()
        };

        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Category name is required.");
        }

        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

        if (category is null)
        {
            return false;
        }

        var normalizedName = dto.Name.Trim();

        bool anotherCategoryHasThisName = await _dbContext.Categories
            .AnyAsync(
                item => item.Id != id &&
                        item.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken);

        if (anotherCategoryHasThisName)
        {
            throw new InvalidOperationException("A category with this name already exists.");
        }

        category.Name = normalizedName;
        category.Description = dto.Description?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .Include(category => category.Products)
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

        if (category is null)
        {
            return false;
        }

        // Do not allow deletion if products use this category.
        if (category.Products.Any())
        {
            throw new InvalidOperationException(
                "This category cannot be deleted because it contains products.");
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
