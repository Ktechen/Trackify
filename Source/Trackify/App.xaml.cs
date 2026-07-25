using System.Diagnostics.CodeAnalysis;
using Serilog;
using Trackify.Application;
using Trackify.Infrastructure;
using Trackify.Services.Remote;

namespace Trackify;

public partial class App : Microsoft.UI.Xaml.Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // Global exception handling (ASP.NET-style): log every unhandled failure so nothing is
        // swallowed silently — UI, background threads, and unobserved tasks.
        Serilog.Log.Logger = CreateSerilogLogger();
        this.UnhandledException += (_, e) =>
        {
            Serilog.Log.Error(e.Exception, "Unhandled UI exception");
            e.Handled = true; // keep the app alive; the error is logged, not silently ignored
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Serilog.Log.Error(e.ExceptionObject as Exception, "Unhandled domain exception");
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Serilog.Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    [SuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                // High-performance logging with a Serilog backend.
                .ConfigureLogging(logging => logging.AddSerilog(CreateSerilogLogger(), dispose: true))
                .ConfigureServices((context, services) =>
                {
                    services.AddTrackifyDomain();
                    services.AddTrackifyApplication();
                    services.AddTrackifyInfrastructure();

                    // Server mode: with a backend URL configured, use the remote transport (REST +
                    // SignalR to a Pi) instead of the device's own Bluetooth — it wins as ILegoService
                    // because it's registered last — and enable syncing its trains into the local store.
                    var serverUrl = context.Configuration["AppConfig:ServerUrl"];
                    if (!string.IsNullOrWhiteSpace(serverUrl))
                    {
                        services.AddTrackifyRemote(serverUrl);
                        services.AddSingleton<RemoteTrainSync>();
                    }
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();
    }

    private static Serilog.Core.Logger CreateSerilogLogger()
        => new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<SecondPage, SecondViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault: true),
                    new("Second", View: views.FindByViewModel<SecondViewModel>()),
                ]
            )
        );
    }
}
