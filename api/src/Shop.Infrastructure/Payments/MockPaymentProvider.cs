using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Payments;

public class MockPaymentProvider : IPaymentProvider
{
    public string ProviderName => "Mock";

    public Task<PaymentInitResult> InitiatePayment(PaymentInitRequest request, CancellationToken cancellationToken = default)
    {
        var paymentKey = $"mock_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentInitResult(
            IsSuccess: true,
            PaymentKey: paymentKey,
            CheckoutUrl: null,
            Error: null));
    }

    public Task<PaymentVerifyResult> VerifyPayment(string paymentKey, string orderId, decimal amount, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PaymentVerifyResult(
            IsSuccess: true,
            TransactionId: $"txn_{Guid.NewGuid():N}",
            PaidAt: DateTime.UtcNow,
            Error: null));
    }

    public Task<PaymentCancelResult> CancelPayment(string transactionId, string reason, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PaymentCancelResult(
            IsSuccess: true,
            Error: null));
    }
}
