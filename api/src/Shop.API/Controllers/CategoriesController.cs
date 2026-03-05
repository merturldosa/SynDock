using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Categories.Commands;
using Shop.Application.Categories.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetCategoriesQuery());
        return Ok(result);
    }

    [HttpGet("slugs")]
    public async Task<IActionResult> GetSlugs()
    {
        var result = await _mediator.Send(new GetCategorySlugsQuery());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _mediator.Send(new CreateCategoryCommand(
            request.Name, request.Slug, request.Description,
            request.Icon, request.ParentId, request.SortOrder));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { categoryId = result.Data });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand(
            id, request.Name, request.Slug, request.Description,
            request.Icon, request.ParentId, request.SortOrder, request.IsActive));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record CreateCategoryRequest(
    string Name, string? Slug = null, string? Description = null,
    string? Icon = null, int? ParentId = null, int SortOrder = 0);

public record UpdateCategoryRequest(
    string Name, string? Slug = null, string? Description = null,
    string? Icon = null, int? ParentId = null, int SortOrder = 0, bool IsActive = true);
