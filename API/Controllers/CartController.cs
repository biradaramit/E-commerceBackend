using System.Security.Claims;
using BLL.DTOs.Cart;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // GET: /api/cart
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart(
        CancellationToken cancellationToken)
    {
        string? userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var cart = await _cartService.GetCartAsync(
            userId,
            cancellationToken);

        return Ok(cart);
    }

    // POST: /api/cart/items
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddCartItemDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            string? userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var cart = await _cartService.AddItemAsync(
                userId,
                dto,
                cancellationToken);

            return Ok(cart);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    // PUT: /api/cart/items/{id}
    // id is CartItem Id, not Product Id.
    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(
        int id,
        [FromBody] UpdateCartItemDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            string? userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var cart = await _cartService.UpdateItemAsync(
                userId,
                id,
                dto,
                cancellationToken);

            return Ok(cart);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    // DELETE: /api/cart/items/{id}
    // id is CartItem Id, not Product Id.
    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> RemoveItem(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            string? userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            await _cartService.RemoveItemAsync(
                userId,
                id,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}