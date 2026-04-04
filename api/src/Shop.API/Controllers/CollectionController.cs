using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Collections.Commands;
using Shop.Application.Collections.Queries;

namespace Shop.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CollectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCollections(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyCollectionsQuery(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCollectionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCollectionCommand(request.Name, request.Description, request.IsPublic), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { collectionId = result.Data });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCollectionDetailQuery(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCollectionCommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> AddItem(int id, [FromBody] AddItemRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddToCollectionCommand(id, request.ProductId, request.Note), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}/items/{productId:int}")]
    public async Task<IActionResult> RemoveItem(int id, int productId, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemoveFromCollectionCommand(id, productId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record CreateCollectionRequest(string Name, string? Description, bool IsPublic = false);
public record AddItemRequest(int ProductId, string? Note);
