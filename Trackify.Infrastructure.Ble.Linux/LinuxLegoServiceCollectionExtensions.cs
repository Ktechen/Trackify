using Microsoft.Extensions.DependencyInjection;
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Bluetooth;
using Trackify.Application.Lego;

namespace Trackify.Infrastructure.Ble.Linux;

/// <summary>Composition root helper for the Linux/BlueZ hub transport.</summary>
public static class LinuxLegoServiceCollectionExtensions
{
    /// <summary>Registers SharpBrick + the BlueZ adapter and exposes it as the Application <see cref="ILegoService"/> port.</summary>
    public static IServiceCollection AddLinuxLego(this IServiceCollection services)
    {
        services.AddPoweredUp();
        services.AddSingleton<IPoweredUpBluetoothAdapter, BlueZPoweredUpBluetoothAdapter>();
        services.AddSingleton<ILegoService, BlueZLegoService>();
        return services;
    }
}
