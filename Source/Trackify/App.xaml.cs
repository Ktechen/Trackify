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
                    services.AddTrackifyApplication();  // registers the platform (local) ILegoService, if any
                    services.AddTrackifyInfrastructure();

                    // Live mode switch: ConnectionState (seeded from config) drives a SwitchingLegoService
                    // that routes each call to the local Bluetooth transport or the remote backend — so
                    // Direct/Server can toggle at runtime. RemoteTrainSync pulls trains into local SQLite.
                    var defaultUrl = context.Configuration["AppConfig:ServerUrl"];
                    services.AddSingleton(sp => new ConnectionState(sp.GetRequiredService<ILogger<ConnectionState>>(), defaultUrl));
                    services.AddSingleton<RemoteTrainSync>();

                    var localDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(ILegoService));
                    services.AddSingleton<ILegoService>(sp => new SwitchingLegoService(
                        sp.GetRequiredService<ConnectionState>(),
                        ResolveLocal(sp, localDescriptor)));
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
            // Keep the overview clean: framework INFO noise (e.g. Uno's optional appsettings probes,
            // Microsoft host chatter) is downgraded to warnings; Trackify's own logs stay at Information.
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Uno", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .CreateLogger();

    // Builds the platform (local Bluetooth) ILegoService from its original registration, so the
    // SwitchingLegoService can wrap it without the two competing for the ILegoService resolution.
    private static ILegoService? ResolveLocal(IServiceProvider services, ServiceDescriptor? descriptor) => descriptor switch
    {
        { ImplementationInstance: ILegoService instance } => instance,
        { ImplementationFactory: { } factory } => (ILegoService)factory(services),
        { ImplementationType: { } type } => (ILegoService)ActivatorUtilities.CreateInstance(services, type),
        _ => null,
    };

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
