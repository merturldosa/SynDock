using System.Text.Json;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.Payments;

public class TenantAwarePaymentProvider : IPaymentProvider
{
    private readonly ITenantContext _tenantContext;
    private readonly TossPaymentProvider _tossProvider;
    private readonly MockPaymentProvider _mockProvider;

    public string ProviderName => GetProviderConfig().provider;

    public TenantAwarePaymentProvider(
        ITenantContext tenantContext,
        TossPaymentProvider tossProvider,
        MockPaymentProvider mockProvider)
    {
        _tenantContext = tenantContext;
        _tossProvider = tossProvider;
        _mockProvider = mockProvider;
    }

    public string? GetClientKey()
    {
        var config = GetProviderConfig();
        return config.clientKey;
    }

    public Task<PaymentInitResult> InitiatePayment(PaymentInitRequest request, CancellationToken cancellationToken = default)
    {
        return GetProvider().InitiatePayment(request, cancellationToken);
    }

    public Task<PaymentVerifyResult> VerifyPayment(string paymentKey, string orderId, decimal amount, CancellationToken cancellationToken = default)
    {
        return GetProvider().VerifyPayment(paymentKey, orderId, amount, cancellationToken);
    }

    public Task<PaymentCancelResult> CancelPayment(string paymentKey, string reason, CancellationToken cancellationToken = default)
    {
        return GetProvider().CancelPayment(paymentKey, reason, cancellationToken);
    }

    private IPaymentProvider GetProvider()
    {
        var config = GetProviderConfig();
        if (config.provider == "TossPayments" && !string.IsNullOrEmpty(config.secretKey))
        {
            _tossProvider.Configure(config.secretKey);
            return _tossProvider;
        }
        return _mockProvider;
    }

    private (string provider, string? clientKey, string? secretKey) GetProviderConfig()
    {
        var tenant = _tenantContext.Tenant;
        if (tenant?.ConfigJson is null)
            return ("Mock", null, null);

        try
        {
            using var doc = JsonDocument.Parse(tenant.ConfigJson);
            if (doc.RootElement.TryGetProperty("paymentConfig", out var paymentConfig))
            {
                var provider = paymentConfig.TryGetProperty("provider", out var p) ? p.GetString() ?? "Mock" : "Mock";
                var clientKey = paymentConfig.TryGetProperty("clientKey", out var ck) ? ck.GetString() : null;
                var secretKey = paymentConfig.TryGetProperty("secretKey", out var sk) ? sk.GetString() : null;
                return (provider, clientKey, secretKey);
            }
        }
        catch
        {
            // Invalid JSON, fallback to Mock
        }

        return ("Mock", null, null);
    }
}
