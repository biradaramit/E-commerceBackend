namespace BLL.DTOs.Products;

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string SKU { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
}