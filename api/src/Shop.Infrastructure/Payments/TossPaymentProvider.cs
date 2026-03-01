using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Payments;

public class TossPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TossPaymentProvider> _logger;
    private const string BaseUrl = "https://api.tosspayments.com/v1/payments";

    public string ProviderName => "TossPayments";

    public TossPaymentProvider(HttpClient httpClient, ILogger<TossPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void Configure(string secretKey)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(secretKey + ":"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
    }

    public Task<PaymentInitResult> InitiatePayment(PaymentInitRequest request, CancellationToken cancellationToken = default)
    {
        // TossPayments uses client-side Widget SDK for initiation
        // Server only needs to return orderId for the frontend
        return Task.FromResult(new PaymentInitResult(
            IsSuccess: true,
            PaymentKey: null,
            CheckoutUrl: null,
            Error: null));
    }

    public async Task<PaymentVerifyResult> VerifyPayment(string paymentKey, string orderId, decimal amount, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new { paymentKey, orderId, amount = (long)amount };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/confirm", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                var transactionId = root.TryGetProperty("transactionKey", out var txKey) ? txKey.GetString() : paymentKey;
                var approvedAt = root.TryGetProperty("approvedAt", out var approved) ? DateTime.Parse(approved.GetString()!) : DateTime.UtcNow;

                return new PaymentVerifyResult(
                    IsSuccess: true,
                    TransactionId: transactionId,
                    PaidAt: approvedAt,
                    Error: null);
            }
            else
            {
                using var doc = JsonDocument.Parse(responseBody);
                var errorMessage = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() : "결제 승인 실패";
                _logger.LogWarning("TossPayments confirm failed: {Response}", responseBody);

                return new PaymentVerifyResult(
                    IsSuccess: false,
                    TransactionId: null,
                    PaidAt: null,
                    Error: errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TossPayments confirm exception");
            return new PaymentVerifyResult(
                IsSuccess: false,
                TransactionId: null,
                PaidAt: null,
                Error: $"결제 승인 중 오류: {ex.Message}");
        }
    }

    public async Task<PaymentCancelResult> CancelPayment(string paymentKey, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new { cancelReason = reason };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/{paymentKey}/cancel", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new PaymentCancelResult(IsSuccess: true, Error: null);
            }
            else
            {
                using var doc = JsonDocument.Parse(responseBody);
                var errorMessage = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() : "결제 취소 실패";
                _logger.LogWarning("TossPayments cancel failed: {Response}", responseBody);

                return new PaymentCancelResult(IsSuccess: false, Error: errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TossPayments cancel exception");
            return new PaymentCancelResult(IsSuccess: false, Error: $"결제 취소 중 오류: {ex.Message}");
        }
    }
}
