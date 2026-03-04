namespace Shop.Application.Common.Interfaces;

/// <summary>
/// 정산금 이체 서비스 인터페이스.
/// 실제 은행 API 연동 시 이 인터페이스를 구현합니다.
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// 정산금 이체 요청
    /// </summary>
    Task<TransferResult> RequestTransferAsync(TransferRequest request, CancellationToken ct = default);
}

public record TransferRequest(
    int SettlementId,
    int TenantId,
    string? BankName,
    string? BankAccount,
    string? BankHolder,
    decimal Amount,
    string Description);

public record TransferResult(
    bool IsSuccess,
    string? TransactionId,
    string? Error);
