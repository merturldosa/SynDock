using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

public class SupplierPerformanceJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SupplierPerformanceJob> _logger;

    public SupplierPerformanceJob(IServiceProvider services, ILogger<SupplierPerformanceJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await UpdateSupplierStats(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier stats");
            }
            await Task.Delay(TimeSpan.FromDays(7), ct);
        }
    }

    private async Task UpdateSupplierStats(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var suppliers = await db.Suppliers.Where(s => s.Status == "Active").ToListAsync(ct);

        foreach (var supplier in suppliers)
        {
            var orders = await db.ProcurementOrders
                .Where(po => po.SupplierId == supplier.Id && po.Status == "Delivered")
                .ToListAsync(ct);

            if (orders.Count == 0) continue;

            supplier.TotalOrders = orders.Count;
            supplier.TotalAmount = orders.Sum(o => o.TotalAmount);

            var onTimeCount = orders.Count(o => o.ActualDeliveryDate != null && o.ExpectedDeliveryDate != null && o.ActualDeliveryDate <= o.ExpectedDeliveryDate);
            supplier.OnTimeDeliveryRate = orders.Count > 0 ? (decimal)onTimeCount / orders.Count * 100 : 100;

            // Update grade based on on-time rate
            supplier.Grade = supplier.OnTimeDeliveryRate switch
            {
                >= 95 => "S",
                >= 85 => "A",
                >= 70 => "B",
                >= 50 => "C",
                _ => "D"
            };

            supplier.UpdatedBy = "SupplierPerformanceJob";
            supplier.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Updated performance stats for {Count} suppliers", suppliers.Count);
    }
}
