using MediatR;

namespace Shop.Application.Orders.Events;

public record OrderConfirmedEvent(int OrderId, int TenantId, string OrderNumber) : INotification;
