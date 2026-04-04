using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class ProvisioningService : IProvisioningService
{
    private readonly IShopDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IServiceProvider _serviceProvider;

    public ProvisioningService(IShopDbContext db, IEmailService emailService, IServiceProvider serviceProvider)
    {
        _db = db;
        _emailService = emailService;
        _serviceProvider = serviceProvider;
    }

    public async Task<TenantApplication> SubmitApplicationAsync(string companyName, string desiredSlug, string email, string? phone, string contactName, string businessType, string planTier, string? businessDescription, string? websiteUrl, bool needsMes, bool needsWms, bool needsErp, bool needsCrm, string? additionalInfoJson, CancellationToken ct = default)
    {
        // Validate slug availability
        var slugExists = await _db.Tenants.AnyAsync(t => t.Slug == desiredSlug, ct);
        var appExists = await _db.TenantApplications.AnyAsync(a => a.DesiredSlug == desiredSlug && a.Status != "Rejected", ct);
        if (slugExists || appExists) throw new InvalidOperationException($"Slug '{desiredSlug}' is already taken");

        var application = new TenantApplication
        {
            CompanyName = companyName, DesiredSlug = desiredSlug, Email = email,
            Phone = phone, ContactName = contactName, BusinessType = businessType,
            PlanTier = planTier, BusinessDescription = businessDescription,
            WebsiteUrl = websiteUrl, NeedsMes = needsMes, NeedsWms = needsWms,
            NeedsErp = needsErp, NeedsCrm = needsCrm,
            AdditionalInfoJson = additionalInfoJson, CreatedBy = email
        };
        _db.TenantApplications.Add(application);
        await _db.SaveChangesAsync(ct);

        // Send confirmation email
        try
        {
            await _emailService.SendAsync(email, "SynDock 분양 신청 접수 확인",
                $"안녕하세요 {contactName}님,\n\n{companyName}의 SynDock 분양 신청이 접수되었습니다.\n신청번호: APP-{application.Id:D6}\n희망 URL: {desiredSlug}.syndock.com\n\n검토 후 결과를 안내드리겠습니다.\n\n감사합니다.\nSynDock 팀");
        }
        catch { /* Email failure should not block application */ }

        return application;
    }

    public async Task<TenantApplication?> GetApplicationAsync(int applicationId, CancellationToken ct = default)
        => await _db.TenantApplications.AsNoTracking().Include(a => a.ProvisionedTenant).FirstOrDefaultAsync(a => a.Id == applicationId, ct);

    public async Task<(List<TenantApplication> Items, int TotalCount)> GetApplicationsAsync(string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.TenantApplications.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task ApproveApplicationAsync(int applicationId, string adminNotes, string approvedBy, CancellationToken ct = default)
    {
        var app = await _db.TenantApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct) ?? throw new InvalidOperationException("Application not found");
        app.Status = "Approved";
        app.ApprovedAt = DateTime.UtcNow;
        app.AdminNotes = adminNotes;
        app.UpdatedBy = approvedBy;
        app.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        try
        {
            await _emailService.SendAsync(app.Email, "SynDock 분양 신청 승인",
                $"안녕하세요 {app.ContactName}님,\n\n{app.CompanyName}의 분양 신청이 승인되었습니다!\n곧 쇼핑몰 프로비저닝이 시작됩니다.\n\n감사합니다.\nSynDock 팀");
        }
        catch { }
    }

    public async Task RejectApplicationAsync(int applicationId, string rejectionReason, string rejectedBy, CancellationToken ct = default)
    {
        var app = await _db.TenantApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct) ?? throw new InvalidOperationException("Application not found");
        app.Status = "Rejected";
        app.RejectedAt = DateTime.UtcNow;
        app.RejectionReason = rejectionReason;
        app.UpdatedBy = rejectedBy;
        app.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Tenant> ProvisionTenantAsync(int applicationId, string provisionedBy, CancellationToken ct = default)
    {
        var app = await _db.TenantApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct) ?? throw new InvalidOperationException("Application not found");
        if (app.Status != "Approved") throw new InvalidOperationException("Application must be approved before provisioning");

        app.Status = "Provisioning";
        await _db.SaveChangesAsync(ct);

        // Create tenant
        var configJson = JsonSerializer.Serialize(new
        {
            theme = new { primaryColor = "#3B82F6", secondaryColor = "#1E40AF", fontFamily = "Inter, sans-serif" },
            features = new
            {
                mes = app.NeedsMes, wms = app.NeedsWms, erp = app.NeedsErp, crm = app.NeedsCrm,
                ai = app.PlanTier == "Pro" || app.PlanTier == "Enterprise",
                multiCurrency = app.PlanTier == "Pro" || app.PlanTier == "Enterprise"
            },
            company = new { name = app.CompanyName, email = app.Email, phone = app.Phone ?? "", website = app.WebsiteUrl ?? "" },
            plan = app.PlanTier
        });

        var tenant = new Tenant
        {
            Name = app.CompanyName,
            Slug = app.DesiredSlug,
            Subdomain = app.DesiredSlug,
            IsActive = true,
            ConfigJson = configJson,
            CreatedBy = provisionedBy
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        // Create admin user for the tenant
        var adminUser = new User
        {
            TenantId = tenant.Id,
            Username = $"admin@{app.DesiredSlug}",
            Email = app.Email,
            Name = app.ContactName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("changeme123!"),
            Role = "TenantAdmin",
            IsActive = true,
            EmailVerified = true,
            MustChangePassword = true,
            CreatedBy = provisionedBy
        };
        _db.Users.Add(adminUser);

        // Create default plan
        var commissionRate = app.PlanTier switch { "Free" => 5.0m, "Starter" => 3.0m, "Pro" => 1.0m, _ => 0.5m };
        var plan = new TenantPlan
        {
            TenantId = tenant.Id,
            PlanType = app.PlanTier,
            MonthlyPrice = app.PlanTier switch { "Free" => 0, "Starter" => 50000, "Pro" => 150000, _ => 500000 },
            BillingStatus = app.PlanTier == "Free" ? "Free" : "Trial",
            TrialEndsAt = app.PlanTier == "Free" ? null : DateTime.UtcNow.AddDays(14),
            CreatedBy = provisionedBy
        };
        _db.TenantPlans.Add(plan);

        // Create default categories
        var categories = new[] { "전체상품", "신상품", "베스트", "할인/이벤트" };
        foreach (var (catName, idx) in categories.Select((c, i) => (c, i)))
        {
            _db.Categories.Add(new Category { TenantId = tenant.Id, Name = catName, Slug = $"cat-{idx + 1}", SortOrder = idx + 1, IsActive = true, CreatedBy = provisionedBy });
        }

        // Create initial commission setting
        _db.CommissionSettings.Add(new CommissionSetting
        {
            TenantId = tenant.Id,
            CommissionRate = commissionRate,
            CreatedBy = provisionedBy
        });

        // If WMS needed, create default warehouse zone
        if (app.NeedsWms)
        {
            var zone = new WarehouseZone { TenantId = tenant.Id, Name = "기본 창고", Code = "MAIN", Type = "General", CreatedBy = provisionedBy };
            _db.WarehouseZones.Add(zone);
        }

        // If ERP needed, seed chart of accounts
        if (app.NeedsErp)
        {
            var accounts = new[]
            {
                ("1000", "자산", "Asset"), ("1100", "현금 및 예금", "Asset"), ("1200", "매출채권", "Asset"),
                ("2000", "부채", "Liability"), ("2100", "매입채무", "Liability"), ("2200", "미지급금", "Liability"),
                ("3000", "자본", "Equity"), ("3100", "자본금", "Equity"),
                ("4000", "수익", "Revenue"), ("4100", "매출", "Revenue"), ("4200", "기타수익", "Revenue"),
                ("5000", "비용", "Expense"), ("5100", "매출원가", "Expense"), ("5200", "인건비", "Expense"), ("5300", "임차료", "Expense")
            };
            foreach (var (code, name, type) in accounts)
            {
                _db.ChartOfAccounts.Add(new ChartOfAccount { TenantId = tenant.Id, AccountCode = code, Name = name, AccountType = type, CreatedBy = provisionedBy });
            }
        }

        await _db.SaveChangesAsync(ct);

        // Load registered seed data (MoHyun, Catholia, etc.) if available
        try
        {
            await Data.InitialDataSeeder.SeedTenantBySlugAsync(_serviceProvider, app.DesiredSlug);
        }
        catch (Exception) { /* Seed failure should not block provisioning */ }

        // Generate AI company homepage
        try
        {
            var homepageGen = _serviceProvider.GetService<IAiHomepageGenerator>();
            if (homepageGen != null)
            {
                await homepageGen.GenerateHomepageAsync(tenant.Id, app.CompanyName, app.BusinessType, app.BusinessDescription, ct);
            }
        }
        catch (Exception) { /* Homepage generation failure should not block provisioning */ }

        // Update application
        app.Status = "Active";
        app.ProvisionedTenantId = tenant.Id;
        app.ProvisionedAt = DateTime.UtcNow;
        app.UpdatedBy = provisionedBy;
        app.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Send welcome email
        try
        {
            await _emailService.SendAsync(app.Email, $"SynDock 쇼핑몰 개설 완료 - {app.CompanyName}",
                $"안녕하세요 {app.ContactName}님,\n\n{app.CompanyName} 쇼핑몰이 성공적으로 개설되었습니다!\n\n" +
                $"🔗 쇼핑몰 URL: https://{app.DesiredSlug}.syndock.com\n" +
                $"👤 관리자 ID: admin@{app.DesiredSlug}\n" +
                $"🔑 초기 비밀번호: changeme123!\n\n" +
                $"⚠️ 첫 로그인 후 반드시 비밀번호를 변경해주세요.\n\n" +
                (app.NeedsMes ? "✅ MES (제조실행시스템) 활성화됨\n" : "") +
                (app.NeedsWms ? "✅ WMS (창고관리시스템) 활성화됨\n" : "") +
                (app.NeedsErp ? "✅ ERP (전사자원관리) 활성화됨\n" : "") +
                (app.NeedsCrm ? "✅ CRM (고객관계관리) 활성화됨\n" : "") +
                $"\n감사합니다.\nSynDock 팀");
        }
        catch { }

        return tenant;
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, CancellationToken ct = default)
    {
        var tenantExists = await _db.Tenants.AnyAsync(t => t.Slug == slug, ct);
        var appExists = await _db.TenantApplications.AnyAsync(a => a.DesiredSlug == slug && a.Status != "Rejected", ct);
        return !tenantExists && !appExists;
    }
}
