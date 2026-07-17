using System.Diagnostics.CodeAnalysis;
using Serilog;
using Trackify.Application;
#if __ANDROID__
using Trackify.Services;
#endif

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
                    RegisterLegoService(services);
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();
    }

    private static Serilog.Core.Logger CreateSerilogLogger()
        => new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

    // The platform ILegoService transports live in Trackify.Application (AddTrackifyApplication
    // filters them per platform in DI). The app only contributes what genuinely belongs to it:
    // the Android permission flow (it needs the Activity) and the no-BLE fallback for desktop/wasm.
    private static void RegisterLegoService(IServiceCollection services)
    {
#if __ANDROID__
        services.AddSingleton<IBluetoothPermissionService, AndroidBluetoothPermissionService>();
#elif !__IOS__ && !WINDOWS
        services.AddSingleton<ILegoService, UnsupportedLegoService>();
#endif
    }

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
