using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/workflow")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflow;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;

    public WorkflowController(IWorkflowService workflow, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _workflow = workflow;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>Get my pending work items (assigned to me or my department)</summary>
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] string? department,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var items = await _workflow.GetMyWorkItemsAsync(tenantId, userId, department, status, ct);
        return Ok(items);
    }

    /// <summary>Get work items for a specific department</summary>
    [HttpGet("department/{dept}")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetDepartmentTasks(
        string dept,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var items = await _workflow.GetDepartmentWorkItemsAsync(tenantId, dept, status, ct);
        return Ok(items);
    }

    /// <summary>Get all work items (admin view with filters)</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetAllTasks(
        [FromQuery] string? module,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var items = await _workflow.GetAllWorkItemsAsync(tenantId, module, status, priority, page, pageSize, ct);
        return Ok(items);
    }

    /// <summary>Complete a work item</summary>
    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> CompleteWorkItem(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        await _workflow.CompleteWorkItemAsync(tenantId, id, _currentUser.Username, ct);
        return Ok(new { message = "Work item completed" });
    }

    /// <summary>Assign work item to a user</summary>
    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> AssignWorkItem(int id, [FromBody] AssignWorkItemRequest request, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        await _workflow.AssignWorkItemAsync(tenantId, id, request.UserId, request.UserName, _currentUser.Username, ct);
        return Ok(new { message = "Work item assigned" });
    }

    /// <summary>Cancel a work item</summary>
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> CancelWorkItem(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        await _workflow.CancelWorkItemAsync(tenantId, id, _currentUser.Username, ct);
        return Ok(new { message = "Work item cancelled" });
    }

    /// <summary>Get process pipeline steps</summary>
    [HttpGet("process/{processType}/{refType}/{refId:int}")]
    public async Task<IActionResult> GetProcessSteps(string processType, string refType, int refId, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var steps = await _workflow.GetProcessStepsAsync(tenantId, processType, refType, refId, ct);
        return Ok(steps);
    }

    /// <summary>Get workflow dashboard (summary, department stats, urgent items)</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? department,
        CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var dashboard = await _workflow.GetWorkflowDashboardAsync(tenantId, userId, department, ct);
        return Ok(dashboard);
    }

    /// <summary>Get AI-driven next action suggestions</summary>
    [HttpGet("ai-suggestions")]
    public async Task<IActionResult> GetAiSuggestions(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var suggestions = await _workflow.GetAiNextActionsAsync(tenantId, userId, ct);
        return Ok(suggestions);
    }

    /// <summary>Create a work item manually</summary>
    [HttpPost("items")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> CreateWorkItem([FromBody] CreateWorkItemRequest request, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var item = await _workflow.CreateWorkItemAsync(
            tenantId, request.Module, request.WorkType, request.Title, request.Description,
            request.Priority ?? "Normal", request.Department, request.AssignedToUserId,
            request.ReferenceType, request.ReferenceId, request.ActionUrl, request.DueDate,
            false, request.CanAutoComplete, request.AiSuggestion,
            _currentUser.Username, ct);
        return Ok(item);
    }
}

// ── Request DTOs ────────────────────────────────────────────

public record AssignWorkItemRequest(int UserId, string UserName);

public class CreateWorkItemRequest
{
    public string Module { get; set; } = "";
    public string WorkType { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Priority { get; set; } = "Normal";
    public string? Department { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? DueDate { get; set; }
    public bool CanAutoComplete { get; set; }
    public string? AiSuggestion { get; set; }
}
