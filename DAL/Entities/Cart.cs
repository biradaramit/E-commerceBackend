namespace DAL.Entities;

public class Cart
{
    public int Id { get; set; }

    // This is the ASP.NET Identity user ID.
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}