namespace Shop.Domain.Interfaces;

public interface IShopFeatureProvider
{
    string FeatureId { get; }
    string TenantSlug { get; }
}
