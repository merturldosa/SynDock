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
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAddressesQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAddressRequest request)
    {
        var result = await _mediator.Send(new CreateAddressCommand(
            request.RecipientName, request.Phone, request.ZipCode,
            request.Address1, request.Address2, request.IsDefault));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { addressId = result.Data });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAddressRequest request)
    {
        var result = await _mediator.Send(new UpdateAddressCommand(
            id, request.RecipientName, request.Phone, request.ZipCode,
            request.Address1, request.Address2, request.IsDefault));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteAddressCommand(id));
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
