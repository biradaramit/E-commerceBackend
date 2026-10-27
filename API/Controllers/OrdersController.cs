using System.Security.Claims;
using BLL.DTOs.Orders;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // POST: /api/orders
    // Creates an order using all items in the logged-in user's cart.
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(
        CancellationToken cancellationToken)
    {
        try
        {
            string? userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var order = await _orderService.CreateOrderFromCartAsync(
                userId,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = order.Id },
                order);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    // GET: /api/orders/my-orders
    [HttpGet("my-orders")]
    public async Task<ActionResult<List<OrderDto>>> GetMyOrders(
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var orders = await _orderService.GetMyOrdersAsync(
            userId,
            cancellationToken);

        return Ok(orders);
    }

    // GET: /api/orders/1
    // Customer can only see their own order.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var order = await _orderService.GetByIdAsync(
            id,
            userId,
            cancellationToken);

        if (order is null)
        {
            return NotFound(new
            {
                message = $"Order with Id {id} was not found."
            });
        }

        return Ok(order);
    }
}