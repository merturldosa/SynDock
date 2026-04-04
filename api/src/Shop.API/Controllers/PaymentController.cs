using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Payments.Commands;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using Shop.Infrastructure.Payments;

namespace Shop.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly TenantAwarePaymentProvider _tenantPaymentProvider;
    private readonly IShopDbContext _db;
    private readonly IConfiguration _config;
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IMediator mediator, TenantAwarePaymentProvider tenantPaymentProvider,
        IShopDbContext db, IConfiguration config, INotificationSender notificationSender,
        ILogger<PaymentController> logger)
    {
        _mediator = mediator;
        _tenantPaymentProvider = tenantPaymentProvider;
        _db = db;
        _config = config;
        _notificationSender = notificationSender;
        _logger = logger;
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmPaymentCommand(request.PaymentKey, request.OrderId, request.Amount), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { orderId = result.Data, success = true });
    }

    [HttpPost("{orderId:int}/refund")]
    public async Task<IActionResult> RefundPayment(int orderId, [FromBody] RefundPaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefundPaymentCommand(orderId, request.Reason), ct);
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

    /// <summary>TossPayments 비동기 웹훅 — 결제 상태 변경 알림</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            body = await reader.ReadToEndAsync(ct);

        // Verify webhook signature
        var webhookSecret = _config["TossPayments:WebhookSecret"] ?? "";
        if (!string.IsNullOrEmpty(webhookSecret))
        {
            var signature = Request.Headers["Toss-Signature"].FirstOrDefault() ?? "";
            var expectedSignature = ComputeHmacSha256(body, webhookSecret);
            if (!string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("TossPayments webhook signature mismatch");
                return Unauthorized(new { error = "Invalid signature" });
            }
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
            var data = root.TryGetProperty("data", out var d) ? d : default;

            _logger.LogInformation("TossPayments webhook: eventType={EventType}", eventType);

            switch (eventType)
            {
                case "PAYMENT_STATUS_CHANGED":
                    await HandlePaymentStatusChanged(data, ct);
                    break;
                case "PAYMENT_CANCELED":
                    await HandlePaymentCanceled(data, ct);
                    break;
                default:
                    _logger.LogInformation("Unhandled webhook eventType: {EventType}", eventType);
                    break;
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TossPayments webhook processing error");
            return Ok(new { success = true }); // Always return 200 to prevent retries
        }
    }

    private async Task HandlePaymentStatusChanged(JsonElement data, CancellationToken ct)
    {
        var paymentKey = data.TryGetProperty("paymentKey", out var pk) ? pk.GetString() : null;
        var status = data.TryGetProperty("status", out var st) ? st.GetString() : null;
        var orderId = data.TryGetProperty("orderId", out var oi) ? oi.GetString() : null;

        if (paymentKey is null || orderId is null) return;

        _logger.LogInformation("Payment status changed: paymentKey={PaymentKey}, status={Status}, orderId={OrderId}",
            paymentKey, status, orderId);

        // Find the order by orderNumber (TossPayments orderId = our orderNumber)
        var order = await _db.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderId, ct);

        if (order is null)
        {
            _logger.LogWarning("Order not found for webhook: orderId={OrderId}", orderId);
            return;
        }

        var payment = await _db.Payments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.OrderId == order.Id, ct);

        if (status == "DONE" && order.Status == nameof(OrderStatus.Pending))
        {
            // Payment confirmed via webhook (user didn't complete redirect)
            order.Status = nameof(OrderStatus.Confirmed);
            order.UpdatedBy = "webhook";
            order.UpdatedAt = DateTime.UtcNow;

            if (payment is not null)
            {
                payment.Status = nameof(PaymentStatus.Completed);
                payment.PaymentKey = paymentKey;
                payment.PaidAt = DateTime.UtcNow;
                payment.UpdatedBy = "webhook";
                payment.UpdatedAt = DateTime.UtcNow;
            }

            await _db.OrderHistories.AddAsync(new OrderHistory
            {
                OrderId = order.Id,
                Status = nameof(OrderStatus.Confirmed),
                Note = "결제 확인 (webhook)",
                CreatedBy = "webhook"
            }, ct);

            await _db.Notifications.AddAsync(new Notification
            {
                UserId = order.UserId,
                Type = nameof(NotificationType.Order),
                Title = "결제 완료",
                Message = $"주문 {order.OrderNumber}의 결제가 완료되었습니다.",
                ReferenceId = order.Id,
                ReferenceType = "Order",
                CreatedBy = "webhook"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await _notificationSender.SendToUser(order.UserId, new
            {
                type = "Order",
                title = "결제 완료",
                message = $"주문 {order.OrderNumber}의 결제가 완료되었습니다.",
                orderId = order.Id
            }, ct);

            _logger.LogInformation("Order {OrderNumber} confirmed via webhook", order.OrderNumber);
        }
    }

    private async Task HandlePaymentCanceled(JsonElement data, CancellationToken ct)
    {
        var paymentKey = data.TryGetProperty("paymentKey", out var pk) ? pk.GetString() : null;
        var orderId = data.TryGetProperty("orderId", out var oi) ? oi.GetString() : null;

        if (paymentKey is null || orderId is null) return;

        var order = await _db.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderId, ct);

        if (order is null) return;

        var payment = await _db.Payments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.OrderId == order.Id, ct);

        if (payment is not null)
        {
            payment.Status = nameof(PaymentStatus.Refunded);
            payment.UpdatedBy = "webhook";
            payment.UpdatedAt = DateTime.UtcNow;
        }

        if (order.Status != nameof(OrderStatus.Refunded) && order.Status != nameof(OrderStatus.Cancelled))
        {
            order.Status = nameof(OrderStatus.Cancelled);
            order.UpdatedBy = "webhook";
            order.UpdatedAt = DateTime.UtcNow;

            await _db.OrderHistories.AddAsync(new OrderHistory
            {
                OrderId = order.Id,
                Status = nameof(OrderStatus.Cancelled),
                Note = "결제 취소 (webhook)",
                CreatedBy = "webhook"
            }, ct);

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Order {OrderNumber} cancelled via webhook", order.OrderNumber);
        }
    }

    private static string ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}

public record ConfirmPaymentRequest(string PaymentKey, string OrderId, decimal Amount);
public record RefundPaymentRequest(string Reason);
