using BLL.DTOs.Cart;

namespace BLL.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<CartDto> AddItemAsync(
        string userId,
        AddCartItemDto dto,
        CancellationToken cancellationToken = default);

    Task<CartDto> UpdateItemAsync(
        string userId,
        int cartItemId,
        UpdateCartItemDto dto,
        CancellationToken cancellationToken = default);

    Task RemoveItemAsync(
        string userId,
        int cartItemId,
        CancellationToken cancellationToken = default);
}