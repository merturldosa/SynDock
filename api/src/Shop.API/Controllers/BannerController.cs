using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Banners.Commands;
using Shop.Application.Banners.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BannerController : ControllerBase
{
    private readonly IMediator _mediator;

    public BannerController(IMediator mediator) => _mediator = mediator;

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] string? page, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetActiveBannersQuery(page), ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAllBannersQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateBannerRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBannerCommand(
            request.Title, request.Description, request.ImageUrl, request.LinkUrl,
            request.DisplayType, request.PageTarget,
            request.StartDate, request.EndDate, request.SortOrder), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { bannerId = result.Data });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBannerRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateBannerCommand(
            id, request.Title, request.Description, request.ImageUrl, request.LinkUrl,
            request.DisplayType, request.PageTarget,
            request.StartDate, request.EndDate, request.SortOrder, request.IsActive), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteBannerCommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record CreateBannerRequest(
    string Title, string? Description, string? ImageUrl, string? LinkUrl,
    string DisplayType, string? PageTarget,
    DateTime? StartDate, DateTime? EndDate, int SortOrder = 0);

public record UpdateBannerRequest(
    string Title, string? Description, string? ImageUrl, string? LinkUrl,
    string DisplayType, string? PageTarget,
    DateTime? StartDate, DateTime? EndDate, int SortOrder, bool IsActive);
