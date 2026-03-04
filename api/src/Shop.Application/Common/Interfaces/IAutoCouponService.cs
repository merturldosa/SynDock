namespace Shop.Application.Common.Interfaces;

public interface IAutoCouponService
{
    Task IssueWelcomeCouponAsync(int tenantId, int userId, CancellationToken ct = default);
    Task IssueBirthdayCouponsAsync(CancellationToken ct = default);
}
