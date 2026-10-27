using BLL.DTOs.Products;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    // GET: /api/products
    // GET: /api/products?search=phone
    // GET: /api/products?categoryId=1
    [HttpGet]
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProductDto>>> GetAll(
    [FromQuery] string? category,
    CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(
            category,
            cancellationToken);

        return Ok(products);
    }

    // GET: /api/products/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return NotFound(new
            {
                Message = $"Product with Id {id} was not found."
            });
        }

        return Ok(product);
    }

    // POST: /api/products
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.CreateAsync(
                dto,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                product);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { Message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { Message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { Message = exception.Message });
        }
    }

    // PUT: /api/products/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            bool updated = await _productService.UpdateAsync(
                id,
                dto,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new
                {
                    Message = $"Product with Id {id} was not found."
                });
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { Message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { Message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { Message = exception.Message });
        }
    }

    // DELETE: /api/products/1
    // This performs a soft delete by setting IsActive = false.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        bool deleted = await _productService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                Message = $"Product with Id {id} was not found."
            });
        }

        return NoContent();
    }
}