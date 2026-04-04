using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using Shop.Infrastructure.Data;
using Shop.Infrastructure.Services;

namespace Shop.CLI.Commands;

public static class CliDbHelper
{
    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("SHOP_DB_CONNECTION")
            ?? "Host=localhost;Port=5434;Database=syndock_shop;Username=postgres;Password=postgres";
    }

    public static ShopDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShopDbContext>();
        optionsBuilder.UseNpgsql(GetConnectionString());

        var tenantContext = new TenantContext();
        return new ShopDbContext(optionsBuilder.Options, tenantContext);
    }

    public static async Task<T> WithDb<T>(Func<ShopDbContext, Task<T>> action)
    {
        using var db = CreateDbContext();
        return await action(db);
    }

    public static async Task WithDb(Func<ShopDbContext, Task> action)
    {
        using var db = CreateDbContext();
        await action(db);
    }
}
