using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class ScmController : ControllerBase
{
    private readonly IScmService _scm;
    private readonly ICurrentUserService _currentUser;

    public ScmController(IScmService scm, ICurrentUserService currentUser)
    {
        _scm = scm;
        _currentUser = currentUser;
    }

    // === Suppliers ===

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers([FromQuery] string? status, CancellationToken ct)
        => Ok(await _scm.GetSuppliersAsync(0, status, ct));

    [HttpGet("suppliers/{supplierId}")]
    public async Task<IActionResult> GetSupplier(int supplierId, CancellationToken ct)
    {
        var supplier = await _scm.GetSupplierAsync(0, supplierId, ct);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest req, CancellationToken ct)
        => Ok(await _scm.CreateSupplierAsync(0, req.Name, req.Code, req.ContactName, req.Email, req.Phone, req.Address, req.BusinessNumber, req.LeadTimeDays, _currentUser.Username ?? "system", ct));

    [HttpPut("suppliers/{supplierId}")]
    public async Task<IActionResult> UpdateSupplier(int supplierId, [FromBody] UpdateSupplierRequest req, CancellationToken ct)
    {
        await _scm.UpdateSupplierAsync(0, supplierId, req.Status, req.Grade, req.Notes, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Supplier updated" });
    }

    // === Procurement Orders ===

    [HttpGet("procurement-orders")]
    public async Task<IActionResult> GetProcurementOrders([FromQuery] string? status, [FromQuery] int? supplierId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var (items, totalCount) = await _scm.GetProcurementOrdersAsync(0, status, supplierId, page, pageSize, ct);
        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpGet("procurement-orders/{orderId}")]
    public async Task<IActionResult> GetProcurementOrder(int orderId, CancellationToken ct)
    {
        var order = await _scm.GetProcurementOrderAsync(0, orderId, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("procurement-orders")]
    public async Task<IActionResult> CreateProcurementOrder([FromBody] CreateProcurementOrderRequest req, CancellationToken ct)
    {
        var items = req.Items.Select(i => (i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();
        var order = await _scm.CreateProcurementOrderAsync(0, req.SupplierId, items, req.ExpectedDeliveryDate, req.Notes, _currentUser.Username ?? "system", ct);
        return Ok(order);
    }

    [HttpPost("procurement-orders/{orderId}/submit")]
    public async Task<IActionResult> SubmitProcurementOrder(int orderId, CancellationToken ct)
    {
        await _scm.SubmitProcurementOrderAsync(0, orderId, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Procurement order submitted" });
    }

    [HttpPost("procurement-orders/{orderId}/confirm")]
    public async Task<IActionResult> ConfirmProcurementOrder(int orderId, CancellationToken ct)
    {
        await _scm.ConfirmProcurementOrderAsync(0, orderId, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Procurement order confirmed" });
    }

    [HttpPost("procurement-orders/{orderId}/ship")]
    public async Task<IActionResult> MarkShipped(int orderId, [FromBody] MarkShippedRequest req, CancellationToken ct)
    {
        await _scm.MarkShippedAsync(0, orderId, req.TrackingNumber, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Procurement order marked as shipped" });
    }

    [HttpPost("procurement-orders/{orderId}/deliver")]
    public async Task<IActionResult> MarkDelivered(int orderId, CancellationToken ct)
    {
        await _scm.MarkDeliveredAsync(0, orderId, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Procurement order delivered, supplier stats updated" });
    }

    // === Supplier Evaluation ===

    [HttpPost("evaluations")]
    public async Task<IActionResult> EvaluateSupplier([FromBody] EvaluateSupplierRequest req, CancellationToken ct)
        => Ok(await _scm.EvaluateSupplierAsync(0, req.SupplierId, req.Period, req.QualityScore, req.DeliveryScore, req.PriceScore, req.ServiceScore, req.Comments, _currentUser.Username ?? "system", ct));

    [HttpGet("evaluations")]
    public async Task<IActionResult> GetEvaluations([FromQuery] int? supplierId, CancellationToken ct)
        => Ok(await _scm.GetEvaluationsAsync(0, supplierId, ct));

    // === Analytics ===

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Ok(await _scm.GetScmDashboardAsync(0, ct));

    [HttpGet("lead-time-analysis")]
    public async Task<IActionResult> GetLeadTimeAnalysis(CancellationToken ct)
        => Ok(await _scm.GetLeadTimeAnalysisAsync(0, ct));
}

// === Request DTOs ===

public record CreateSupplierRequest(
    string Name,
    string Code,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Address,
    string? BusinessNumber,
    int LeadTimeDays = 7
);

public record UpdateSupplierRequest(
    string? Status,
    string? Grade,
    string? Notes
);

public record CreateProcurementOrderRequest(
    int SupplierId,
    List<ProcurementOrderItemRequest> Items,
    DateTime? ExpectedDeliveryDate,
    string? Notes
);

public record ProcurementOrderItemRequest(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record MarkShippedRequest(
    string? TrackingNumber
);

public record EvaluateSupplierRequest(
    int SupplierId,
    string Period,
    int QualityScore,
    int DeliveryScore,
    int PriceScore,
    int ServiceScore,
    string? Comments
);
