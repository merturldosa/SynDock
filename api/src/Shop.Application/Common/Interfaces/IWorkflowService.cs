using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IWorkflowService
{
    // Work Items
    Task<WorkItem> CreateWorkItemAsync(int tenantId, string module, string workType, string title, string? description, string priority, string? department, int? assignedToUserId, string? referenceType, int? referenceId, string? actionUrl, DateTime? dueDate, bool isAuto, bool canAutoComplete, string? aiSuggestion, string createdBy, CancellationToken ct = default);
    Task<List<WorkItem>> GetMyWorkItemsAsync(int tenantId, int userId, string? department, string? status, CancellationToken ct = default);
    Task<List<WorkItem>> GetDepartmentWorkItemsAsync(int tenantId, string department, string? status, CancellationToken ct = default);
    Task<List<WorkItem>> GetAllWorkItemsAsync(int tenantId, string? module, string? status, string? priority, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task CompleteWorkItemAsync(int tenantId, int workItemId, string completedBy, CancellationToken ct = default);
    Task AssignWorkItemAsync(int tenantId, int workItemId, int assignedToUserId, string assignedToName, string updatedBy, CancellationToken ct = default);
    Task CancelWorkItemAsync(int tenantId, int workItemId, string cancelledBy, CancellationToken ct = default);

    // Process Pipeline
    Task CreateProcessPipelineAsync(int tenantId, string processType, string? referenceType, int? referenceId, List<string> stepNames, string createdBy, CancellationToken ct = default);
    Task<List<ProcessStep>> GetProcessStepsAsync(int tenantId, string processType, string? referenceType, int? referenceId, CancellationToken ct = default);
    Task AdvanceProcessAsync(int tenantId, string processType, string? referenceType, int? referenceId, string completedBy, string? notes, CancellationToken ct = default);

    // Dashboard
    Task<object> GetWorkflowDashboardAsync(int tenantId, int? userId, string? department, CancellationToken ct = default);
    Task<object> GetAiNextActionsAsync(int tenantId, int userId, CancellationToken ct = default);

    // Auto-create work items from events
    Task CreateOrderWorkItemsAsync(int tenantId, int orderId, string orderNumber, decimal totalAmount, CancellationToken ct = default);
}
