using System.CommandLine;
using BepInEx.AssemblyPublicizer.Cli;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var rootCommand = new PublicizeCommand();
    return await rootCommand.InvokeAsync(args);
}
finally
{
    Log.CloseAndFlush();
}
