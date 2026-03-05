using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Commands;

// ── Commission Setting (수수료 설정) ──

public record UpsertCommissionSettingCommand(
    int TenantId,
    int? ProductId,
    int? CategoryId,
    decimal CommissionRate,
    string SettlementCycle,
    int SettlementDayOfWeek,
    decimal MinSettlementAmount,
    string? BankName,
    string? BankAccount,
    string? BankHolder) : IRequest<Result<int>>;

public class UpsertCommissionSettingCommandHandler : IRequestHandler<UpsertCommissionSettingCommand, Result<int>>
{
    private readonly IShopDbContext _db;

    public UpsertCommissionSettingCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> Handle(UpsertCommissionSettingCommand request, CancellationToken cancellationToken)
    {
        var existing = await _db.CommissionSettings
            .FirstOrDefaultAsync(cs =>
                cs.TenantId == request.TenantId
                && cs.ProductId == request.ProductId
                && cs.CategoryId == request.CategoryId, cancellationToken);

        if (existing is not null)
        {
            existing.CommissionRate = request.CommissionRate;
            existing.SettlementCycle = request.SettlementCycle;
            existing.SettlementDayOfWeek = request.SettlementDayOfWeek;
            existing.MinSettlementAmount = request.MinSettlementAmount;
            existing.BankName = request.BankName;
            existing.BankAccount = request.BankAccount;
            existing.BankHolder = request.BankHolder;
            existing.UpdatedBy = "PlatformAdmin";
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new CommissionSetting
            {
                TenantId = request.TenantId,
                ProductId = request.ProductId,
                CategoryId = request.CategoryId,
                CommissionRate = request.CommissionRate,
                SettlementCycle = request.SettlementCycle,
                SettlementDayOfWeek = request.SettlementDayOfWeek,
                MinSettlementAmount = request.MinSettlementAmount,
                BankName = request.BankName,
                BankAccount = request.BankAccount,
                BankHolder = request.BankHolder,
                CreatedBy = "PlatformAdmin"
            };
            await _db.CommissionSettings.AddAsync(existing, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(existing.Id);
    }
}

// ── Calculate Order Commission (주문별 수수료 계산) ──

public record CalculateOrderCommissionCommand(int OrderId) : IRequest<Result<int>>;

public class CalculateOrderCommissionCommandHandler : IRequestHandler<CalculateOrderCommissionCommand, Result<int>>
{
    private readonly IShopDbContext _db;

    public CalculateOrderCommissionCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> Handle(CalculateOrderCommissionCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
            return Result<int>.Failure("주문을 찾을 수 없습니다.");

        // 이미 수수료가 계산된 주문인지 확인
        var exists = await _db.Commissions
            .AnyAsync(c => c.OrderId == request.OrderId, cancellationToken);
        if (exists)
            return Result<int>.Failure("이미 수수료가 계산된 주문입니다.");

        // 수수료율 결정: 상품별 → 카테고리별 → 테넌트 기본
        var rate = await ResolveCommissionRate(order.TenantId, cancellationToken);

        var orderAmount = order.TotalAmount;
        var commissionAmount = Math.Floor(orderAmount * rate / 100m);
        var settlementAmount = orderAmount - commissionAmount;

        var commission = new Commission
        {
            TenantId = order.TenantId,
            OrderId = order.Id,
            OrderAmount = orderAmount,
            CommissionRate = rate,
            CommissionAmount = commissionAmount,
            SettlementAmount = settlementAmount,
            Status = "Pending",
            CreatedBy = "CommissionSystem"
        };

        await _db.Commissions.AddAsync(commission, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(commission.Id);
    }

    private async Task<decimal> ResolveCommissionRate(int tenantId, CancellationToken ct)
    {
        // 테넌트 기본 수수료율 (ProductId=null, CategoryId=null)
        var setting = await _db.CommissionSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cs =>
                cs.TenantId == tenantId
                && cs.ProductId == null
                && cs.CategoryId == null, ct);

        return setting?.CommissionRate ?? 5.0m; // 기본 5%
    }
}

// ── Create Settlement (정산 배치 생성) ──

public record CreateSettlementCommand(int TenantId, DateTime PeriodStart, DateTime PeriodEnd) : IRequest<Result<int>>;

public class CreateSettlementCommandHandler : IRequestHandler<CreateSettlementCommand, Result<int>>
{
    private readonly IShopDbContext _db;

    public CreateSettlementCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> Handle(CreateSettlementCommand request, CancellationToken cancellationToken)
    {
        // 해당 기간에 Pending 상태인 수수료 내역 조회
        var pendingCommissions = await _db.Commissions
            .Where(c => c.TenantId == request.TenantId
                        && c.Status == "Pending"
                        && c.CreatedAt >= request.PeriodStart
                        && c.CreatedAt < request.PeriodEnd)
            .ToListAsync(cancellationToken);

        if (pendingCommissions.Count == 0)
            return Result<int>.Failure("정산할 수수료 내역이 없습니다.");

        var totalOrderAmount = pendingCommissions.Sum(c => c.OrderAmount);
        var totalCommission = pendingCommissions.Sum(c => c.CommissionAmount);
        var totalSettlement = pendingCommissions.Sum(c => c.SettlementAmount);

        // 최소 정산 금액 확인
        var setting = await _db.CommissionSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cs =>
                cs.TenantId == request.TenantId
                && cs.ProductId == null
                && cs.CategoryId == null, cancellationToken);

        if (setting is not null && totalSettlement < setting.MinSettlementAmount)
            return Result<int>.Failure($"최소 정산 금액({setting.MinSettlementAmount:N0}원) 미달. 현재: {totalSettlement:N0}원");

        var settlement = new Settlement
        {
            TenantId = request.TenantId,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            OrderCount = pendingCommissions.Count,
            TotalOrderAmount = totalOrderAmount,
            TotalCommission = totalCommission,
            TotalSettlementAmount = totalSettlement,
            Status = "Ready",
            BankName = setting?.BankName,
            BankAccount = setting?.BankAccount,
            CreatedBy = "SettlementSystem"
        };

        await _db.Settlements.AddAsync(settlement, cancellationToken);

        // 수수료 내역을 정산에 연결
        foreach (var commission in pendingCommissions)
        {
            commission.SettlementId = settlement.Id;
            commission.Status = "Settled";
            commission.UpdatedBy = "SettlementSystem";
            commission.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(settlement.Id);
    }
}

// ── Process Settlement (정산 처리 완료 - 이체 요청 + 알림) ──

public record ProcessSettlementCommand(int SettlementId, string? TransactionId, string? SettledBy) : IRequest<Result<bool>>;

public class ProcessSettlementCommandHandler : IRequestHandler<ProcessSettlementCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ITransferService _transferService;
    private readonly IEmailService _emailService;

    public ProcessSettlementCommandHandler(IShopDbContext db, ITransferService transferService, IEmailService emailService)
    {
        _db = db;
        _transferService = transferService;
        _emailService = emailService;
    }

    public async Task<Result<bool>> Handle(ProcessSettlementCommand request, CancellationToken cancellationToken)
    {
        var settlement = await _db.Settlements
            .FirstOrDefaultAsync(s => s.Id == request.SettlementId, cancellationToken);

        if (settlement is null)
            return Result<bool>.Failure("정산 배치를 찾을 수 없습니다.");

        if (settlement.Status == "Completed")
            return Result<bool>.Failure("이미 처리 완료된 정산입니다.");

        // 이체 요청 (ITransferService 통해 은행 API 호출)
        var transactionId = request.TransactionId;
        if (string.IsNullOrEmpty(transactionId))
        {
            settlement.Status = "Processing";
            await _db.SaveChangesAsync(cancellationToken);

            var transferResult = await _transferService.RequestTransferAsync(new TransferRequest(
                settlement.Id,
                settlement.TenantId,
                settlement.BankName,
                settlement.BankAccount,
                null,
                settlement.TotalSettlementAmount,
                $"SynDock 정산 #{settlement.Id} ({settlement.PeriodStart:yyyy-MM-dd}~{settlement.PeriodEnd:yyyy-MM-dd})"),
                cancellationToken);

            if (!transferResult.IsSuccess)
            {
                settlement.Status = "Failed";
                settlement.UpdatedBy = "SettlementSystem";
                settlement.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return Result<bool>.Failure($"이체 실패: {transferResult.Error}");
            }

            transactionId = transferResult.TransactionId;
        }

        settlement.Status = "Completed";
        settlement.TransactionId = transactionId;
        settlement.SettledBy = request.SettledBy ?? "SettlementSystem";
        settlement.SettledAt = DateTime.UtcNow;
        settlement.UpdatedBy = request.SettledBy ?? "SettlementSystem";
        settlement.UpdatedAt = DateTime.UtcNow;

        // 연결된 수수료 내역을 Paid 상태로 변경
        var commissions = await _db.Commissions
            .Where(c => c.SettlementId == request.SettlementId)
            .ToListAsync(cancellationToken);

        foreach (var c in commissions)
        {
            c.Status = "Paid";
            c.UpdatedBy = "SettlementSystem";
            c.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // 정산 완료 이메일 알림 (테넌트 관리자에게)
        await SendSettlementNotificationAsync(settlement, cancellationToken);

        return Result<bool>.Success(true);
    }

    private async Task SendSettlementNotificationAsync(Settlement settlement, CancellationToken ct)
    {
        try
        {
            var tenant = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == settlement.TenantId, ct);

            if (tenant is null) return;

            // 테넌트 관리자 이메일 조회
            var adminEmails = await _db.Users
                .AsNoTracking()
                .Where(u => u.TenantId == settlement.TenantId
                    && (u.Role == "TenantAdmin" || u.Role == "Admin")
                    && u.IsActive
                    && !string.IsNullOrEmpty(u.Email))
                .Select(u => u.Email)
                .ToListAsync(ct);

            var subject = $"[SynDock] 정산 완료 알림 - {settlement.TotalSettlementAmount:N0}원";
            var body = BuildSettlementEmailBody(
                tenant.Name, settlement.PeriodStart, settlement.PeriodEnd,
                settlement.OrderCount, settlement.TotalOrderAmount,
                settlement.TotalCommission, settlement.TotalSettlementAmount,
                settlement.TransactionId);

            foreach (var email in adminEmails)
            {
                try { await _emailService.SendAsync(email, subject, body); }
                catch { /* Email failure does not affect settlement processing */ }
            }
        }
        catch { /* Notification failure is non-critical */ }
    }

    private static string BuildSettlementEmailBody(
        string tenantName, DateTime periodStart, DateTime periodEnd,
        int orderCount, decimal totalOrderAmount, decimal totalCommission,
        decimal totalSettlementAmount, string? transactionId)
    {
        var txInfo = transactionId != null
            ? $"<p style=\"color:#666;\">이체 번호: <strong>{transactionId}</strong></p>" : "";

        return $@"<!DOCTYPE html><html><head><meta charset=""utf-8""></head>
<body style=""margin:0;padding:0;background:#f5f5f5;font-family:sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;margin:0 auto;background:#fff;"">
<tr><td style=""background:#2e7d32;padding:24px 32px;""><h1 style=""margin:0;color:#fff;font-size:20px;"">정산 완료 알림</h1></td></tr>
<tr><td style=""padding:32px;"">
<p style=""color:#666;"">안녕하세요, <strong>{tenantName}</strong> 관리자님.</p>
<table style=""width:100%;border-collapse:collapse;margin:16px 0;"">
<tr style=""background:#f9f9f9;""><td style=""padding:12px;border:1px solid #eee;color:#666;"">정산 기간</td><td style=""padding:12px;border:1px solid #eee;font-weight:bold;"">{periodStart:yyyy-MM-dd} ~ {periodEnd:yyyy-MM-dd}</td></tr>
<tr><td style=""padding:12px;border:1px solid #eee;color:#666;"">주문 건수</td><td style=""padding:12px;border:1px solid #eee;font-weight:bold;"">{orderCount}건</td></tr>
<tr style=""background:#f9f9f9;""><td style=""padding:12px;border:1px solid #eee;color:#666;"">총 주문 금액</td><td style=""padding:12px;border:1px solid #eee;font-weight:bold;"">{totalOrderAmount:N0}원</td></tr>
<tr><td style=""padding:12px;border:1px solid #eee;color:#666;"">수수료</td><td style=""padding:12px;border:1px solid #eee;color:#e74c3c;"">-{totalCommission:N0}원</td></tr>
<tr style=""background:#e8f5e9;""><td style=""padding:12px;border:1px solid #eee;font-weight:bold;"">정산 금액</td><td style=""padding:12px;border:1px solid #eee;font-weight:bold;color:#2e7d32;font-size:18px;"">{totalSettlementAmount:N0}원</td></tr>
</table>
{txInfo}
<p style=""color:#999;font-size:13px;"">입금까지 영업일 기준 1~2일 소요될 수 있습니다.</p>
</td></tr>
<tr><td style=""padding:16px 32px;background:#f9f9f9;text-align:center;font-size:12px;color:#999;"">본 메일은 발신 전용입니다.</td></tr>
</table></body></html>";
    }
}
