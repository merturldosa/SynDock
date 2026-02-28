using Shop.Domain.Entities;

namespace Shop.Domain.Interfaces;

public interface ITenantContext
{
    int TenantId { get; }
    string TenantSlug { get; }
    Tenant? Tenant { get; }
    void SetTenant(Tenant tenant);
}
