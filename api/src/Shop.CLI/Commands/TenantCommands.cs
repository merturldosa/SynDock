using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;

namespace Shop.CLI.Commands;

public static class TenantCommands
{
    public static Command Create()
    {
        var command = new Command("tenant", "테넌트 관리");

        command.AddCommand(CreateListCommand());
        command.AddCommand(CreateInfoCommand());
        command.AddCommand(CreateAddCommand());
        command.AddCommand(CreateSeedCommand());

        return command;
    }

    private static Command CreateListCommand()
    {
        var cmd = new Command("list", "테넌트 목록 조회");
        cmd.SetHandler(async () =>
        {
            await CliDbHelper.WithDb(async db =>
            {
                var tenants = await db.Tenants
                    .IgnoreQueryFilters()
                    .OrderBy(t => t.Id)
                    .Select(t => new { t.Id, t.Slug, t.Name, t.IsActive, t.CustomDomain })
                    .ToListAsync();

                Console.WriteLine($"{"ID",-5} {"Slug",-15} {"Name",-25} {"Active",-8} {"Domain"}");
                Console.WriteLine(new string('-', 75));

                foreach (var t in tenants)
                {
                    Console.WriteLine($"{t.Id,-5} {t.Slug,-15} {t.Name,-25} {(t.IsActive ? "Yes" : "No"),-8} {t.CustomDomain ?? "-"}");
                }

                Console.WriteLine($"\n총 {tenants.Count}개 테넌트");
            });
        });
        return cmd;
    }

    private static Command CreateInfoCommand()
    {
        var slugArg = new Argument<string>("slug", "테넌트 slug");
        var cmd = new Command("info", "테넌트 상세 정보") { slugArg };

        cmd.SetHandler(async (string slug) =>
        {
            await CliDbHelper.WithDb(async db =>
            {
                var tenant = await db.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == slug);

                if (tenant is null)
                {
                    Console.WriteLine($"테넌트 '{slug}'을(를) 찾을 수 없습니다.");
                    return;
                }

                var userCount = await db.Users.IgnoreQueryFilters().CountAsync(u => u.TenantId == tenant.Id);
                var productCount = await db.Products.IgnoreQueryFilters().CountAsync(p => p.TenantId == tenant.Id);
                var orderCount = await db.Orders.IgnoreQueryFilters().CountAsync(o => o.TenantId == tenant.Id);

                Console.WriteLine($"테넌트 정보: {tenant.Name} ({tenant.Slug})");
                Console.WriteLine($"  ID:          {tenant.Id}");
                Console.WriteLine($"  Active:      {tenant.IsActive}");
                Console.WriteLine($"  Domain:      {tenant.CustomDomain ?? "-"}");
                Console.WriteLine($"  Subdomain:   {tenant.Subdomain ?? "-"}");
                Console.WriteLine($"  Users:       {userCount}");
                Console.WriteLine($"  Products:    {productCount}");
                Console.WriteLine($"  Orders:      {orderCount}");
                Console.WriteLine($"  Created:     {tenant.CreatedAt:yyyy-MM-dd HH:mm}");
            });
        }, slugArg);
        return cmd;
    }

    private static Command CreateAddCommand()
    {
        var slugArg = new Argument<string>("slug", "테넌트 slug");
        var nameArg = new Argument<string>("name", "테넌트 이름");
        var cmd = new Command("create", "새 테넌트 생성") { slugArg, nameArg };

        cmd.SetHandler(async (string slug, string name) =>
        {
            await CliDbHelper.WithDb(async db =>
            {
                var exists = await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == slug);
                if (exists)
                {
                    Console.WriteLine($"테넌트 '{slug}'이(가) 이미 존재합니다.");
                    return;
                }

                var tenant = new Tenant
                {
                    Slug = slug,
                    Name = name,
                    IsActive = true,
                    CreatedBy = "cli"
                };

                db.Tenants.Add(tenant);
                await db.SaveChangesAsync();

                Console.WriteLine($"테넌트 생성 완료: {name} (slug: {slug}, id: {tenant.Id})");
            });
        }, slugArg, nameArg);
        return cmd;
    }

    private static Command CreateSeedCommand()
    {
        var slugArg = new Argument<string>("slug", "테넌트 slug (catholia, mohyun)");
        var cmd = new Command("seed", "테넌트 시드 데이터 적용") { slugArg };

        cmd.SetHandler(async (string slug) =>
        {
            Console.WriteLine($"테넌트 '{slug}' 시드 데이터 적용은 API를 통해 실행해 주세요:");
            Console.WriteLine($"  POST /api/platform/tenants/{slug}/seed");
            Console.WriteLine($"  또는 Swagger UI에서 실행");
        }, slugArg);
        return cmd;
    }
}
