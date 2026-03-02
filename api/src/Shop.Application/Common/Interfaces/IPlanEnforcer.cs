using SynDock.Core.Common;

namespace Shop.Application.Common.Interfaces;

public interface IPlanEnforcer
{
    Task<Result<bool>> CanCreateProduct(int tenantId, CancellationToken ct = default);
    Task<Result<bool>> CanRegisterUser(int tenantId, CancellationToken ct = default);
    Task<Result<bool>> CanPlaceOrder(int tenantId, CancellationToken ct = default);

    /// <summary>Ensures TenantUsage row exists and returns current counts</summary>
    Task EnsureUsageTracked(int tenantId, CancellationToken ct = default);

    /// <summary>Increment product count after creation</summary>
    Task IncrementProductCount(int tenantId, int delta = 1, CancellationToken ct = default);

    /// <summary>Increment user count after registration</summary>
    Task IncrementUserCount(int tenantId, int delta = 1, CancellationToken ct = default);

    /// <summary>Increment monthly order count after order creation</summary>
    Task IncrementOrderCount(int tenantId, int delta = 1, CancellationToken ct = default);
}
