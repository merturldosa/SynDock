using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;

namespace Shop.Infrastructure.Integration;

public class AccountingEntryAutoCreator : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IAccountingService _accounting;
    private readonly IShopDbContext _db;
    private readonly ILogger<AccountingEntryAutoCreator> _logger;

    public AccountingEntryAutoCreator(IAccountingService accounting, IShopDbContext db, ILogger<AccountingEntryAutoCreator> logger)
    {
        _accounting = accounting;
        _db = db;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken ct)
    {
        try
        {
            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == notification.OrderId, ct);
            if (order == null) return;

            // Find Accounts Receivable (1200) and Sales Revenue (4100)
            var arAccount = await _db.ChartOfAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.TenantId == notification.TenantId && a.AccountCode == "1200", ct);
            var revenueAccount = await _db.ChartOfAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.TenantId == notification.TenantId && a.AccountCode == "4100", ct);

            if (arAccount == null || revenueAccount == null)
            {
                _logger.LogWarning("Chart of Accounts not set up for tenant {TenantId}, skipping GL entry for order {OrderNumber}", notification.TenantId, notification.OrderNumber);
                return;
            }

            await _accounting.CreateDoubleEntryAsync(
                notification.TenantId,
                debitAccountId: arAccount.Id,
                creditAccountId: revenueAccount.Id,
                amount: order.TotalAmount,
                entryDate: DateTime.UtcNow,
                description: $"Sales Order {notification.OrderNumber}",
                referenceType: "Order",
                referenceId: notification.OrderId,
                createdBy: "system-auto",
                ct: ct);

            _logger.LogInformation("Auto-created GL entry for order {OrderNumber}, amount: {Amount}", notification.OrderNumber, order.TotalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create GL entry for order {OrderNumber}", notification.OrderNumber);
        }
    }
}
