using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Products.Commands;
using Shop.Application.Products.Queries;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IImageGenerator _imageGenerator;
    private readonly IShopDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public ProductsController(IMediator mediator, IImageGenerator imageGenerator, IShopDbContext db, IFileStorageService fileStorage)
    {
        _mediator = mediator;
        _imageGenerator = imageGenerator;
        _db = db;
        _fileStorage = fileStorage;
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
        [FromQuery] bool? isNew = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(
            category, search, sort, page, pageSize,
            minPrice, maxPrice, minRating, isFeatured, isNew), ct);
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

    [HttpGet("slugs")]
    public async Task<IActionResult> GetSlugs(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductSlugsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string term, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSearchSuggestionsQuery(term), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (result == null)
            return NotFound(new { error = "Product not found." });
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateProductCommand(
            request.Name, request.Slug, request.Description,
            request.Price, request.SalePrice, request.PriceType,
            request.Specification, request.CategoryId,
            request.IsFeatured, request.IsNew, request.CustomFieldsJson,
            request.Images, request.Variants), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { productId = result.Data });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProductCommand(
            id, request.Name, request.Slug, request.Description,
            request.Price, request.SalePrice, request.PriceType,
            request.Specification, request.CategoryId,
            request.IsActive, request.IsFeatured, request.IsNew,
            request.CustomFieldsJson), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("{id:int}/variants")]
    public async Task<IActionResult> GetVariants(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductVariantsQuery(id), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}/variants")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> UpdateVariants(int id, [FromBody] UpdateProductVariantsRequest request, CancellationToken ct)
    {
        var variants = request.Variants.Select(v =>
            new VariantDto(v.Id, v.Name, v.Sku, v.Price, v.Stock, v.SortOrder, v.IsActive)).ToList();
        var result = await _mediator.Send(new UpdateProductVariantsCommand(id, variants), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPost("{id:int}/generate-content")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> GenerateContent(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateProductContentCommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("{id:int}/generate-image")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> GenerateImage(int id, [FromBody] GenerateImageRequest request, CancellationToken ct)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (product == null)
            return NotFound(new { error = "Product not found." });

        var prompt = string.IsNullOrWhiteSpace(request.Prompt)
            ? $"Professional product photography of {product.Name}. Clean white background, studio lighting, e-commerce style, high quality."
            : request.Prompt;

        var result = await _imageGenerator.GenerateAsync(prompt, request.Size ?? "1024x1024");

        if (string.IsNullOrEmpty(result.Url))
            return BadRequest(new { error = result.RevisedPrompt ?? "Image generation failed." });

        return Ok(new { url = result.Url, revisedPrompt = result.RevisedPrompt });
    }

    /// <summary>상품 이미지 삭제</summary>
    [HttpDelete("{productId:int}/images/{imageId:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> DeleteImage(int productId, int imageId, CancellationToken ct)
    {
        var image = await _db.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct);

        if (image is null)
            return NotFound(new { error = "Image not found." });

        // Delete file from storage
        if (!string.IsNullOrEmpty(image.Url))
            await _fileStorage.DeleteAsync(image.Url, ct);

        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync(ct);

        return Ok(new { success = true });
    }

    /// <summary>상품 이미지 추가</summary>
    [HttpPost("{productId:int}/images")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> AddImage(int productId, [FromBody] AddProductImageRequest request, CancellationToken ct)
    {
        var product = await _db.Products.FindAsync(new object[] { productId }, ct);
        if (product is null)
            return NotFound(new { error = "Product not found." });

        var existingCount = await _db.ProductImages.CountAsync(i => i.ProductId == productId, ct);

        var image = new Domain.Entities.ProductImage
        {
            ProductId = productId,
            Url = request.Url,
            AltText = request.AltText,
            SortOrder = request.SortOrder ?? existingCount,
            IsPrimary = request.IsPrimary ?? (existingCount == 0),
            CreatedBy = User.Identity?.Name ?? "system"
        };

        await _db.ProductImages.AddAsync(image, ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new { imageId = image.Id });
    }

    /// <summary>상세 섹션용 AI 이미지 생성</summary>
    [HttpPost("{productId:int}/sections/generate-image")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> GenerateSectionImage(int productId, [FromBody] GenerateSectionImageRequest request, CancellationToken ct)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(productId), ct);
        if (product is null)
            return NotFound(new { error = "Product not found." });

        var prompt = string.IsNullOrWhiteSpace(request.Prompt)
            ? $"Beautiful lifestyle photography showcasing {product.Name}. Natural setting, warm lighting, appetizing food photography style, Korean traditional aesthetic."
            : request.Prompt;

        if (!string.IsNullOrWhiteSpace(request.SectionType))
        {
            prompt = request.SectionType switch
            {
                "Hero" => $"Hero banner image for {product.Name}. Wide format, dramatic lighting, premium feel, Korean traditional food aesthetic.",
                "Feature" => $"Feature highlight of {product.Name}. Close-up detail shot, showing texture and quality, appetizing warm tones.",
                "Closing" => $"Elegant closing image for {product.Name}. Gift-wrapped presentation, premium packaging, ready to serve.",
                _ => prompt
            };
        }

        var result = await _imageGenerator.GenerateAsync(prompt, request.Size ?? "1024x1024");

        if (string.IsNullOrEmpty(result.Url))
            return BadRequest(new { error = result.RevisedPrompt ?? "Image generation failed." });

        return Ok(new { url = result.Url, revisedPrompt = result.RevisedPrompt });
    }

    [HttpGet("export")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportProductsCommand(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return File(result.Data!, "text/csv", $"products_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("import")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "CSV file is required" });

        using var reader = new StreamReader(file.OpenReadStream());
        var csvContent = await reader.ReadToEndAsync();

        var result = await _mediator.Send(new ImportProductsCommand(csvContent), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }
}

public class CreateProductRequest
{
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public string PriceType { get; set; } = "Fixed";
    public string? Specification { get; set; }
    public int CategoryId { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public string? CustomFieldsJson { get; set; }
    public List<CreateProductImageDto>? Images { get; set; }
    public List<CreateProductVariantDto>? Variants { get; set; }
}

public record UpdateProductRequest(
    string Name, string? Slug, string? Description,
    decimal Price, decimal? SalePrice, string PriceType,
    string? Specification, int CategoryId,
    bool IsActive = true, bool IsFeatured = false, bool IsNew = false,
    string? CustomFieldsJson = null);

public record UpdateVariantRequestItem(int? Id, string Name, string? Sku, decimal? Price, int Stock, int SortOrder, bool IsActive);
public record UpdateProductVariantsRequest(List<UpdateVariantRequestItem> Variants);
public record GenerateImageRequest(string? Prompt = null, string? Size = "1024x1024");
public record AddProductImageRequest(string Url, string? AltText = null, int? SortOrder = null, bool? IsPrimary = null);
public record GenerateSectionImageRequest(string? Prompt = null, string? SectionType = null, string? Size = "1024x1024");
