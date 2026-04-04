using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class AccountingService : IAccountingService
{
    private readonly IShopDbContext _db;
    public AccountingService(IShopDbContext db) => _db = db;

    public async Task<List<ChartOfAccount>> GetAccountsAsync(int tenantId, CancellationToken ct = default)
        => await _db.ChartOfAccounts.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.AccountCode).ToListAsync(ct);

    public async Task<ChartOfAccount> CreateAccountAsync(int tenantId, string accountCode, string name, string accountType, string? parentAccountCode, string? description, string createdBy, CancellationToken ct = default)
    {
        var account = new ChartOfAccount { TenantId = tenantId, AccountCode = accountCode, Name = name, AccountType = accountType, ParentAccountCode = parentAccountCode, Description = description, CreatedBy = createdBy };
        _db.ChartOfAccounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return account;
    }

    public async Task<AccountEntry> CreateEntryAsync(int tenantId, int chartOfAccountId, DateTime entryDate, string entryType, decimal amount, string description, string? referenceType, int? referenceId, string createdBy, CancellationToken ct = default)
    {
        var entryNumber = $"JE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var entry = new AccountEntry
        {
            TenantId = tenantId, EntryNumber = entryNumber, ChartOfAccountId = chartOfAccountId,
            EntryDate = entryDate, EntryType = entryType, Amount = amount,
            Description = description, ReferenceType = referenceType, ReferenceId = referenceId,
            CreatedBy = createdBy
        };
        _db.AccountEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<(List<AccountEntry> Items, int TotalCount)> GetEntriesAsync(int tenantId, int? accountId = null, string? status = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.AccountEntries.AsNoTracking().Include(e => e.Account).AsQueryable();
        if (accountId.HasValue) query = query.Where(e => e.ChartOfAccountId == accountId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(e => e.Status == status);
        if (from.HasValue) query = query.Where(e => e.EntryDate >= from.Value);
        if (to.HasValue) query = query.Where(e => e.EntryDate <= to.Value);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(e => e.EntryDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task ApproveEntryAsync(int tenantId, int entryId, int approverUserId, string updatedBy, CancellationToken ct = default)
    {
        var entry = await _db.AccountEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct) ?? throw new InvalidOperationException("Entry not found");
        entry.Status = "Posted";
        entry.ApprovedByUserId = approverUserId;
        entry.ApprovedAt = DateTime.UtcNow;
        entry.UpdatedBy = updatedBy;
        entry.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReverseEntryAsync(int tenantId, int entryId, string updatedBy, CancellationToken ct = default)
    {
        var entry = await _db.AccountEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct) ?? throw new InvalidOperationException("Entry not found");
        entry.Status = "Reversed";
        entry.UpdatedBy = updatedBy;
        entry.UpdatedAt = DateTime.UtcNow;

        // Create reverse entry
        var reverseEntry = new AccountEntry
        {
            TenantId = entry.TenantId, EntryNumber = $"REV-{entry.EntryNumber}",
            ChartOfAccountId = entry.ChartOfAccountId, EntryDate = DateTime.UtcNow,
            EntryType = entry.EntryType == "Debit" ? "Credit" : "Debit",
            Amount = entry.Amount, Description = $"Reversal of {entry.EntryNumber}",
            ReferenceType = "Reversal", ReferenceId = entry.Id,
            Status = "Posted", CreatedBy = updatedBy
        };
        _db.AccountEntries.Add(reverseEntry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<object> GetBalanceSheetAsync(int tenantId, DateTime asOfDate, CancellationToken ct = default)
    {
        var entries = await _db.AccountEntries.AsNoTracking()
            .Where(e => e.Status == "Posted" && e.EntryDate <= asOfDate)
            .Include(e => e.Account)
            .ToListAsync(ct);

        var grouped = entries.GroupBy(e => e.Account.AccountType)
            .Select(g => new
            {
                Type = g.Key,
                Total = g.Sum(e => e.EntryType == "Debit" ? e.Amount : -e.Amount)
            }).ToList();

        return new
        {
            asOfDate,
            assets = grouped.FirstOrDefault(g => g.Type == "Asset")?.Total ?? 0,
            liabilities = grouped.FirstOrDefault(g => g.Type == "Liability")?.Total ?? 0,
            equity = grouped.FirstOrDefault(g => g.Type == "Equity")?.Total ?? 0,
            details = grouped
        };
    }

    public async Task<object> GetIncomeStatementAsync(int tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var entries = await _db.AccountEntries.AsNoTracking()
            .Where(e => e.Status == "Posted" && e.EntryDate >= from && e.EntryDate <= to)
            .Include(e => e.Account)
            .ToListAsync(ct);

        var revenue = entries.Where(e => e.Account.AccountType == "Revenue").Sum(e => e.Amount);
        var expenses = entries.Where(e => e.Account.AccountType == "Expense").Sum(e => e.Amount);

        return new { from, to, revenue, expenses, netIncome = revenue - expenses };
    }

    public async Task<(AccountEntry Debit, AccountEntry Credit)> CreateDoubleEntryAsync(int tenantId, int debitAccountId, int creditAccountId, decimal amount, DateTime entryDate, string description, string? referenceType, int? referenceId, string createdBy, CancellationToken ct = default)
    {
        var baseNumber = $"JE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var debitEntry = new AccountEntry
        {
            TenantId = tenantId, EntryNumber = $"{baseNumber}-D", ChartOfAccountId = debitAccountId,
            EntryDate = entryDate, EntryType = "Debit", Amount = amount,
            Description = description, ReferenceType = referenceType, ReferenceId = referenceId,
            CreatedBy = createdBy
        };
        var creditEntry = new AccountEntry
        {
            TenantId = tenantId, EntryNumber = $"{baseNumber}-C", ChartOfAccountId = creditAccountId,
            EntryDate = entryDate, EntryType = "Credit", Amount = amount,
            Description = description, ReferenceType = referenceType, ReferenceId = referenceId,
            CreatedBy = createdBy
        };

        _db.AccountEntries.Add(debitEntry);
        _db.AccountEntries.Add(creditEntry);
        await _db.SaveChangesAsync(ct);

        return (debitEntry, creditEntry);
    }

    public async Task<object> GetTrialBalanceAsync(int tenantId, DateTime asOfDate, CancellationToken ct = default)
    {
        var entries = await _db.AccountEntries.AsNoTracking()
            .Where(e => e.Status == "Posted" && e.EntryDate <= asOfDate)
            .Include(e => e.Account)
            .ToListAsync(ct);

        var accounts = entries.GroupBy(e => e.Account)
            .Select(g => new
            {
                code = g.Key.AccountCode,
                name = g.Key.Name,
                type = g.Key.AccountType,
                debitTotal = g.Where(e => e.EntryType == "Debit").Sum(e => e.Amount),
                creditTotal = g.Where(e => e.EntryType == "Credit").Sum(e => e.Amount),
                balance = g.Where(e => e.EntryType == "Debit").Sum(e => e.Amount) - g.Where(e => e.EntryType == "Credit").Sum(e => e.Amount)
            })
            .OrderBy(a => a.code)
            .ToList();

        var totalDebits = accounts.Sum(a => a.debitTotal);
        var totalCredits = accounts.Sum(a => a.creditTotal);

        return new { accounts, totalDebits, totalCredits, isBalanced = totalDebits == totalCredits };
    }

    public async Task<object> CloseMonthAsync(int tenantId, string period, string closedBy, CancellationToken ct = default)
    {
        // Check if already closed
        var existing = await _db.AccountingPeriods.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Period == period, ct);
        if (existing != null && existing.Status == "Closed")
            throw new InvalidOperationException($"Period {period} is already closed");

        // Parse period to date range
        var parts = period.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);
        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // Get all posted entries in the period for trial balance verification
        var postedEntries = await _db.AccountEntries.AsNoTracking()
            .Where(e => e.Status == "Posted" && e.EntryDate >= periodStart && e.EntryDate <= periodEnd)
            .ToListAsync(ct);

        var totalDebits = postedEntries.Where(e => e.EntryType == "Debit").Sum(e => e.Amount);
        var totalCredits = postedEntries.Where(e => e.EntryType == "Credit").Sum(e => e.Amount);
        var isBalanced = totalDebits == totalCredits;

        if (!isBalanced)
            throw new InvalidOperationException($"Trial balance is not balanced for period {period}. Debits: {totalDebits}, Credits: {totalCredits}");

        // Mark all Draft entries in the period as Closed
        var draftEntries = await _db.AccountEntries
            .Where(e => e.Status == "Draft" && e.EntryDate >= periodStart && e.EntryDate <= periodEnd)
            .ToListAsync(ct);

        foreach (var entry in draftEntries)
        {
            entry.Status = "Closed";
            entry.UpdatedBy = closedBy;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        var entriesClosed = draftEntries.Count;

        // Create or update AccountingPeriod record
        if (existing == null)
        {
            existing = new AccountingPeriod
            {
                TenantId = tenantId, Period = period, CreatedBy = closedBy
            };
            _db.AccountingPeriods.Add(existing);
        }

        existing.Status = "Closed";
        existing.TotalDebits = totalDebits;
        existing.TotalCredits = totalCredits;
        existing.IsBalanced = isBalanced;
        existing.EntriesCount = postedEntries.Count + entriesClosed;
        existing.ClosedAt = DateTime.UtcNow;
        existing.ClosedBy = closedBy;
        existing.UpdatedBy = closedBy;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new { period, entriesClosed, totalDebits, totalCredits, isBalanced };
    }

    public async Task<List<string>> GetClosedPeriodsAsync(int tenantId, CancellationToken ct = default)
    {
        return await _db.AccountingPeriods.AsNoTracking()
            .Where(p => p.Status == "Closed")
            .OrderByDescending(p => p.Period)
            .Select(p => p.Period)
            .ToListAsync(ct);
    }

    public async Task<object> GetAccountsReceivableAsync(int tenantId, CancellationToken ct = default)
    {
        var entries = await _db.AccountEntries.AsNoTracking()
            .Where(e => e.Status == "Posted" && e.Account.AccountType == "Asset" && e.Account.AccountCode.StartsWith("12"))
            .Include(e => e.Account)
            .ToListAsync(ct);

        var grouped = entries.GroupBy(e => new { e.ReferenceType, e.ReferenceId })
            .Select(g => new
            {
                referenceType = g.Key.ReferenceType,
                referenceId = g.Key.ReferenceId,
                debitTotal = g.Where(e => e.EntryType == "Debit").Sum(e => e.Amount),
                creditTotal = g.Where(e => e.EntryType == "Credit").Sum(e => e.Amount),
                balance = g.Where(e => e.EntryType == "Debit").Sum(e => e.Amount) - g.Where(e => e.EntryType == "Credit").Sum(e => e.Amount)
            })
            .Where(g => g.balance != 0)
            .ToList();

        return new
        {
            totalReceivable = grouped.Sum(g => g.balance),
            items = grouped
        };
    }

    public async Task<object> GetAccountsPayableAsync(int tenantId, CancellationToken ct = default)
    {
        var entries = await _db.AccountEntries.AsNoTracking()
            .Where(e => e.Status == "Posted" && e.Account.AccountType == "Liability" && e.Account.AccountCode.StartsWith("21"))
            .Include(e => e.Account)
            .ToListAsync(ct);

        var grouped = entries.GroupBy(e => new { e.ReferenceType, e.ReferenceId })
            .Select(g => new
            {
                referenceType = g.Key.ReferenceType,
                referenceId = g.Key.ReferenceId,
                debitTotal = g.Where(e => e.EntryType == "Debit").Sum(e => e.Amount),
                creditTotal = g.Where(e => e.EntryType == "Credit").Sum(e => e.Amount),
                balance = g.Where(e => e.EntryType == "Credit").Sum(e => e.Amount) - g.Where(e => e.EntryType == "Debit").Sum(e => e.Amount)
            })
            .Where(g => g.balance != 0)
            .ToList();

        return new
        {
            totalPayable = grouped.Sum(g => g.balance),
            items = grouped
        };
    }

    public async Task<CostAnalysis> CreateCostAnalysisAsync(int tenantId, int? productId, string analysisPeriod, decimal materialCost, decimal laborCost, decimal overheadCost, decimal revenue, int unitsSold, string createdBy, CancellationToken ct = default)
    {
        var totalCost = materialCost + laborCost + overheadCost;
        var grossProfit = revenue - totalCost;
        var grossMargin = revenue > 0 ? (grossProfit / revenue) * 100 : 0;
        var costPerUnit = unitsSold > 0 ? totalCost / unitsSold : 0;

        var analysis = new CostAnalysis
        {
            TenantId = tenantId, ProductId = productId, AnalysisPeriod = analysisPeriod,
            MaterialCost = materialCost, LaborCost = laborCost, OverheadCost = overheadCost,
            TotalCost = totalCost, Revenue = revenue, GrossProfit = grossProfit,
            GrossMarginPercent = grossMargin, UnitsSold = unitsSold, CostPerUnit = costPerUnit,
            CreatedBy = createdBy
        };
        _db.CostAnalyses.Add(analysis);
        await _db.SaveChangesAsync(ct);
        return analysis;
    }

    public async Task<List<CostAnalysis>> GetCostAnalysesAsync(int tenantId, int? productId = null, string? period = null, CancellationToken ct = default)
    {
        var query = _db.CostAnalyses.AsNoTracking().Include(c => c.Product).AsQueryable();
        if (productId.HasValue) query = query.Where(c => c.ProductId == productId.Value);
        if (!string.IsNullOrEmpty(period)) query = query.Where(c => c.AnalysisPeriod == period);
        return await query.OrderByDescending(c => c.AnalysisPeriod).ToListAsync(ct);
    }
}
