namespace BLL.DTOs.Cart;

public class CartDto
{
    public int Id { get; set; }

    public List<CartItemDto> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }
}