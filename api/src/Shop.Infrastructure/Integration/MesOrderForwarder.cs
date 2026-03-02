using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;

namespace Shop.Infrastructure.Integration;

public class MesOrderForwarder : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IMesClient _mesClient;
    private readonly IMesProductMapper _mapper;
    private readonly IShopDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MesOrderForwarder> _logger;

    public MesOrderForwarder(
        IMesClient mesClient,
        IMesProductMapper mapper,
        IShopDbContext db,
        IConfiguration configuration,
        ILogger<MesOrderForwarder> logger)
    {
        _mesClient = mesClient;
        _mapper = mapper;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _mesClient.IsAvailableAsync(cancellationToken))
            {
                _logger.LogInformation("MES not available, skipping order forwarding for {OrderNumber}", notification.OrderNumber);
                return;
            }

            var order = await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == notification.OrderId, cancellationToken);
            if (order is null) return;

            var orderItems = await _db.OrderItems.AsNoTracking()
                .Where(oi => oi.OrderId == notification.OrderId)
                .ToListAsync(cancellationToken);

            var customerId = _configuration.GetValue<long>("Mes:CustomerId", 1);
            var salesUserId = _configuration.GetValue<long>("Mes:SalesUserId", 1);

            var lineNo = 0;
            var items = new List<MesSalesOrderLine>();
            foreach (var item in orderItems)
            {
                var mesCode = await _mapper.GetMesProductCodeAsync(item.ProductId, cancellationToken);
                if (mesCode is null) continue;

                lineNo++;
                items.Add(new MesSalesOrderLine(
                    LineNo: lineNo,
                    ProductId: item.ProductId,
                    OrderedQuantity: item.Quantity,
                    Unit: "EA",
                    UnitPrice: item.UnitPrice));
            }

            if (items.Count == 0)
            {
                _logger.LogInformation("No MES-mapped products in order {OrderNumber}", notification.OrderNumber);
                return;
            }

            var request = new MesSalesOrderRequest(
                OrderNo: notification.OrderNumber,
                OrderDate: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                CustomerId: customerId,
                SalesUserId: salesUserId,
                Items: items);

            var result = await _mesClient.CreateSalesOrderAsync(request, cancellationToken);

            if (result.Success)
                _logger.LogInformation("Order {OrderNumber} forwarded to MES: {MesOrderId}", notification.OrderNumber, result.MesOrderId);
            else
                _logger.LogWarning("Failed to forward order {OrderNumber} to MES: {Error}", notification.OrderNumber, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            // Non-blocking: MES failure should not affect order confirmation
            _logger.LogError(ex, "Error forwarding order {OrderNumber} to MES", notification.OrderNumber);
        }
    }
}
