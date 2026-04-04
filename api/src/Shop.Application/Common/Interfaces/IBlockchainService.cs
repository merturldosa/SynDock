using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IBlockchainService
{
    // Transaction Proof
    Task<BlockchainTransaction> RecordProofAsync(int tenantId, string referenceType, int referenceId, string dataToHash, string createdBy, CancellationToken ct = default);
    Task<bool> VerifyProofAsync(int transactionId, CancellationToken ct = default);
    Task<List<BlockchainTransaction>> GetProofsAsync(int tenantId, string? referenceType = null, int page = 1, int pageSize = 50, CancellationToken ct = default);

    // Token Wallet
    Task<TokenWallet> GetOrCreateWalletAsync(int tenantId, int userId, CancellationToken ct = default);
    Task<TokenTransaction> EarnTokensAsync(int tenantId, int userId, decimal amount, string transactionType, string description, string? referenceType, int? referenceId, string createdBy, CancellationToken ct = default);
    Task<TokenTransaction> SpendTokensAsync(int tenantId, int userId, decimal amount, string description, string? referenceType, int? referenceId, string createdBy, CancellationToken ct = default);
    Task<TokenTransaction> TransferTokensAsync(int tenantId, int fromUserId, int toUserId, decimal amount, string description, string createdBy, CancellationToken ct = default);
    Task<List<TokenTransaction>> GetTokenHistoryAsync(int tenantId, int userId, int page = 1, int pageSize = 50, CancellationToken ct = default);

    // Dashboard
    Task<object> GetChainDashboardAsync(int tenantId, CancellationToken ct = default);
}
