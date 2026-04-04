using MediatR;

namespace Shop.Application.Orders.Events;

public record ProductCreatedEvent(int ProductId, int TenantId, string ProductName, decimal Price, string? ImageUrl) : INotification;
