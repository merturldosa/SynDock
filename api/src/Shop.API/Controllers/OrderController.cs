using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Orders.Commands;
using Shop.Application.Orders.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _mediator.Send(new CreateOrderCommand(request.ShippingAddressId, request.Note, request.CouponCode, request.PointsToUse));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { orderId = result.Data!.OrderId, orderNumber = result.Data.OrderNumber });
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetOrdersQuery(status, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        if (result is null)
            return NotFound(new { error = "주문을 찾을 수 없습니다." });
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderStatusCommand(id, request.Status, request.TrackingNumber, request.TrackingCarrier));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPut("{id:int}/shipping")]
    public async Task<IActionResult> UpdateShippingInfo(int id, [FromBody] UpdateShippingRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderStatusCommand(id, "Shipped", request.TrackingNumber, request.TrackingCarrier));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetOrderHistory(int id)
    {
        var result = await _mediator.Send(new GetOrderHistoryQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("{id:int}/tracking")]
    public async Task<IActionResult> GetShippingTracking(int id)
    {
        var result = await _mediator.Send(new GetShippingTrackingQuery(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }
}

public record CreateOrderRequest(int? ShippingAddressId, string? Note, string? CouponCode = null, decimal PointsToUse = 0);
public record UpdateOrderStatusRequest(string Status, string? TrackingNumber = null, string? TrackingCarrier = null);
public record UpdateShippingRequest(string TrackingNumber, string? TrackingCarrier = null);
