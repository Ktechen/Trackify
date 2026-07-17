using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Trackify.Application;
using Trackify.Cli.Commands;
using Spectre.Console.Cli.Extensions.DependencyInjection;
using Trackify.Cli.Extensions;
using Trackify.Domain;
using Trackify.Infrastructure;
using Trackify.Infrastructure.Persistence;

// High-performance logging with a Serilog backend (diagnostics go to the console at Information+).
var serilog = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var storePath = Environment.GetEnvironmentVariable("TRACKIFY_STORE");

// Compose the layers — each owns its own DI (TRACKIFY_STORE overrides the store location).
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSerilog(serilog));
services.AddTrackifyDomain();
services.AddTrackifyApplication();
services.AddTrackifyInfrastructure(storePath);

Trackify.Cli.Logging.Log.Started(
    new SerilogLoggerFactory(serilog).CreateLogger("trackify"),
    storePath ?? JsonTrainStore.DefaultPath());

// No command → the dashboard (banner + saved trains + cheat-sheet).
// DependencyInjectionRegistrar (NuGet) bridges Spectre onto Microsoft.Extensions.DependencyInjection.
using var registrar = new DependencyInjectionRegistrar(services);
var app = new CommandApp<DashboardCommand>(registrar);
app.Configure(config =>
{
    config.SetApplicationName("trackify");
    config.AddCommand<DiscoverCommand>("discover").WithDescription("Scan for nearby hubs.").WithExample("discover", "--timeout", "15");
    config.AddCommand<ListCommand>("list").WithDescription("List saved trains.");
    config.AddCommand<ConnectCommand>("connect").WithDescription("Connect a train's hub (reachability test).").WithExample("connect", "\"Blauer Zug\"");
    config.AddCommand<DriveCommand>("drive").WithDescription("Run a train until Ctrl+C.").WithExample("drive", "\"Blauer Zug\"", "--speed", "40", "--color", "Green");
    config.AddCommand<StopCommand>("stop").WithDescription("Stop a train's motor.");
    config.AddCommand<ColorCommand>("color").WithDescription("Set a train's hub LED colour.").WithExample("color", "\"Blauer Zug\"", "Blue");
});

// Ctrl+C (also systemd/docker SIGINT) cancels this token; commands react and shut down cleanly.
using var cancellation = ConsoleCancellation.CreateTokenSource();
return await app.RunAsync(args, cancellation.Token);
