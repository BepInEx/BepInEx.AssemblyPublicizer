using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using BepInEx.AssemblyPublicizer.Cli;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    return await new CommandLineBuilder(new PublicizeCommand())
        .UseDefaults()
        .UseExceptionHandler((ex, _) => Log.Fatal(ex, "Exception, cannot continue!"), -1)
        .Build()
        .InvokeAsync(args);
}
finally
{
    Log.CloseAndFlush();
}
