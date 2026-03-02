using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using Shop.Application.Products.Commands;
using Shop.Application.Products.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IImageGenerator _imageGenerator;

    public ProductsController(IMediator mediator, IImageGenerator imageGenerator)
    {
        _mediator = mediator;
        _imageGenerator = imageGenerator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] bool? isNew = null)
    {
        var result = await _mediator.Send(new GetProductsQuery(
            category, search, sort, page, pageSize,
            minPrice, maxPrice, minRating, isFeatured, isNew));
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

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string term)
    {
        var result = await _mediator.Send(new GetSearchSuggestionsQuery(term));
        return Ok(result);
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

    [HttpGet("{id:int}/variants")]
    public async Task<IActionResult> GetVariants(int id)
    {
        var result = await _mediator.Send(new GetProductVariantsQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:int}/variants")]
    [Authorize]
    public async Task<IActionResult> UpdateVariants(int id, [FromBody] UpdateProductVariantsRequest request)
    {
        var variants = request.Variants.Select(v =>
            new VariantDto(v.Id, v.Name, v.Sku, v.Price, v.Stock, v.SortOrder, v.IsActive)).ToList();
        var result = await _mediator.Send(new UpdateProductVariantsCommand(id, variants));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPost("{id:int}/generate-content")]
    [Authorize]
    public async Task<IActionResult> GenerateContent(int id)
    {
        var result = await _mediator.Send(new GenerateProductContentCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("{id:int}/generate-image")]
    [Authorize]
    public async Task<IActionResult> GenerateImage(int id, [FromBody] GenerateImageRequest request)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id));
        if (product == null)
            return NotFound(new { error = "상품을 찾을 수 없습니다." });

        var prompt = string.IsNullOrWhiteSpace(request.Prompt)
            ? $"Professional product photography of {product.Name}. Clean white background, studio lighting, e-commerce style, high quality."
            : request.Prompt;

        var result = await _imageGenerator.GenerateAsync(prompt, request.Size ?? "1024x1024");

        if (string.IsNullOrEmpty(result.Url))
            return BadRequest(new { error = result.RevisedPrompt ?? "이미지 생성에 실패했습니다." });

        return Ok(new { url = result.Url, revisedPrompt = result.RevisedPrompt });
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

public record UpdateVariantRequestItem(int? Id, string Name, string? Sku, decimal? Price, int Stock, int SortOrder, bool IsActive);
public record UpdateProductVariantsRequest(List<UpdateVariantRequestItem> Variants);
public record GenerateImageRequest(string? Prompt = null, string? Size = "1024x1024");
