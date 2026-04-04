using System.CommandLine;
using Shop.CLI.Commands;

var rootCommand = new RootCommand("SynDock CLI - 플랫폼 관리 도구")
{
    TenantCommands.Create(),
    DbCommands.Create(),
    UserCommands.Create(),
    HealthCommand.Create(),
    InfoCommand.Create()
};

return await rootCommand.InvokeAsync(args);
