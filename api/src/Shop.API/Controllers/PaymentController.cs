using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Payments.Commands;
using Shop.Infrastructure.Payments;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly TenantAwarePaymentProvider _tenantPaymentProvider;

    public PaymentController(IMediator mediator, TenantAwarePaymentProvider tenantPaymentProvider)
    {
        _mediator = mediator;
        _tenantPaymentProvider = tenantPaymentProvider;
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        var result = await _mediator.Send(new ConfirmPaymentCommand(request.PaymentKey, request.OrderId, request.Amount));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { orderId = result.Data, success = true });
    }

    [HttpPost("{orderId:int}/refund")]
    public async Task<IActionResult> RefundPayment(int orderId, [FromBody] RefundPaymentRequest request)
    {
        var result = await _mediator.Send(new RefundPaymentCommand(orderId, request.Reason));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("client-key")]
    public IActionResult GetClientKey()
    {
        var clientKey = _tenantPaymentProvider.GetClientKey();
        var providerName = _tenantPaymentProvider.ProviderName;
        return Ok(new { clientKey, provider = providerName });
    }
}

public record ConfirmPaymentRequest(string PaymentKey, string OrderId, decimal Amount);
public record RefundPaymentRequest(string Reason);
