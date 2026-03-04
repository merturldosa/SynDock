using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class AutoCouponService : IAutoCouponService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IShopDbContext _db;
    private readonly ILogger<AutoCouponService> _logger;

    public AutoCouponService(IShopDbContext db, ILogger<AutoCouponService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static AutoCouponConfig? GetAutoCouponConfig(string? configJson)
    {
        if (string.IsNullOrEmpty(configJson)) return null;
        try
        {
            var tc = JsonSerializer.Deserialize<TenantConfig>(configJson, JsonOptions);
            return tc?.AutoCoupon;
        }
        catch { return null; }
    }

    public async Task IssueWelcomeCouponAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var configJson = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.ConfigJson)
            .FirstOrDefaultAsync(ct);

        var config = GetAutoCouponConfig(configJson);
        if (config is not { WelcomeCouponEnabled: true }) return;

        var coupon = await _db.Coupons
            .FirstOrDefaultAsync(c => c.TenantId == tenantId
                && c.Code == config.WelcomeCouponCode
                && c.IsActive, ct);

        if (coupon == null)
        {
            _logger.LogWarning("Welcome coupon '{Code}' not found for tenant {TenantId}",
                config.WelcomeCouponCode, tenantId);
            return;
        }

        var alreadyIssued = await _db.UserCoupons
            .AnyAsync(uc => uc.TenantId == tenantId
                && uc.UserId == userId
                && uc.CouponId == coupon.Id, ct);

        if (alreadyIssued) return;

        await _db.UserCoupons.AddAsync(new UserCoupon
        {
            TenantId = tenantId,
            UserId = userId,
            CouponId = coupon.Id,
            CreatedBy = "System"
        }, ct);

        _logger.LogInformation("Welcome coupon '{Code}' issued to user {UserId} in tenant {TenantId}",
            config.WelcomeCouponCode, userId, tenantId);
    }

    public async Task IssueBirthdayCouponsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var tenants = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.ConfigJson })
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            var config = GetAutoCouponConfig(tenant.ConfigJson);
            if (config is not { BirthdayCouponEnabled: true }) continue;

            var coupon = await _db.Coupons
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == tenant.Id
                    && c.Code == config.BirthdayCouponCode
                    && c.IsActive, ct);

            if (coupon == null) continue;

            // Find users with today's birthday who haven't received this year's coupon
            var birthdayUsers = await _db.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenant.Id
                    && u.IsActive
                    && u.Birthday != null
                    && u.Birthday.Value.Month == today.Month
                    && u.Birthday.Value.Day == today.Day)
                .Select(u => u.Id)
                .ToListAsync(ct);

            if (birthdayUsers.Count == 0) continue;

            var alreadyIssued = await _db.UserCoupons
                .IgnoreQueryFilters()
                .Where(uc => uc.TenantId == tenant.Id
                    && uc.CouponId == coupon.Id
                    && birthdayUsers.Contains(uc.UserId)
                    && uc.CreatedAt.Year == today.Year)
                .Select(uc => uc.UserId)
                .ToListAsync(ct);

            var newRecipients = birthdayUsers.Except(alreadyIssued).ToList();
            if (newRecipients.Count == 0) continue;

            foreach (var userId in newRecipients)
            {
                await _db.UserCoupons.AddAsync(new UserCoupon
                {
                    TenantId = tenant.Id,
                    UserId = userId,
                    CouponId = coupon.Id,
                    CreatedBy = "BirthdayCouponJob"
                }, ct);
            }

            _logger.LogInformation("Birthday coupon '{Code}' issued to {Count} users in tenant {TenantId}",
                config.BirthdayCouponCode, newRecipients.Count, tenant.Id);
        }
    }
}
