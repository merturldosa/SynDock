using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChainController : ControllerBase
{
    private readonly IBlockchainService _chain;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;

    public ChainController(IBlockchainService chain, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _chain = chain;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    private int TenantId => _tenantContext.TenantId;

    // === Proof Endpoints ===

    /// <summary>Record a transaction proof (hash on chain)</summary>
    [HttpPost("proof")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> RecordProof([FromBody] RecordProofRequest req, CancellationToken ct)
    {
        var result = await _chain.RecordProofAsync(TenantId, req.ReferenceType, req.ReferenceId, req.Data, _currentUser.Username ?? "system", ct);
        return Ok(new
        {
            id = result.Id,
            dataHash = result.DataHash,
            status = result.Status,
            confirmedAt = result.ConfirmedAt
        });
    }

    /// <summary>Verify a proof by transaction ID</summary>
    [HttpGet("proof/{id}/verify")]
    public async Task<IActionResult> VerifyProof(int id, CancellationToken ct)
    {
        var valid = await _chain.VerifyProofAsync(id, ct);
        return Ok(new { transactionId = id, verified = valid });
    }

    /// <summary>List proofs with optional filter</summary>
    [HttpGet("proofs")]
    public async Task<IActionResult> ListProofs([FromQuery] string? referenceType, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var proofs = await _chain.GetProofsAsync(TenantId, referenceType, page, pageSize, ct);
        return Ok(proofs.Select(p => new
        {
            p.Id,
            p.TransactionType,
            p.ReferenceType,
            p.ReferenceId,
            p.DataHash,
            p.OnChainTxHash,
            p.Status,
            p.Network,
            p.BlockNumber,
            p.ConfirmedAt,
            p.CreatedAt
        }));
    }

    // === Wallet Endpoints ===

    /// <summary>Get my token wallet</summary>
    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? 0;
        var wallet = await _chain.GetOrCreateWalletAsync(TenantId, userId, ct);
        return Ok(new
        {
            wallet.Id,
            wallet.UserId,
            wallet.Balance,
            wallet.TotalEarned,
            wallet.TotalSpent,
            wallet.OnChainAddress,
            tokenSymbol = "SDT",
            tokenName = "SynDock Token"
        });
    }

    /// <summary>Earn tokens (admin only)</summary>
    [HttpPost("tokens/earn")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> EarnTokens([FromBody] EarnTokensRequest req, CancellationToken ct)
    {
        var tx = await _chain.EarnTokensAsync(TenantId, req.UserId, req.Amount, req.TransactionType, req.Description, req.ReferenceType, req.ReferenceId, _currentUser.Username ?? "system", ct);
        return Ok(new { transactionId = tx.Id, tx.Amount, tx.TransactionType, tx.Description });
    }

    /// <summary>Spend tokens from my wallet</summary>
    [HttpPost("tokens/spend")]
    public async Task<IActionResult> SpendTokens([FromBody] SpendTokensRequest req, CancellationToken ct)
    {
        try
        {
            var tx = await _chain.SpendTokensAsync(TenantId, _currentUser.UserId ?? 0, req.Amount, req.Description, req.ReferenceType, req.ReferenceId, _currentUser.Username ?? "system", ct);
            return Ok(new { transactionId = tx.Id, tx.Amount, tx.Description });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Transfer tokens to another user</summary>
    [HttpPost("tokens/transfer")]
    public async Task<IActionResult> TransferTokens([FromBody] TransferTokensRequest req, CancellationToken ct)
    {
        try
        {
            var tx = await _chain.TransferTokensAsync(TenantId, _currentUser.UserId ?? 0, req.ToUserId, req.Amount, req.Description, _currentUser.Username ?? "system", ct);
            return Ok(new { transactionId = tx.Id, tx.Amount, fromUserId = _currentUser.UserId ?? 0, toUserId = req.ToUserId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get my token transaction history</summary>
    [HttpGet("tokens/history")]
    public async Task<IActionResult> GetTokenHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var history = await _chain.GetTokenHistoryAsync(TenantId, _currentUser.UserId ?? 0, page, pageSize, ct);
        return Ok(history.Select(t => new
        {
            t.Id,
            t.FromUserId,
            t.ToUserId,
            t.Amount,
            t.TransactionType,
            t.Description,
            t.ReferenceType,
            t.ReferenceId,
            t.OnChainTxHash,
            t.CreatedAt
        }));
    }

    // === Dashboard ===

    /// <summary>Chain dashboard (admin)</summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var dashboard = await _chain.GetChainDashboardAsync(TenantId, ct);
        return Ok(dashboard);
    }

    /// <summary>Public chain stats (no auth required)</summary>
    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicStats(CancellationToken ct)
    {
        var dashboard = await _chain.GetChainDashboardAsync(TenantId, ct);
        return Ok(dashboard);
    }
}

// === Request DTOs ===

public record RecordProofRequest(string ReferenceType, int ReferenceId, string Data);
public record EarnTokensRequest(int UserId, decimal Amount, string TransactionType, string Description, string? ReferenceType = null, int? ReferenceId = null);
public record SpendTokensRequest(decimal Amount, string Description, string? ReferenceType = null, int? ReferenceId = null);
public record TransferTokensRequest(int ToUserId, decimal Amount, string Description);
