using BLL.DTOs.Categories;

namespace BLL.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        int id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
