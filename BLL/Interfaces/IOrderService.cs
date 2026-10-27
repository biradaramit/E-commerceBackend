using BLL.DTOs.Orders;

namespace BLL.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<List<OrderDto>> GetMyOrdersAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<OrderDto?> GetByIdAsync(
        int orderId,
        string userId,
        CancellationToken cancellationToken = default);
}