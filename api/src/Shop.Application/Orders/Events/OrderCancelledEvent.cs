using MediatR;

namespace Shop.Application.Orders.Events;

public record OrderCancelledEvent(int OrderId, int TenantId, string OrderNumber, int UserId, string? Reason) : INotification;
