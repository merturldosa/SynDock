using System.CommandLine;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Shop.CLI.Commands;

public static class InfoCommand
{
    public static Command Create()
    {
        var cmd = new Command("info", "플랫폼 정보");

        cmd.SetHandler(async () =>
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            Console.WriteLine($@"
  ____              ____             _
 / ___| _   _ _ __ |  _ \  ___   ___| | __
 \___ \| | | | '_ \| | | |/ _ \ / __| |/ /
  ___) | |_| | | | | |_| | (_) | (__|   <
 |____/ \__, |_| |_|____/ \___/ \___|_|\_\
        |___/

 SynDock Platform CLI v{version}
 Runtime: .NET {Environment.Version}
 OS:      {Environment.OSVersion}
");

            try
            {
                await CliDbHelper.WithDb(async db =>
                {
                    var tenants = await db.Tenants.IgnoreQueryFilters().CountAsync();
                    var users = await db.Users.IgnoreQueryFilters().CountAsync();
                    var products = await db.Products.IgnoreQueryFilters().CountAsync();
                    var orders = await db.Orders.IgnoreQueryFilters().CountAsync();
                    var drivers = await db.DeliveryDrivers.IgnoreQueryFilters().CountAsync();

                    Console.WriteLine($" Platform Stats:");
                    Console.WriteLine($"   Tenants:   {tenants}");
                    Console.WriteLine($"   Users:     {users}");
                    Console.WriteLine($"   Products:  {products}");
                    Console.WriteLine($"   Orders:    {orders}");
                    Console.WriteLine($"   Drivers:   {drivers}");
                    Console.WriteLine();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" DB 연결 실패: {ex.Message[..Math.Min(60, ex.Message.Length)]}");
                Console.WriteLine(" SHOP_DB_CONNECTION 환경변수를 확인하세요.\n");
            }
        });

        return cmd;
    }
}
