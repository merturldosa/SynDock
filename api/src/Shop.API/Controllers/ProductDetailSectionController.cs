using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.ProductDetailSections.Commands;
using Shop.Application.ProductDetailSections.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/products/{productId:int}/sections")]
public class ProductDetailSectionController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductDetailSectionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetSections(int productId)
    {
        var result = await _mediator.Send(new GetProductDetailSectionsQuery(productId));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(int productId, [FromBody] CreateSectionRequest request)
    {
        var result = await _mediator.Send(new CreateProductDetailSectionCommand(
            productId, request.Title, request.Content, request.ImageUrl,
            request.ImageAltText, request.SectionType, request.SortOrder));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { sectionId = result.Data });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int productId, int id, [FromBody] UpdateSectionRequest request)
    {
        var result = await _mediator.Send(new UpdateProductDetailSectionCommand(
            id, request.Title, request.Content, request.ImageUrl,
            request.ImageAltText, request.SectionType, request.SortOrder, request.IsActive));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int productId, int id)
    {
        var result = await _mediator.Send(new DeleteProductDetailSectionCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPut("reorder")]
    [Authorize]
    public async Task<IActionResult> Reorder(int productId, [FromBody] ReorderRequest request)
    {
        var result = await _mediator.Send(new ReorderProductDetailSectionsCommand(productId, request.SectionIds));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record CreateSectionRequest(string Title, string? Content, string? ImageUrl, string? ImageAltText, string SectionType, int SortOrder);
public record UpdateSectionRequest(string? Title, string? Content, string? ImageUrl, string? ImageAltText, string? SectionType, int? SortOrder, bool? IsActive);
public record ReorderRequest(List<int> SectionIds);
