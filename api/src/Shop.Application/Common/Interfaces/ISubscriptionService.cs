using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface ISubscriptionService
{
    Task<TenantPlan?> GetCurrentPlanAsync(int tenantId, CancellationToken ct = default);
    Task ChangePlanAsync(int tenantId, string newPlanType, string updatedBy, CancellationToken ct = default);
    Task<object> GetUsageSummaryAsync(int tenantId, CancellationToken ct = default);
    Task<List<Invoice>> GetInvoicesAsync(int tenantId, CancellationToken ct = default);
    Task ProcessMonthlyBillingAsync(CancellationToken ct = default);
}
