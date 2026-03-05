using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Addresses.Commands;
using Shop.Application.Addresses.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IMediator _mediator;

    public AddressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAddressesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAddressRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAddressCommand(
            request.RecipientName, request.Phone, request.ZipCode,
            request.Address1, request.Address2, request.IsDefault), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { addressId = result.Data });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAddressRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAddressCommand(
            id, request.RecipientName, request.Phone, request.ZipCode,
            request.Address1, request.Address2, request.IsDefault), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAddressCommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record CreateAddressRequest(
    string RecipientName, string Phone, string ZipCode,
    string Address1, string? Address2 = null, bool IsDefault = false);

public record UpdateAddressRequest(
    string RecipientName, string Phone, string ZipCode,
    string Address1, string? Address2 = null, bool IsDefault = false);
