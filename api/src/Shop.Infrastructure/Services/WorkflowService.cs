using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IShopDbContext _db;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(IShopDbContext db, ILogger<WorkflowService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Work Items ──────────────────────────────────────────────

    public async Task<WorkItem> CreateWorkItemAsync(
        int tenantId, string module, string workType, string title, string? description,
        string priority, string? department, int? assignedToUserId,
        string? referenceType, int? referenceId, string? actionUrl, DateTime? dueDate,
        bool isAuto, bool canAutoComplete, string? aiSuggestion,
        string createdBy, CancellationToken ct = default)
    {
        var item = new WorkItem
        {
            TenantId = tenantId,
            Module = module,
            WorkType = workType,
            Title = title,
            Description = description,
            Status = "Pending",
            Priority = priority,
            Department = department,
            AssignedToUserId = assignedToUserId,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ActionUrl = actionUrl,
            DueDate = dueDate,
            IsAutoCreated = isAuto,
            CanAutoComplete = canAutoComplete,
            AiSuggestion = aiSuggestion,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        // === Dynamic Auto-Routing ===
        // If no specific user assigned, find the right person automatically
        if (item.AssignedToUserId == null && !string.IsNullOrEmpty(department))
        {
            item.AssignedToUserId = await FindDepartmentOwnerAsync(tenantId, department, ct);
            if (item.AssignedToUserId != null)
            {
                var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == item.AssignedToUserId, ct);
                item.AssignedToName = user?.Name;
                _logger.LogInformation("Auto-routed [{Department}] task to {User}", department, item.AssignedToName);
            }
        }

        // If still no assignee (no department person), escalate to TenantAdmin
        if (item.AssignedToUserId == null)
        {
            var admin = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Role == "TenantAdmin" && u.IsActive, ct);
            if (admin != null)
            {
                item.AssignedToUserId = admin.Id;
                item.AssignedToName = admin.Name;
                _logger.LogInformation("No [{Department}] owner — escalated to TenantAdmin {Admin}", department, admin.Name);
            }
        }

        // If CanAutoComplete and no human needed, auto-complete immediately
        if (item.CanAutoComplete && item.AssignedToUserId == null)
        {
            item.Status = "AutoCompleted";
            item.CompletedAt = DateTime.UtcNow;
            item.CompletedBy = "AI-AutoComplete";
            _logger.LogInformation("Auto-completed [{Module}] {Title} — no assignee required", module, title);
        }

        _db.WorkItems.Add(item);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("WorkItem created: [{Module}] {Title} → {Assignee} (TenantId={TenantId})",
            module, title, item.AssignedToName ?? "Unassigned", tenantId);
        return item;
    }

    public async Task<List<WorkItem>> GetMyWorkItemsAsync(
        int tenantId, int userId, string? department, string? status, CancellationToken ct = default)
    {
        var query = _db.WorkItems
            .Where(w => w.TenantId == tenantId)
            .Where(w => w.AssignedToUserId == userId
                        || (w.AssignedToUserId == null && department != null && w.Department == department));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(w => w.Status == status);
        else
            query = query.Where(w => w.Status == "Pending" || w.Status == "InProgress");

        return await query.OrderByDescending(w => w.Priority == "Urgent" ? 0 : w.Priority == "High" ? 1 : w.Priority == "Normal" ? 2 : 3)
            .ThenBy(w => w.DueDate)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<WorkItem>> GetDepartmentWorkItemsAsync(
        int tenantId, string department, string? status, CancellationToken ct = default)
    {
        var query = _db.WorkItems
            .Where(w => w.TenantId == tenantId && w.Department == department);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(w => w.Status == status);
        else
            query = query.Where(w => w.Status == "Pending" || w.Status == "InProgress");

        return await query.OrderByDescending(w => w.Priority == "Urgent" ? 0 : w.Priority == "High" ? 1 : w.Priority == "Normal" ? 2 : 3)
            .ThenBy(w => w.DueDate)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<WorkItem>> GetAllWorkItemsAsync(
        int tenantId, string? module, string? status, string? priority,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.WorkItems.Where(w => w.TenantId == tenantId);

        if (!string.IsNullOrEmpty(module)) query = query.Where(w => w.Module == module);
        if (!string.IsNullOrEmpty(status)) query = query.Where(w => w.Status == status);
        if (!string.IsNullOrEmpty(priority)) query = query.Where(w => w.Priority == priority);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task CompleteWorkItemAsync(int tenantId, int workItemId, string completedBy, CancellationToken ct = default)
    {
        var item = await _db.WorkItems.FirstOrDefaultAsync(
            w => w.Id == workItemId && w.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"WorkItem {workItemId} not found");

        item.Status = "Completed";
        item.CompletedAt = DateTime.UtcNow;
        item.CompletedBy = completedBy;
        item.UpdatedBy = completedBy;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AssignWorkItemAsync(
        int tenantId, int workItemId, int assignedToUserId, string assignedToName,
        string updatedBy, CancellationToken ct = default)
    {
        var item = await _db.WorkItems.FirstOrDefaultAsync(
            w => w.Id == workItemId && w.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"WorkItem {workItemId} not found");

        item.AssignedToUserId = assignedToUserId;
        item.AssignedToName = assignedToName;
        item.Status = "InProgress";
        item.StartedAt ??= DateTime.UtcNow;
        item.UpdatedBy = updatedBy;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelWorkItemAsync(int tenantId, int workItemId, string cancelledBy, CancellationToken ct = default)
    {
        var item = await _db.WorkItems.FirstOrDefaultAsync(
            w => w.Id == workItemId && w.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"WorkItem {workItemId} not found");

        item.Status = "Cancelled";
        item.CompletedAt = DateTime.UtcNow;
        item.CompletedBy = cancelledBy;
        item.UpdatedBy = cancelledBy;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ── Process Pipeline ────────────────────────────────────────

    public async Task CreateProcessPipelineAsync(
        int tenantId, string processType, string? referenceType, int? referenceId,
        List<string> stepNames, string createdBy, CancellationToken ct = default)
    {
        for (var i = 0; i < stepNames.Count; i++)
        {
            var step = new ProcessStep
            {
                TenantId = tenantId,
                ProcessType = processType,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                StepName = stepNames[i],
                StepOrder = i + 1,
                Status = i == 0 ? "Active" : "Waiting",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
            _db.ProcessSteps.Add(step);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ProcessStep>> GetProcessStepsAsync(
        int tenantId, string processType, string? referenceType, int? referenceId,
        CancellationToken ct = default)
    {
        var query = _db.ProcessSteps
            .Where(s => s.TenantId == tenantId && s.ProcessType == processType);

        if (referenceType != null) query = query.Where(s => s.ReferenceType == referenceType);
        if (referenceId != null) query = query.Where(s => s.ReferenceId == referenceId);

        return await query.OrderBy(s => s.StepOrder).ToListAsync(ct);
    }

    public async Task AdvanceProcessAsync(
        int tenantId, string processType, string? referenceType, int? referenceId,
        string completedBy, string? notes, CancellationToken ct = default)
    {
        var steps = await GetProcessStepsAsync(tenantId, processType, referenceType, referenceId, ct);
        if (steps.Count == 0) return;

        var activeStep = steps.FirstOrDefault(s => s.Status == "Active");
        if (activeStep == null) return;

        activeStep.Status = "Completed";
        activeStep.CompletedAt = DateTime.UtcNow;
        activeStep.CompletedBy = completedBy;
        activeStep.Notes = notes;
        activeStep.UpdatedBy = completedBy;
        activeStep.UpdatedAt = DateTime.UtcNow;

        // Activate next step
        var nextStep = steps.FirstOrDefault(s => s.StepOrder == activeStep.StepOrder + 1);
        if (nextStep != null)
        {
            nextStep.Status = "Active";
            nextStep.UpdatedBy = completedBy;
            nextStep.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Dashboard ───────────────────────────────────────────────

    public async Task<object> GetWorkflowDashboardAsync(
        int tenantId, int? userId, string? department, CancellationToken ct = default)
    {
        var allItems = _db.WorkItems.Where(w => w.TenantId == tenantId);

        var pendingCount = await allItems.CountAsync(w => w.Status == "Pending", ct);
        var inProgressCount = await allItems.CountAsync(w => w.Status == "InProgress", ct);
        var completedTodayCount = await allItems.CountAsync(
            w => w.Status == "Completed" && w.CompletedAt != null && w.CompletedAt.Value.Date == DateTime.UtcNow.Date, ct);
        var overdueCount = await allItems.CountAsync(
            w => (w.Status == "Pending" || w.Status == "InProgress") && w.DueDate != null && w.DueDate < DateTime.UtcNow, ct);

        // Department breakdown
        var departmentStats = await allItems
            .Where(w => w.Status == "Pending" || w.Status == "InProgress")
            .Where(w => w.Department != null)
            .GroupBy(w => w.Department!)
            .Select(g => new { Department = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // My items (if userId provided)
        List<WorkItem>? myItems = null;
        if (userId.HasValue)
        {
            myItems = await GetMyWorkItemsAsync(tenantId, userId.Value, department, null, ct);
        }

        // Active process pipelines (recent 10)
        var activePipelines = await _db.ProcessSteps
            .Where(s => s.TenantId == tenantId && s.Status == "Active")
            .OrderByDescending(s => s.CreatedAt)
            .Take(10)
            .Select(s => new
            {
                s.ProcessType,
                s.ReferenceType,
                s.ReferenceId,
                s.StepName,
                s.StepOrder
            })
            .ToListAsync(ct);

        // Urgent items
        var urgentItems = await allItems
            .Where(w => w.Priority == "Urgent" && (w.Status == "Pending" || w.Status == "InProgress"))
            .OrderBy(w => w.DueDate)
            .Take(5)
            .ToListAsync(ct);

        return new
        {
            Summary = new { Pending = pendingCount, InProgress = inProgressCount, CompletedToday = completedTodayCount, Overdue = overdueCount },
            DepartmentStats = departmentStats,
            MyItems = myItems?.Select(w => new
            {
                w.Id, w.Module, w.WorkType, w.Title, w.Status, w.Priority,
                w.Department, w.DueDate, w.AiSuggestion, w.CanAutoComplete, w.ActionUrl
            }),
            ActivePipelines = activePipelines,
            UrgentItems = urgentItems.Select(w => new
            {
                w.Id, w.Title, w.Priority, w.Department, w.DueDate, w.AiSuggestion
            })
        };
    }

    public async Task<object> GetAiNextActionsAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var suggestions = new List<object>();

        // 1. Overdue items (highest priority)
        var overdueItems = await _db.WorkItems
            .Where(w => w.TenantId == tenantId
                        && (w.Status == "Pending" || w.Status == "InProgress")
                        && w.DueDate != null && w.DueDate < now)
            .OrderBy(w => w.DueDate)
            .Take(5)
            .ToListAsync(ct);

        foreach (var item in overdueItems)
        {
            suggestions.Add(new
            {
                Type = "Overdue",
                Priority = "Urgent",
                Message = $"[{item.Department}] {item.Title} - 기한 초과 ({item.DueDate:yyyy-MM-dd})",
                WorkItemId = item.Id,
                ActionUrl = item.ActionUrl,
                AiSuggestion = item.AiSuggestion ?? "즉시 처리가 필요합니다"
            });
        }

        // 2. Urgent pending items
        var urgentItems = await _db.WorkItems
            .Where(w => w.TenantId == tenantId
                        && w.Status == "Pending"
                        && w.Priority == "Urgent")
            .OrderBy(w => w.CreatedAt)
            .Take(3)
            .ToListAsync(ct);

        foreach (var item in urgentItems)
        {
            suggestions.Add(new
            {
                Type = "UrgentPending",
                Priority = "High",
                Message = $"[{item.Department}] {item.Title}",
                WorkItemId = item.Id,
                ActionUrl = item.ActionUrl,
                AiSuggestion = item.AiSuggestion ?? "긴급 작업입니다. 우선 처리해주세요"
            });
        }

        // 3. Auto-completable items
        var autoItems = await _db.WorkItems
            .Where(w => w.TenantId == tenantId
                        && w.Status == "Pending"
                        && w.CanAutoComplete)
            .OrderByDescending(w => w.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        if (autoItems.Count > 0)
        {
            suggestions.Add(new
            {
                Type = "AutoCompletable",
                Priority = "Normal",
                Message = $"자동 처리 가능한 작업이 {autoItems.Count}건 있습니다",
                WorkItemIds = autoItems.Select(w => w.Id).ToList(),
                AiSuggestion = "AI가 자동으로 처리할 수 있습니다. 일괄 승인하시겠습니까?"
            });
        }

        // 4. High-priority unassigned items
        var unassigned = await _db.WorkItems
            .Where(w => w.TenantId == tenantId
                        && w.Status == "Pending"
                        && w.AssignedToUserId == null
                        && (w.Priority == "Urgent" || w.Priority == "High"))
            .CountAsync(ct);

        if (unassigned > 0)
        {
            suggestions.Add(new
            {
                Type = "UnassignedHighPriority",
                Priority = "Normal",
                Message = $"미할당 긴급/높음 우선순위 작업이 {unassigned}건 있습니다",
                AiSuggestion = "담당자 배정이 필요합니다"
            });
        }

        // 5. Time-based suggestions
        if (now.Day >= 25)
        {
            var hasPayroll = await _db.WorkItems.AnyAsync(
                w => w.TenantId == tenantId && w.WorkType == "PayrollApprove"
                     && w.Status == "Pending" && w.CreatedAt.Month == now.Month, ct);

            if (!hasPayroll)
            {
                suggestions.Add(new
                {
                    Type = "TimeBased",
                    Priority = "Normal",
                    Message = "월말 급여 정산 처리를 확인해주세요",
                    AiSuggestion = "매월 25일 이후 급여 처리가 필요합니다"
                });
            }
        }

        // 6. Stale items (pending > 3 days)
        var staleCount = await _db.WorkItems
            .CountAsync(w => w.TenantId == tenantId
                             && w.Status == "Pending"
                             && w.CreatedAt < now.AddDays(-3), ct);

        if (staleCount > 0)
        {
            suggestions.Add(new
            {
                Type = "Stale",
                Priority = "Low",
                Message = $"3일 이상 미처리 작업이 {staleCount}건 있습니다",
                AiSuggestion = "오래된 작업을 정리하거나 우선순위를 재조정하세요"
            });
        }

        return new
        {
            GeneratedAt = now,
            TotalSuggestions = suggestions.Count,
            Suggestions = suggestions
        };
    }

    // ── Dynamic Routing ───────────────────────────────────────────

    /// <summary>
    /// Find the right person for a department task.
    /// Priority: Department specialist → Any TenantAdmin → null (AI handles or escalates)
    /// Works for all company sizes:
    /// - Enterprise: dedicated department staff found
    /// - Mid-size: some departments have staff, others fall through to admin
    /// - 1-person: always falls through to TenantAdmin (the owner)
    /// </summary>
    private async Task<int?> FindDepartmentOwnerAsync(int tenantId, string department, CancellationToken ct)
    {
        // 1. Find user with matching Department field (dedicated staff)
        var specialist = await _db.Users.IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.Department == department && u.IsActive)
            .OrderBy(u => u.Id)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);

        if (specialist != null) return specialist;

        // 2. Map department to related role keywords
        var relatedKeywords = department switch
        {
            "Warehouse" => new[] { "warehouse", "logistics", "wms", "물류", "창고" },
            "Accounting" => new[] { "accounting", "finance", "erp", "회계", "재무" },
            "CS" => new[] { "cs", "support", "crm", "고객", "서비스" },
            "Production" => new[] { "production", "manufacturing", "mes", "생산", "제조" },
            "Sales" => new[] { "sales", "commerce", "영업", "판매" },
            "Marketing" => new[] { "marketing", "마케팅", "홍보" },
            "HR" => new[] { "hr", "human", "인사", "급여" },
            _ => Array.Empty<string>()
        };

        // 3. Try to find anyone whose name/email/department contains keywords
        foreach (var keyword in relatedKeywords)
        {
            var match = await _db.Users.IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantId && u.IsActive && u.Role != "Member"
                    && (u.Name.ToLower().Contains(keyword) || (u.Department != null && u.Department.ToLower().Contains(keyword))))
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(ct);

            if (match != null) return match;
        }

        // 4. No specialist found — return null (caller will escalate to TenantAdmin)
        return null;
    }

    // ── Order Event Auto-Creator ────────────────────────────────

    public async Task CreateOrderWorkItemsAsync(
        int tenantId, int orderId, string orderNumber, decimal totalAmount, CancellationToken ct = default)
    {
        var createdBy = "system-workflow";
        var actionBase = $"/admin/orders/{orderId}";

        // 1. Warehouse - Picking confirmation
        await CreateWorkItemAsync(tenantId, "WMS", "PickingAssign",
            $"[{orderNumber}] 피킹 지시 확인",
            $"주문 {orderNumber}의 피킹 지시를 확인하고 담당자를 배정해주세요",
            "High", "Warehouse", null, "Order", orderId,
            actionBase, DateTime.UtcNow.AddHours(4),
            true, false, "재고 위치를 확인하고 피킹 리스트를 출력하세요",
            createdBy, ct);

        // 2. Warehouse - Packing & shipment
        await CreateWorkItemAsync(tenantId, "WMS", "PackingShipment",
            $"[{orderNumber}] 패킹 및 출하",
            $"주문 {orderNumber}의 상품을 패킹하고 출하 처리해주세요",
            "Normal", "Warehouse", null, "Order", orderId,
            actionBase, DateTime.UtcNow.AddHours(8),
            true, false, "피킹 완료 후 포장하고 송장번호를 등록하세요",
            createdBy, ct);

        // 3. Accounting - Revenue voucher confirmation
        await CreateWorkItemAsync(tenantId, "ERP", "RevenueVoucher",
            $"[{orderNumber}] 매출 전표 확인",
            $"주문 {orderNumber}의 매출 전표를 확인해주세요",
            "Normal", "Accounting", null, "Order", orderId,
            $"/admin/accounting/entries?orderId={orderId}", DateTime.UtcNow.AddDays(1),
            true, true, "결제 완료된 주문의 매출 전표를 자동 생성합니다",
            createdBy, ct);

        // 4. CS - Order confirmation notification
        await CreateWorkItemAsync(tenantId, "Shop", "OrderNotification",
            $"[{orderNumber}] 주문 확인 알림 발송",
            $"고객에게 주문 {orderNumber} 확인 알림을 발송해주세요",
            "Low", "CS", null, "Order", orderId,
            actionBase, DateTime.UtcNow.AddHours(1),
            true, true, "주문 확인 이메일/알림톡이 자동 발송됩니다",
            createdBy, ct);

        // 5. Sales - Shipping tracking registration
        await CreateWorkItemAsync(tenantId, "Shop", "ShippingTrack",
            $"[{orderNumber}] 배송 추적 등록",
            $"주문 {orderNumber}의 배송 추적 정보를 등록해주세요",
            "Normal", "Sales", null, "Order", orderId,
            actionBase, DateTime.UtcNow.AddDays(1),
            true, false, "택배사에서 송장번호를 받으면 등록하세요",
            createdBy, ct);

        // Create process pipeline
        await CreateProcessPipelineAsync(tenantId, "OrderFulfillment", "Order", orderId,
            new List<string> { "주문접수", "결제확인", "피킹", "패킹", "출하", "배송중", "배송완료" },
            createdBy, ct);

        // Auto-advance first 2 steps (order received + payment confirmed)
        await AdvanceProcessAsync(tenantId, "OrderFulfillment", "Order", orderId, createdBy, "주문 자동 접수", ct);
        await AdvanceProcessAsync(tenantId, "OrderFulfillment", "Order", orderId, createdBy, "결제 확인 완료", ct);

        _logger.LogInformation("Created 5 work items + pipeline for order {OrderNumber} (TenantId={TenantId})", orderNumber, tenantId);
    }
}
