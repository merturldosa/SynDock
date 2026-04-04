using System.CommandLine;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;

namespace Shop.CLI.Commands;

public static class UserCommands
{
    public static Command Create()
    {
        var command = new Command("user", "사용자 관리");

        command.AddCommand(CreateAddCommand());
        command.AddCommand(CreateResetPasswordCommand());
        command.AddCommand(CreateListCommand());

        return command;
    }

    private static Command CreateAddCommand()
    {
        var emailArg = new Argument<string>("email", "이메일 주소");
        var roleArg = new Argument<string>("role", "역할 (Member, Driver, TenantAdmin, Admin, PlatformAdmin)");
        var tenantOpt = new Option<string>("--tenant", "테넌트 slug") { IsRequired = true };

        var cmd = new Command("create", "새 사용자 생성") { emailArg, roleArg, tenantOpt };

        cmd.SetHandler(async (string email, string role, string tenantSlug) =>
        {
            await CliDbHelper.WithDb(async db =>
            {
                var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Slug == tenantSlug);
                if (tenant is null)
                {
                    Console.WriteLine($"테넌트 '{tenantSlug}'을(를) 찾을 수 없습니다.");
                    return;
                }

                var exists = await db.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == email && u.TenantId == tenant.Id);
                if (exists)
                {
                    Console.WriteLine($"사용자 '{email}'이(가) 이미 존재합니다.");
                    return;
                }

                var tempPassword = GeneratePassword();
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                var user = new User
                {
                    TenantId = tenant.Id,
                    Email = email,
                    Username = email.Split('@')[0],
                    Name = email.Split('@')[0],
                    PasswordHash = passwordHash,
                    Role = role,
                    IsActive = true,
                    EmailVerified = true,
                    CreatedBy = "cli"
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                Console.WriteLine($"사용자 생성 완료:");
                Console.WriteLine($"  Email:    {email}");
                Console.WriteLine($"  Role:     {role}");
                Console.WriteLine($"  Tenant:   {tenantSlug}");
                Console.WriteLine($"  Password: {tempPassword}");
                Console.WriteLine($"\n비밀번호를 안전하게 전달하고, 사용자에게 변경을 권장하세요.");
            });
        }, emailArg, roleArg, tenantOpt);
        return cmd;
    }

    private static Command CreateResetPasswordCommand()
    {
        var emailArg = new Argument<string>("email", "이메일 주소");
        var tenantOpt = new Option<string>("--tenant", "테넌트 slug") { IsRequired = true };

        var cmd = new Command("reset-password", "비밀번호 재설정") { emailArg, tenantOpt };

        cmd.SetHandler(async (string email, string tenantSlug) =>
        {
            await CliDbHelper.WithDb(async db =>
            {
                var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Slug == tenantSlug);
                if (tenant is null)
                {
                    Console.WriteLine($"테넌트 '{tenantSlug}'을(를) 찾을 수 없습니다.");
                    return;
                }

                var user = await db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == email && u.TenantId == tenant.Id);

                if (user is null)
                {
                    Console.WriteLine($"사용자 '{email}'을(를) 찾을 수 없습니다.");
                    return;
                }

                var newPassword = GeneratePassword();
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedBy = "cli";
                user.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                Console.WriteLine($"비밀번호 재설정 완료:");
                Console.WriteLine($"  Email:        {email}");
                Console.WriteLine($"  New Password: {newPassword}");
            });
        }, emailArg, tenantOpt);
        return cmd;
    }

    private static Command CreateListCommand()
    {
        var tenantOpt = new Option<string>("--tenant", "테넌트 slug") { IsRequired = true };
        var cmd = new Command("list", "사용자 목록") { tenantOpt };

        cmd.SetHandler(async (string tenantSlug) =>
        {
            await CliDbHelper.WithDb(async db =>
            {
                var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Slug == tenantSlug);
                if (tenant is null)
                {
                    Console.WriteLine($"테넌트 '{tenantSlug}'을(를) 찾을 수 없습니다.");
                    return;
                }

                var users = await db.Users.IgnoreQueryFilters()
                    .Where(u => u.TenantId == tenant.Id)
                    .OrderBy(u => u.Id)
                    .Select(u => new { u.Id, u.Email, u.Username, u.Role, u.IsActive })
                    .ToListAsync();

                Console.WriteLine($"{"ID",-5} {"Email",-30} {"Username",-15} {"Role",-15} {"Active"}");
                Console.WriteLine(new string('-', 75));

                foreach (var u in users)
                    Console.WriteLine($"{u.Id,-5} {u.Email,-30} {u.Username,-15} {u.Role,-15} {(u.IsActive ? "Yes" : "No")}");

                Console.WriteLine($"\n총 {users.Count}명");
            });
        }, tenantOpt);
        return cmd;
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
        var bytes = RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
