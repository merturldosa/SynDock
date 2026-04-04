using MediatR;

namespace Shop.Application.Orders.Events;

public record ProcurementDeliveredEvent(int ProcurementOrderId, int TenantId, string OrderNumber) : INotification;
