using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

/// <summary>
/// Mock 이체 서비스. 실제 은행 API 연동 전 개발/테스트용.
/// 이체 요청을 로그로 기록하고 성공을 반환합니다.
/// 프로덕션에서는 은행 Open Banking API 또는 PG사 이체 API로 교체합니다.
/// </summary>
public class MockTransferService : ITransferService
{
    private readonly ILogger<MockTransferService> _logger;

    public MockTransferService(ILogger<MockTransferService> logger)
    {
        _logger = logger;
    }

    public Task<TransferResult> RequestTransferAsync(TransferRequest request, CancellationToken ct = default)
    {
        // 필수 정보 검증
        if (string.IsNullOrEmpty(request.BankAccount))
        {
            _logger.LogWarning(
                "Transfer skipped for settlement {SettlementId}: No bank account configured",
                request.SettlementId);
            return Task.FromResult(new TransferResult(false, null, "계좌 정보가 설정되지 않았습니다."));
        }

        // Mock 트랜잭션 ID 생성
        var transactionId = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.SettlementId:D6}";

        _logger.LogInformation(
            "[MOCK TRANSFER] Settlement #{SettlementId} → {BankName} {BankAccount} ({BankHolder}), Amount: {Amount:N0}원, TxId: {TransactionId}",
            request.SettlementId,
            request.BankName,
            request.BankAccount,
            request.BankHolder,
            request.Amount,
            transactionId);

        return Task.FromResult(new TransferResult(true, transactionId, null));
    }
}
