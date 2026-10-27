using BLL.DTOs.Categories;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // GET: /api/categories
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(cancellationToken);

        return Ok(categories);
    }

    // GET: /api/categories/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return NotFound(new
            {
                Message = $"Category with Id {id} was not found."
            });
        }

        return Ok(category);
    }

    // POST: /api/categories
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var createdCategory = await _categoryService.CreateAsync(
                dto,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdCategory.Id },
                createdCategory);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { Message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { Message = exception.Message });
        }
    }

    // PUT: /api/categories/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _categoryService.UpdateAsync(
                id,
                dto,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new
                {
                    Message = $"Category with Id {id} was not found."
                });
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { Message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { Message = exception.Message });
        }
    }

    // DELETE: /api/categories/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _categoryService.DeleteAsync(
                id,
                cancellationToken);

            if (!deleted)
            {
                return NotFound(new
                {
                    Message = $"Category with Id {id} was not found."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { Message = exception.Message });
        }
    }
}