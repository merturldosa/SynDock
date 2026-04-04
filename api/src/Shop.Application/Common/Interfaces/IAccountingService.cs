using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IAccountingService
{
    // Chart of Accounts
    Task<List<ChartOfAccount>> GetAccountsAsync(int tenantId, CancellationToken ct = default);
    Task<ChartOfAccount> CreateAccountAsync(int tenantId, string accountCode, string name, string accountType, string? parentAccountCode, string? description, string createdBy, CancellationToken ct = default);

    // Account Entries
    Task<AccountEntry> CreateEntryAsync(int tenantId, int chartOfAccountId, DateTime entryDate, string entryType, decimal amount, string description, string? referenceType, int? referenceId, string createdBy, CancellationToken ct = default);
    Task<(List<AccountEntry> Items, int TotalCount)> GetEntriesAsync(int tenantId, int? accountId = null, string? status = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task ApproveEntryAsync(int tenantId, int entryId, int approverUserId, string updatedBy, CancellationToken ct = default);
    Task ReverseEntryAsync(int tenantId, int entryId, string updatedBy, CancellationToken ct = default);

    // Reports
    Task<object> GetBalanceSheetAsync(int tenantId, DateTime asOfDate, CancellationToken ct = default);
    Task<object> GetIncomeStatementAsync(int tenantId, DateTime from, DateTime to, CancellationToken ct = default);

    // Double-entry bookkeeping
    Task<(AccountEntry Debit, AccountEntry Credit)> CreateDoubleEntryAsync(int tenantId, int debitAccountId, int creditAccountId, decimal amount, DateTime entryDate, string description, string? referenceType, int? referenceId, string createdBy, CancellationToken ct = default);

    // Trial Balance
    Task<object> GetTrialBalanceAsync(int tenantId, DateTime asOfDate, CancellationToken ct = default);

    // Period Closing
    Task<object> CloseMonthAsync(int tenantId, string period, string closedBy, CancellationToken ct = default);
    Task<List<string>> GetClosedPeriodsAsync(int tenantId, CancellationToken ct = default);

    // Accounts Receivable / Payable
    Task<object> GetAccountsReceivableAsync(int tenantId, CancellationToken ct = default);
    Task<object> GetAccountsPayableAsync(int tenantId, CancellationToken ct = default);

    // Cost Analysis
    Task<CostAnalysis> CreateCostAnalysisAsync(int tenantId, int? productId, string analysisPeriod, decimal materialCost, decimal laborCost, decimal overheadCost, decimal revenue, int unitsSold, string createdBy, CancellationToken ct = default);
    Task<List<CostAnalysis>> GetCostAnalysesAsync(int tenantId, int? productId = null, string? period = null, CancellationToken ct = default);
}
