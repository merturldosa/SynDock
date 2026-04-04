using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IProvisioningService
{
    Task<TenantApplication> SubmitApplicationAsync(string companyName, string desiredSlug, string email, string? phone, string contactName, string businessType, string planTier, string? businessDescription, string? websiteUrl, bool needsMes, bool needsWms, bool needsErp, bool needsCrm, string? additionalInfoJson, CancellationToken ct = default);
    Task<TenantApplication?> GetApplicationAsync(int applicationId, CancellationToken ct = default);
    Task<(List<TenantApplication> Items, int TotalCount)> GetApplicationsAsync(string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task ApproveApplicationAsync(int applicationId, string adminNotes, string approvedBy, CancellationToken ct = default);
    Task RejectApplicationAsync(int applicationId, string rejectionReason, string rejectedBy, CancellationToken ct = default);
    Task<Tenant> ProvisionTenantAsync(int applicationId, string provisionedBy, CancellationToken ct = default);
    Task<bool> IsSlugAvailableAsync(string slug, CancellationToken ct = default);
}
