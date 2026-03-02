namespace Shop.Application.Common;

public static class PlanLimits
{
    public record Limits(int MaxProducts, int MaxUsers, int MaxMonthlyOrders, long MaxStorageBytes);

    private static readonly Dictionary<string, Limits> Plans = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Free"]       = new(20,    50,    100,    1L * 1024 * 1024 * 1024),   // 1 GB
        ["Basic"]      = new(200,   500,   1_000,  5L * 1024 * 1024 * 1024),   // 5 GB
        ["Pro"]        = new(2_000, 5_000, 10_000, 20L * 1024 * 1024 * 1024),  // 20 GB
        ["Enterprise"] = new(int.MaxValue, int.MaxValue, int.MaxValue, long.MaxValue),
    };

    public static Limits GetLimits(string planType)
        => Plans.TryGetValue(planType, out var limits) ? limits : Plans["Free"];
}
