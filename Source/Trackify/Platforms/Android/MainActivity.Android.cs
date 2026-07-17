using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Trackify.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    private const int BlePermissionRequestCode = 9001;
    private TaskCompletionSource<bool>? _blePermissionRequest;

    /// <summary>The running activity, used by the BLE permission handler to prompt the user.</summary>
    internal static MainActivity? Instance { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Instance = this;

        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);
    }

    /// <summary>
    /// Ensures the BLE runtime permissions are granted, prompting the user if needed. Returns once
    /// the user has responded (or immediately if already granted). Called on demand from the scan flow.
    /// </summary>
    internal Task<bool> EnsureBluetoothPermissionsAsync()
    {
        // Runtime permission prompts only exist on API 23+; earlier versions grant at install time.
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            return Task.FromResult(true);

        // Android 12+ uses the dedicated BLUETOOTH_SCAN/CONNECT runtime permissions; older versions
        // gate BLE scanning behind fine location instead.
        var required = OperatingSystem.IsAndroidVersionAtLeast(31)
            ? new[] { Android.Manifest.Permission.BluetoothScan, Android.Manifest.Permission.BluetoothConnect }
            : new[] { Android.Manifest.Permission.AccessFineLocation };

        // A plain loop (rather than LINQ) so the API-23 guard above statically covers the
        // CheckSelfPermission call for the platform-compatibility analyzer.
        var missing = new List<string>();
        foreach (var permission in required)
        {
            if (CheckSelfPermission(permission) != Permission.Granted)
                missing.Add(permission);
        }

        if (missing.Count == 0)
            return Task.FromResult(true);

        _blePermissionRequest = new TaskCompletionSource<bool>();
        RequestPermissions(missing.ToArray(), BlePermissionRequestCode);
        return _blePermissionRequest.Task;
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode != BlePermissionRequestCode)
            return;

        var granted = grantResults.Length > 0 && grantResults.All(r => r == Permission.Granted);
        _blePermissionRequest?.TrySetResult(granted);
        _blePermissionRequest = null;
    }
}
