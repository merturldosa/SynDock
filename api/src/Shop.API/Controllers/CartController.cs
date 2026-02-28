using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Carts.Commands;
using Shop.Application.Carts.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await _mediator.Send(new GetCartQuery());
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        var result = await _mediator.Send(new AddToCartCommand(request.ProductId, request.VariantId, request.Quantity));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { cartId = result.Data });
    }

    [HttpPut("items/{id:int}")]
    public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemRequest request)
    {
        var result = await _mediator.Send(new UpdateCartItemCommand(id, request.Quantity));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> RemoveCartItem(int id)
    {
        var result = await _mediator.Send(new RemoveCartItemCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var result = await _mediator.Send(new ClearCartCommand());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record AddToCartRequest(int ProductId, int? VariantId, int Quantity = 1);
public record UpdateCartItemRequest(int Quantity);
