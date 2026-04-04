using MediatR;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;

namespace Shop.Infrastructure.Integration;

public class CrmTicketAutoCreator : INotificationHandler<OrderCancelledEvent>
{
    private readonly ICrmService _crm;
    private readonly ILogger<CrmTicketAutoCreator> _logger;

    public CrmTicketAutoCreator(ICrmService crm, ILogger<CrmTicketAutoCreator> logger)
    {
        _crm = crm;
        _logger = logger;
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken ct)
    {
        try
        {
            var ticket = await _crm.CreateTicketAsync(
                notification.TenantId,
                notification.UserId,
                subject: $"Order {notification.OrderNumber} Cancelled",
                category: "Order",
                priority: "High",
                content: $"Order {notification.OrderNumber} was cancelled.\nReason: {notification.Reason ?? "Not specified"}\n\nPlease follow up with the customer.",
                orderId: notification.OrderId,
                createdBy: "system-auto",
                ct: ct);

            _logger.LogInformation("Auto-created CS ticket {TicketNumber} for cancelled order {OrderNumber}", ticket.TicketNumber, notification.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create CS ticket for cancelled order {OrderNumber}", notification.OrderNumber);
        }
    }
}
