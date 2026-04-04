using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class AccountingController : ControllerBase
{
    private readonly IAccountingService _accounting;
    private readonly ICurrentUserService _currentUser;

    public AccountingController(IAccountingService accounting, ICurrentUserService currentUser)
    {
        _accounting = accounting;
        _currentUser = currentUser;
    }

    // Chart of Accounts
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
        => Ok(await _accounting.GetAccountsAsync(0, ct));

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest req, CancellationToken ct)
        => Ok(await _accounting.CreateAccountAsync(0, req.AccountCode, req.Name, req.AccountType, req.ParentAccountCode, req.Description, _currentUser.Username ?? "system", ct));

    // Entries
    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries([FromQuery] int? accountId, [FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var (items, totalCount) = await _accounting.GetEntriesAsync(0, accountId, status, from, to, page, pageSize, ct);
        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpPost("entries")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateEntryRequest req, CancellationToken ct)
        => Ok(await _accounting.CreateEntryAsync(0, req.ChartOfAccountId, req.EntryDate, req.EntryType, req.Amount, req.Description, req.ReferenceType, req.ReferenceId, _currentUser.Username ?? "system", ct));

    [HttpPut("entries/{id}/approve")]
    public async Task<IActionResult> ApproveEntry(int id, CancellationToken ct)
    {
        await _accounting.ApproveEntryAsync(0, id, _currentUser.UserId ?? 0, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Entry approved" });
    }

    [HttpPut("entries/{id}/reverse")]
    public async Task<IActionResult> ReverseEntry(int id, CancellationToken ct)
    {
        await _accounting.ReverseEntryAsync(0, id, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Entry reversed" });
    }

    // Reports
    [HttpGet("reports/balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTime? asOfDate, CancellationToken ct)
        => Ok(await _accounting.GetBalanceSheetAsync(0, asOfDate ?? DateTime.UtcNow, ct));

    [HttpGet("reports/income-statement")]
    public async Task<IActionResult> GetIncomeStatement([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(await _accounting.GetIncomeStatementAsync(0, from, to, ct));

    // Cost Analysis
    [HttpGet("cost-analysis")]
    public async Task<IActionResult> GetCostAnalyses([FromQuery] int? productId, [FromQuery] string? period, CancellationToken ct)
        => Ok(await _accounting.GetCostAnalysesAsync(0, productId, period, ct));

    [HttpPost("cost-analysis")]
    public async Task<IActionResult> CreateCostAnalysis([FromBody] CreateCostAnalysisRequest req, CancellationToken ct)
        => Ok(await _accounting.CreateCostAnalysisAsync(0, req.ProductId, req.AnalysisPeriod, req.MaterialCost, req.LaborCost, req.OverheadCost, req.Revenue, req.UnitsSold, _currentUser.Username ?? "system", ct));

    // Double-entry bookkeeping
    [HttpPost("entries/double-entry")]
    public async Task<IActionResult> CreateDoubleEntry([FromBody] CreateDoubleEntryRequest req, CancellationToken ct)
    {
        var (debit, credit) = await _accounting.CreateDoubleEntryAsync(0, req.DebitAccountId, req.CreditAccountId, req.Amount, req.EntryDate, req.Description, req.ReferenceType, req.ReferenceId, _currentUser.Username ?? "system", ct);
        return Ok(new { debit, credit });
    }

    // Trial Balance
    [HttpGet("reports/trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime? asOfDate, CancellationToken ct)
        => Ok(await _accounting.GetTrialBalanceAsync(0, asOfDate ?? DateTime.UtcNow, ct));

    // Period Closing
    [HttpPost("period/close")]
    public async Task<IActionResult> CloseMonth([FromBody] CloseMonthRequest req, CancellationToken ct)
        => Ok(await _accounting.CloseMonthAsync(0, req.Period, _currentUser.Username ?? "system", ct));

    [HttpGet("period/closed")]
    public async Task<IActionResult> GetClosedPeriods(CancellationToken ct)
        => Ok(await _accounting.GetClosedPeriodsAsync(0, ct));

    // Accounts Receivable / Payable
    [HttpGet("reports/accounts-receivable")]
    public async Task<IActionResult> GetAccountsReceivable(CancellationToken ct)
        => Ok(await _accounting.GetAccountsReceivableAsync(0, ct));

    [HttpGet("reports/accounts-payable")]
    public async Task<IActionResult> GetAccountsPayable(CancellationToken ct)
        => Ok(await _accounting.GetAccountsPayableAsync(0, ct));
}

// Request DTOs
public record CreateAccountRequest(string AccountCode, string Name, string AccountType, string? ParentAccountCode = null, string? Description = null);
public record CreateEntryRequest(int ChartOfAccountId, DateTime EntryDate, string EntryType, decimal Amount, string Description, string? ReferenceType = null, int? ReferenceId = null);
public record CreateCostAnalysisRequest(int? ProductId, string AnalysisPeriod, decimal MaterialCost, decimal LaborCost, decimal OverheadCost, decimal Revenue, int UnitsSold);
public record CreateDoubleEntryRequest(int DebitAccountId, int CreditAccountId, decimal Amount, DateTime EntryDate, string Description, string? ReferenceType = null, int? ReferenceId = null);
public record CloseMonthRequest(string Period);
