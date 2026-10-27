using BLL.DTOs.Products;

namespace BLL.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(
        string? category,
        CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(
        CreateProductDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        int id,
        UpdateProductDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}