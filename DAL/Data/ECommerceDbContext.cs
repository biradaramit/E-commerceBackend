using DAL.Entities;
using DAL.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL.Data;

public class ECommerceDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Product> Products { get; set; }

    public DbSet<Cart> Carts { get; set; }

    public DbSet<CartItem> CartItems { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- Category ----------------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);

            entity.Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(category => category.Description)
                .HasMaxLength(500);

            entity.HasIndex(category => category.Name)
                .IsUnique();
        });

        // ---------------- Product ----------------
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);

            entity.Property(product => product.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(product => product.Description)
                .HasMaxLength(1000);

            entity.Property(product => product.Price)
                .HasPrecision(18, 2);

            entity.Property(product => product.StockQuantity)
                .IsRequired();

            entity.Property(product => product.ImageUrl)
                .HasMaxLength(1000);

            // Keep SKU if it still exists in Product.cs.
            entity.Property(product => product.SKU)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(product => product.SKU)
                .IsUnique();

            // Keep CategoryId / Category navigation because you already
            // have Category entity and Category CRUD.
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Cart ----------------
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(cart => cart.Id);

            entity.Property(cart => cart.UserId)
                .IsRequired()
                .HasMaxLength(450);

            // One cart belongs to one user.
            entity.HasIndex(cart => cart.UserId)
                .IsUnique();
        });

        // ---------------- CartItem ----------------
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(cartItem => cartItem.Id);

            entity.Property(cartItem => cartItem.Quantity)
                .IsRequired();

            entity.HasOne(cartItem => cartItem.Cart)
                .WithMany(cart => cart.CartItems)
                .HasForeignKey(cartItem => cartItem.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cartItem => cartItem.Product)
                .WithMany(product => product.CartItems)
                .HasForeignKey(cartItem => cartItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Do not allow duplicate rows for the same product in one cart.
            entity.HasIndex(cartItem => new
            {
                cartItem.CartId,
                cartItem.ProductId
            }).IsUnique();
        });

        // ---------------- Order ----------------
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);

            entity.Property(order => order.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(order => order.OrderDate)
                .IsRequired();

            entity.Property(order => order.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(order => order.Status)
                .HasConversion<string>()
                .HasMaxLength(30);
        });

        // ---------------- OrderItem ----------------
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(orderItem => orderItem.Id);

            entity.Property(orderItem => orderItem.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(orderItem => orderItem.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(orderItem => orderItem.TotalPrice)
                .HasPrecision(18, 2);

            entity.HasOne(orderItem => orderItem.Order)
                .WithMany(order => order.OrderItems)
                .HasForeignKey(orderItem => orderItem.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(orderItem => orderItem.Product)
                .WithMany(product => product.OrderItems)
                .HasForeignKey(orderItem => orderItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}