using DAL.Enums;

namespace BLL.DTOs.Orders;

public class OrderDto
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}