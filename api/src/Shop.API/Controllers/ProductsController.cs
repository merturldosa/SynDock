using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Products.Commands;
using Shop.Application.Products.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetProductsQuery(category, search, sort, page, pageSize));
        return Ok(new
        {
            items = result.Items,
            totalCount = result.TotalCount,
            page = result.PageNumber,
            pageSize = result.PageSize,
            totalPages = result.TotalPages,
            hasNext = result.HasNextPage,
            hasPrev = result.HasPreviousPage
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        if (result == null)
            return NotFound(new { error = "상품을 찾을 수 없습니다." });
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var result = await _mediator.Send(new CreateProductCommand(
            request.Name, request.Slug, request.Description,
            request.Price, request.SalePrice, request.PriceType,
            request.Specification, request.CategoryId,
            request.IsFeatured, request.IsNew, request.CustomFieldsJson,
            request.Images, request.Variants));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { productId = result.Data });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        var result = await _mediator.Send(new UpdateProductCommand(
            id, request.Name, request.Slug, request.Description,
            request.Price, request.SalePrice, request.PriceType,
            request.Specification, request.CategoryId,
            request.IsActive, request.IsFeatured, request.IsNew,
            request.CustomFieldsJson));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record CreateProductRequest(
    string Name, string? Slug, string? Description,
    decimal Price, decimal? SalePrice, string PriceType,
    string? Specification, int CategoryId,
    bool IsFeatured = false, bool IsNew = false,
    string? CustomFieldsJson = null,
    List<CreateProductImageDto>? Images = null,
    List<CreateProductVariantDto>? Variants = null);

public record UpdateProductRequest(
    string Name, string? Slug, string? Description,
    decimal Price, decimal? SalePrice, string PriceType,
    string? Specification, int CategoryId,
    bool IsActive = true, bool IsFeatured = false, bool IsNew = false,
    string? CustomFieldsJson = null);
