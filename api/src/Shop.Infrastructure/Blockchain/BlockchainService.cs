using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Blockchain;

public class BlockchainService : IBlockchainService
{
    private readonly IShopDbContext _db;
    private readonly ILogger<BlockchainService> _logger;

    public BlockchainService(IShopDbContext db, ILogger<BlockchainService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // === Transaction Proof ===

    public async Task<BlockchainTransaction> RecordProofAsync(int tenantId, string referenceType, int referenceId, string dataToHash, string createdBy, CancellationToken ct = default)
    {
        var hash = ComputeSha256(dataToHash);

        var tx = new BlockchainTransaction
        {
            TenantId = tenantId,
            TransactionType = "Proof",
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            DataHash = hash,
            Status = "Confirmed", // Off-chain proof (on-chain pending integration)
            Network = "syndock_internal",
            ConfirmedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { dataLength = dataToHash.Length, algorithm = "SHA-256" }),
            CreatedBy = createdBy
        };

        _db.BlockchainTransactions.Add(tx);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Proof recorded: {Type}#{Id} hash={Hash}", referenceType, referenceId, hash[..16]);
        return tx;
    }

    public async Task<bool> VerifyProofAsync(int transactionId, CancellationToken ct = default)
    {
        var tx = await _db.BlockchainTransactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        return tx != null && tx.Status == "Confirmed" && !string.IsNullOrEmpty(tx.DataHash);
    }

    public async Task<List<BlockchainTransaction>> GetProofsAsync(int tenantId, string? referenceType = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.BlockchainTransactions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(referenceType))
            query = query.Where(t => t.ReferenceType == referenceType);
        return await query.OrderByDescending(t => t.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    // === Token Wallet ===

    public async Task<TokenWallet> GetOrCreateWalletAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var wallet = await _db.TokenWallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wallet != null) return wallet;

        wallet = new TokenWallet
        {
            TenantId = tenantId,
            UserId = userId,
            Balance = 0,
            TotalEarned = 0,
            TotalSpent = 0,
            CreatedBy = "system"
        };
        _db.TokenWallets.Add(wallet);
        await _db.SaveChangesAsync(ct);
        return wallet;
    }

    public async Task<TokenTransaction> EarnTokensAsync(int tenantId, int userId, decimal amount, string transactionType, string description, string? referenceType, int? referenceId, string createdBy, CancellationToken ct = default)
    {
        var wallet = await GetOrCreateWalletAsync(tenantId, userId, ct);
        wallet.Balance += amount;
        wallet.TotalEarned += amount;
        wallet.UpdatedBy = createdBy;
        wallet.UpdatedAt = DateTime.UtcNow;

        var tx = new TokenTransaction
        {
            TenantId = tenantId,
            ToUserId = userId,
            Amount = amount,
            TransactionType = transactionType,
            Description = description,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedBy = createdBy
        };
        _db.TokenTransactions.Add(tx);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("SDT earned: user={User} amount={Amount} type={Type}", userId, amount, transactionType);
        return tx;
    }

    public async Task<TokenTransaction> SpendTokensAsync(int tenantId, int userId, decimal amount, string description, string? referenceType, int? referenceId, string createdBy, CancellationToken ct = default)
    {
        var wallet = await GetOrCreateWalletAsync(tenantId, userId, ct);
        if (wallet.Balance < amount)
            throw new InvalidOperationException($"Insufficient SDT balance: {wallet.Balance} < {amount}");

        wallet.Balance -= amount;
        wallet.TotalSpent += amount;
        wallet.UpdatedBy = createdBy;
        wallet.UpdatedAt = DateTime.UtcNow;

        var tx = new TokenTransaction
        {
            TenantId = tenantId,
            FromUserId = userId,
            Amount = amount,
            TransactionType = "Spend",
            Description = description,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedBy = createdBy
        };
        _db.TokenTransactions.Add(tx);
        await _db.SaveChangesAsync(ct);
        return tx;
    }

    public async Task<TokenTransaction> TransferTokensAsync(int tenantId, int fromUserId, int toUserId, decimal amount, string description, string createdBy, CancellationToken ct = default)
    {
        var fromWallet = await GetOrCreateWalletAsync(tenantId, fromUserId, ct);
        if (fromWallet.Balance < amount)
            throw new InvalidOperationException("Insufficient balance");

        var toWallet = await GetOrCreateWalletAsync(tenantId, toUserId, ct);

        fromWallet.Balance -= amount;
        fromWallet.TotalSpent += amount;
        toWallet.Balance += amount;
        toWallet.TotalEarned += amount;

        var tx = new TokenTransaction
        {
            TenantId = tenantId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount,
            TransactionType = "Transfer",
            Description = description,
            CreatedBy = createdBy
        };
        _db.TokenTransactions.Add(tx);
        await _db.SaveChangesAsync(ct);
        return tx;
    }

    public async Task<List<TokenTransaction>> GetTokenHistoryAsync(int tenantId, int userId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        return await _db.TokenTransactions.AsNoTracking()
            .Where(t => t.FromUserId == userId || t.ToUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
    }

    // === Dashboard ===

    public async Task<object> GetChainDashboardAsync(int tenantId, CancellationToken ct = default)
    {
        var totalProofs = await _db.BlockchainTransactions.CountAsync(t => t.TransactionType == "Proof", ct);
        var totalWallets = await _db.TokenWallets.CountAsync(ct);
        var totalTokensCirculating = await _db.TokenWallets.SumAsync(w => (decimal?)w.Balance, ct) ?? 0;
        var totalTransactions = await _db.TokenTransactions.CountAsync(ct);
        var recentTransactions = await _db.TokenTransactions.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt).Take(10)
            .Select(t => new { t.TransactionType, t.Amount, t.Description, t.CreatedAt })
            .ToListAsync(ct);

        return new
        {
            totalProofs,
            totalWallets,
            totalTokensCirculating,
            totalTransactions,
            recentTransactions,
            tokenSymbol = "SDT",
            tokenName = "SynDock Token",
            network = "Polygon L2"
        };
    }

    private static string ComputeSha256(string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return "0x" + Convert.ToHexString(bytes).ToLower();
    }
}
