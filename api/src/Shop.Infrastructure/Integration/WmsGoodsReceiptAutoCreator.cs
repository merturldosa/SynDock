using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;

namespace Shop.Infrastructure.Integration;

public class WmsGoodsReceiptAutoCreator : INotificationHandler<ProcurementDeliveredEvent>
{
    private readonly IWmsService _wms;
    private readonly IShopDbContext _db;
    private readonly ILogger<WmsGoodsReceiptAutoCreator> _logger;

    public WmsGoodsReceiptAutoCreator(IWmsService wms, IShopDbContext db, ILogger<WmsGoodsReceiptAutoCreator> logger)
    {
        _wms = wms;
        _db = db;
        _logger = logger;
    }

    public async Task Handle(ProcurementDeliveredEvent notification, CancellationToken ct)
    {
        try
        {
            var po = await _db.ProcurementOrders.AsNoTracking()
                .Include(p => p.Items).Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == notification.ProcurementOrderId, ct);
            if (po == null) return;

            var items = po.Items.Select(i => (i.ProductId, i.Quantity, i.LotNumber, (DateTime?)null)).ToList();

            var receipt = await _wms.CreateGoodsReceiptAsync(
                notification.TenantId,
                purchaseOrderId: null,
                supplierName: po.Supplier?.Name,
                items: items,
                createdBy: "system-auto",
                ct: ct);

            _logger.LogInformation("Auto-created goods receipt {ReceiptNumber} for PO {OrderNumber}", receipt.ReceiptNumber, notification.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create goods receipt for PO {OrderNumber}", notification.OrderNumber);
        }
    }
}
