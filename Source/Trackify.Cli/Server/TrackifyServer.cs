using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Trackify.Application;
using Trackify.Application.Lego;
using Trackify.Application.Remote;
using Trackify.Application.Trains;
using Trackify.Domain;
using Trackify.Domain.Enums;
using Trackify.Infrastructure;

namespace Trackify.Cli.Server;

/// <summary>
/// Hosts the Trackify backend — a REST API (one-shot actions) plus a SignalR hub (real-time speed/LED)
/// over the very same Domain/Application/Infrastructure use-cases the CLI runs. Started by
/// <c>trackify serve</c>; on a Pi this owns the BlueZ radio and the app connects to it remotely.
/// </summary>
internal static class TrackifyServer
{
    public static async Task<int> RunAsync(string[] args, string? storePath, CancellationToken cancellationToken)
    {
        // Content root = the binary's dir so appsettings.json (copied next to trackify) is found even
        // when launched from another working directory (systemd/Docker).
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });

        // Everything comes from appsettings.json / env / args — nothing hardcoded. Kestrel binds the
        // "Urls" key automatically; Serilog levels + sinks are read from the "Serilog" section.
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger(), dispose: true);

        builder.Services.AddTrackifyDomain();
        builder.Services.AddTrackifyApplication();
        builder.Services.AddTrackifyInfrastructure(storePath);
        builder.Services.AddTrackifyServer();

        var app = builder.Build();

        // Global exception handling (ASP.NET-style): log every unhandled request error and return a
        // clean 500 JSON — no failure escapes unlogged.
        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("trackify.serve");
            logger.LogError(error, "Unhandled request exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = error?.Message ?? "Unexpected error" });
        }));

        app.UseCors();
        MapApi(app, app.Configuration.GetValue("Trackify:Server:DiscoverTimeoutSeconds", 20));
        app.MapHub<TrainHub>(ApiRoutes.TrainHub);

        Log.ServerStarting(app.Logger, storePath ?? "(default)");
        await app.RunAsync(cancellationToken);
        return 0;
    }

    private static void MapApi(WebApplication app, int discoverTimeoutSeconds)
    {
        // Train list — the app syncs this into its local SQLite store.
        app.MapGet(ApiRoutes.Trains, (ITrainService trains, CancellationToken ct) => trains.GetAllAsync(ct));

        app.MapPost(ApiRoutes.Discover, async (ILegoService lego, CancellationToken ct) =>
        {
            // Discovery has no fixed window (stops at the first hub); cap it so a REST call can't hang.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(discoverTimeoutSeconds));
            return await lego.DiscoverAsync(timeout.Token);
        });

        app.MapPost(ApiRoutes.Connect, async (string hubId, HubType hubType, ILegoService lego, CancellationToken ct) =>
        {
            await lego.ConnectAsync(hubId, hubType, ct);
            return Results.Ok();
        });

        app.MapPost(ApiRoutes.Disconnect, async (string hubId, ILegoService lego, CancellationToken ct) =>
        {
            await lego.DisconnectAsync(hubId, ct);
            return Results.Ok();
        });

        app.MapGet(ApiRoutes.State, (TrainStateStore state) => state.Snapshot());
    }
}
