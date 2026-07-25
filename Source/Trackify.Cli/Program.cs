using Microsoft.Extensions.Configuration;
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
using Log = Trackify.Cli.Log;

// Configuration comes from appsettings.json (next to the binary) + environment — nothing hardcoded.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// High-performance logging with a Serilog backend; levels + sinks are read from the "Serilog" section.
var serilog = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var storePath = Environment.GetEnvironmentVariable("TRACKIFY_STORE");

// Compose the layers — each owns its own DI (TRACKIFY_STORE overrides the store location).
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSerilog(serilog));
services.AddTrackifyDomain();
services.AddTrackifyApplication();
services.AddTrackifyInfrastructure(storePath);

var hostLogger = new SerilogLoggerFactory(serilog).CreateLogger("trackify");
Log.Started(hostLogger, storePath ?? SqliteTrainRepository.DefaultDatabasePath());

// No command → the dashboard (banner + saved trains + cheat-sheet).
// DependencyInjectionRegistrar (NuGet) bridges Spectre onto Microsoft.Extensions.DependencyInjection.
using var registrar = new DependencyInjectionRegistrar(services);
var app = new CommandApp<DashboardCommand>(registrar);
app.Configure(config =>
{
    config.SetApplicationName("trackify");

    // Global exception handling (ASP.NET-style): log every unhandled command error and surface a
    // clean message with a non-zero exit — never let one escape unlogged.
    config.SetExceptionHandler((ex, _) =>
    {
        Log.Unhandled(hostLogger, ex);
        AnsiConsole.MarkupLineInterpolated($"[red]✗ Error:[/] {ex.Message}");
        return 1;
    });

    config.AddCommand<DiscoverCommand>("discover").WithDescription("Scan for nearby hubs.").WithExample("discover", "--timeout", "15");
    config.AddCommand<ListCommand>("list").WithDescription("List saved trains.");
    config.AddCommand<ConnectCommand>("connect").WithDescription("Connect a train's hub (reachability test).").WithExample("connect", "\"Blauer Zug\"");
    config.AddCommand<DriveCommand>("drive").WithDescription("Run a train until Ctrl+C.").WithExample("drive", "\"Blauer Zug\"", "--speed", "40", "--color", "Green");
    config.AddCommand<StopCommand>("stop").WithDescription("Stop a train's motor.");
    config.AddCommand<ColorCommand>("color").WithDescription("Set a train's hub LED colour.").WithExample("color", "\"Blauer Zug\"", "Blue");
    config.AddCommand<AutoCommand>("auto").WithDescription("Auto-pilot: keep all saved trains running, re-scanning on an interval.").WithExample("auto", "--interval", "60");
    config.AddCommand<ServerCommand>("server").WithDescription("Run the REST + SignalR backend so the app can drive this Pi.").WithExample("server", "--urls", "http://0.0.0.0:5000");
});

// Ctrl+C (also systemd/docker SIGINT) cancels this token; commands react and shut down cleanly.
using var cancellation = ConsoleCancellation.CreateTokenSource();
return await app.RunAsync(args, cancellation.Token);
