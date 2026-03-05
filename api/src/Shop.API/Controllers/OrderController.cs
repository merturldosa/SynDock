using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Commands;
using Shop.Application.Orders.Queries;
using Shop.Domain.Interfaces;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPdfService _pdfService;
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public OrderController(IMediator mediator, IPdfService pdfService, IShopDbContext db, ITenantContext tenantContext)
    {
        _mediator = mediator;
        _pdfService = pdfService;
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateOrderCommand(request.ShippingAddressId, request.Note, request.CouponCode, request.PointsToUse), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { orderId = result.Data!.OrderId, orderNumber = result.Data.OrderNumber });
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetOrdersQuery(status, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        if (result is null)
            return NotFound(new { error = "Order not found." });
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateOrderStatusCommand(id, request.Status, request.TrackingNumber, request.TrackingCarrier), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPut("{id:int}/shipping")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> UpdateShippingInfo(int id, [FromBody] UpdateShippingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateOrderStatusCommand(id, "Shipped", request.TrackingNumber, request.TrackingCarrier), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetOrderHistory(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrderHistoryQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("{id:int}/tracking")]
    public async Task<IActionResult> GetShippingTracking(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetShippingTrackingQuery(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("{id:int}/receipt")]
    public async Task<IActionResult> DownloadReceipt(int id, CancellationToken ct)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        if (order is null)
            return NotFound(new { error = "Order not found." });

        var tenantId = _tenantContext.TenantId;
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        var tenantName = tenant?.Name ?? "SynDock Shop";

        var pdf = _pdfService.GenerateOrderReceipt(order, tenantName);
        return File(pdf, "application/pdf", $"receipt-{order.OrderNumber}.pdf");
    }
}

public record CreateOrderRequest(int? ShippingAddressId, string? Note, string? CouponCode = null, decimal PointsToUse = 0);
public record UpdateOrderStatusRequest(string Status, string? TrackingNumber = null, string? TrackingCarrier = null);
public record UpdateShippingRequest(string TrackingNumber, string? TrackingCarrier = null);
