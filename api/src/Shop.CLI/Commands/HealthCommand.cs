using System.CommandLine;
using Microsoft.EntityFrameworkCore;

namespace Shop.CLI.Commands;

public static class HealthCommand
{
    public static Command Create()
    {
        var cmd = new Command("health", "서비스 상태 점검");

        cmd.SetHandler(async () =>
        {
            Console.WriteLine("SynDock 서비스 상태 점검\n");

            // PostgreSQL
            await CheckDatabase();

            // Redis
            await CheckRedis();

            // MES API
            await CheckMesApi();
        });

        return cmd;
    }

    private static async Task CheckDatabase()
    {
        Console.Write("  [DB] PostgreSQL ... ");
        try
        {
            using var db = CliDbHelper.CreateDbContext();
            var canConnect = await db.Database.CanConnectAsync();
            if (canConnect)
            {
                var tenantCount = await db.Tenants.IgnoreQueryFilters().CountAsync();
                Console.WriteLine($"OK ({tenantCount} tenants)");
            }
            else
            {
                Console.WriteLine("FAIL (연결 불가)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL ({ex.Message})");
        }
    }

    private static async Task CheckRedis()
    {
        Console.Write("  [Cache] Redis ... ");
        try
        {
            var redisConn = Environment.GetEnvironmentVariable("SHOP_REDIS_CONNECTION") ?? "localhost:6381";
            using var redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(
                new StackExchange.Redis.ConfigurationOptions
                {
                    EndPoints = { redisConn },
                    ConnectTimeout = 3000,
                    AbortOnConnectFail = false
                });

            if (redis.IsConnected)
            {
                var server = redis.GetServer(redis.GetEndPoints()[0]);
                var info = await server.InfoAsync("memory");
                Console.WriteLine("OK");
            }
            else
            {
                Console.WriteLine("FAIL (연결 불가)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SKIP ({ex.Message[..Math.Min(50, ex.Message.Length)]})");
        }
    }

    private static async Task CheckMesApi()
    {
        Console.Write("  [MES] API ... ");
        try
        {
            var mesUrl = Environment.GetEnvironmentVariable("MES_API_URL") ?? "http://localhost:8080";
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetAsync($"{mesUrl}/actuator/health");

            if (response.IsSuccessStatusCode)
                Console.WriteLine("OK");
            else
                Console.WriteLine($"WARN (status: {(int)response.StatusCode})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SKIP ({ex.Message[..Math.Min(50, ex.Message.Length)]})");
        }
    }
}
