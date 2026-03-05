namespace Shop.Application.Common;

public static class PlanLimits
{
    public record Limits(int MaxProducts, int MaxUsers, int MaxMonthlyOrders, long MaxStorageBytes);

    public record PlanInfo(string PlanType, decimal MonthlyPrice, Limits Limits);

    private static readonly Dictionary<string, Limits> Plans = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Free"]       = new(20,    50,    100,    1L * 1024 * 1024 * 1024),   // 1 GB
        ["Basic"]      = new(200,   500,   1_000,  5L * 1024 * 1024 * 1024),   // 5 GB
        ["Pro"]        = new(2_000, 5_000, 10_000, 20L * 1024 * 1024 * 1024),  // 20 GB
        ["Enterprise"] = new(int.MaxValue, int.MaxValue, int.MaxValue, long.MaxValue),
    };

    private static readonly Dictionary<string, decimal> Prices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Free"]       = 0m,
        ["Basic"]      = 29_000m,
        ["Pro"]        = 79_000m,
        ["Enterprise"] = 199_000m,
    };

    public static Limits GetLimits(string planType)
        => Plans.TryGetValue(planType, out var limits) ? limits : Plans["Free"];

    public static decimal GetPrice(string planType)
        => Prices.TryGetValue(planType, out var price) ? price : 0m;

    public static IReadOnlyList<PlanInfo> GetAllPlans()
        => Plans.Select(kv => new PlanInfo(kv.Key, GetPrice(kv.Key), kv.Value)).ToList();
}
