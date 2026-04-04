using MediatR;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;

namespace Shop.Infrastructure.Integration;

public class WorkflowAutoCreator : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IWorkflowService _workflow;
    private readonly ILogger<WorkflowAutoCreator> _logger;

    public WorkflowAutoCreator(IWorkflowService workflow, ILogger<WorkflowAutoCreator> logger)
    {
        _workflow = workflow;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken ct)
    {
        try
        {
            await _workflow.CreateOrderWorkItemsAsync(notification.TenantId, notification.OrderId, notification.OrderNumber, 0, ct);
            _logger.LogInformation("Workflow items created for order {OrderNumber}", notification.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow items for order {OrderNumber}", notification.OrderNumber);
        }
    }
}
