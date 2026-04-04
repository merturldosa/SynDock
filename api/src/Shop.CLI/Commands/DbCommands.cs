using System.CommandLine;
using Microsoft.EntityFrameworkCore;

namespace Shop.CLI.Commands;

public static class DbCommands
{
    public static Command Create()
    {
        var command = new Command("db", "데이터베이스 관리");

        command.AddCommand(CreateMigrateCommand());
        command.AddCommand(CreateStatusCommand());

        return command;
    }

    private static Command CreateMigrateCommand()
    {
        var cmd = new Command("migrate", "마이그레이션 적용");
        cmd.SetHandler(async () =>
        {
            Console.WriteLine("데이터베이스 마이그레이션 적용 중...");

            try
            {
                using var db = CliDbHelper.CreateDbContext();
                await db.Database.MigrateAsync();
                Console.WriteLine("마이그레이션 적용 완료.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"마이그레이션 실패: {ex.Message}");
            }
        });
        return cmd;
    }

    private static Command CreateStatusCommand()
    {
        var cmd = new Command("status", "마이그레이션 상태 확인");
        cmd.SetHandler(async () =>
        {
            try
            {
                using var db = CliDbHelper.CreateDbContext();

                var pending = await db.Database.GetPendingMigrationsAsync();
                var applied = await db.Database.GetAppliedMigrationsAsync();

                Console.WriteLine($"적용된 마이그레이션: {applied.Count()}개");
                foreach (var m in applied.TakeLast(5))
                    Console.WriteLine($"  [적용] {m}");

                var pendingList = pending.ToList();
                if (pendingList.Count > 0)
                {
                    Console.WriteLine($"\n대기 중 마이그레이션: {pendingList.Count}개");
                    foreach (var m in pendingList)
                        Console.WriteLine($"  [대기] {m}");
                }
                else
                {
                    Console.WriteLine("\n모든 마이그레이션이 적용되었습니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"상태 확인 실패: {ex.Message}");
            }
        });
        return cmd;
    }
}
