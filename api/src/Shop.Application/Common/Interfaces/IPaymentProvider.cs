namespace Shop.Application.Common.Interfaces;

public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentInitResult> InitiatePayment(PaymentInitRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVerifyResult> VerifyPayment(string paymentKey, string orderId, decimal amount, CancellationToken cancellationToken = default);
    Task<PaymentCancelResult> CancelPayment(string transactionId, string reason, CancellationToken cancellationToken = default);
}

public record PaymentInitRequest(
    string OrderNumber,
    decimal Amount,
    string ProductName,
    string CustomerName,
    string CustomerEmail);

public record PaymentInitResult(
    bool IsSuccess,
    string? PaymentKey,
    string? CheckoutUrl,
    string? Error);

public record PaymentVerifyResult(
    bool IsSuccess,
    string? TransactionId,
    DateTime? PaidAt,
    string? Error);

public record PaymentCancelResult(
    bool IsSuccess,
    string? Error);
