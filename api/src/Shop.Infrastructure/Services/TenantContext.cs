using Shop.Domain.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public int TenantId => Tenant?.Id ?? 0;
    public string TenantSlug => Tenant?.Slug ?? string.Empty;
    public Tenant? Tenant { get; private set; }

    public void SetTenant(Tenant tenant)
    {
        Tenant = tenant;
    }
}
