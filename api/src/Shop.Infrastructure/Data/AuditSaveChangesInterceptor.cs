using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shop.Domain.Entities;
using SynDock.Core.Entities;
using SynDock.Core.Interfaces;

namespace Shop.Infrastructure.Data;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    private static readonly HashSet<string> ExcludedEntities = new()
    {
        nameof(AuditLog),
        nameof(RefreshToken)
    };

    public AuditSaveChangesInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;
        var userId = _currentUserService.UserId;
        var username = _currentUserService.Username;
        var now = DateTime.UtcNow;

        var auditEntries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog)
                continue;

            var entityName = entry.Entity.GetType().Name;

            if (ExcludedEntities.Contains(entityName))
                continue;

            // Auto-set audit fields on BaseEntity
            if (entry.Entity is BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (string.IsNullOrEmpty(baseEntity.CreatedBy))
                            baseEntity.CreatedBy = username ?? "system";
                        baseEntity.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        baseEntity.UpdatedBy = username ?? "system";
                        baseEntity.UpdatedAt = now;
                        break;
                }
            }

            // Build audit log
            int entityId = 0;
            if (entry.Entity is BaseEntity be)
                entityId = be.Id;

            int? tenantId = null;
            if (entry.Entity is Domain.Interfaces.ITenantEntity te)
                tenantId = te.TenantId;

            switch (entry.State)
            {
                case EntityState.Added:
                    auditEntries.Add(new AuditLog
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        Action = "Create",
                        Changes = null, // full entity logged on first save
                        UserId = userId,
                        Username = username,
                        TenantId = tenantId,
                        Timestamp = now
                    });
                    break;

                case EntityState.Modified:
                    var changes = new Dictionary<string, object?>();
                    foreach (var prop in entry.Properties.Where(p => p.IsModified))
                    {
                        changes[prop.Metadata.Name] = new
                        {
                            old = prop.OriginalValue,
                            @new = prop.CurrentValue
                        };
                    }
                    if (changes.Count > 0)
                    {
                        auditEntries.Add(new AuditLog
                        {
                            EntityName = entityName,
                            EntityId = entityId,
                            Action = "Update",
                            Changes = JsonSerializer.Serialize(changes),
                            UserId = userId,
                            Username = username,
                            TenantId = tenantId,
                            Timestamp = now
                        });
                    }
                    break;

                case EntityState.Deleted:
                    auditEntries.Add(new AuditLog
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        Action = "Delete",
                        Changes = null,
                        UserId = userId,
                        Username = username,
                        TenantId = tenantId,
                        Timestamp = now
                    });
                    break;
            }
        }

        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
