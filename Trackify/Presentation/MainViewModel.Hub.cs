using System.Collections.ObjectModel;
using System.Globalization;
using Trackify.Models.Trains;
using Trackify.Models.Trains.Enums;
using Trackify.Services;
using Train = Trackify.Models.Trains.Train;

namespace Trackify.Presentation;

/// <summary>
/// Hub (Bluetooth) control for the main screen: discovering hubs, connecting the selected train's
/// hub, and pushing live speed/LED commands. Talks only to <see cref="ILegoService"/>, so it is
/// unaware of whether that runs on-device BLE or anything else.
/// </summary>
public partial class MainViewModel
{
    private readonly ILegoService _lego;
    private CancellationTokenSource? _speedDebounceCts;
    private CancellationTokenSource? _discoverCts;

    [ObservableProperty] private bool isDiscovering;
    [ObservableProperty] private string? discoverStatus;
    // Non-null while the "add this hub?" popup is showing; the found hub awaiting confirmation.
    [ObservableProperty] private DiscoveredHub? pendingHub;

    public ObservableCollection<DiscoveredHub> DiscoveredHubs { get; } = [];

    // Scans until a hub turns up or the user stops it (StopDiscover); then connects to it
    // automatically - no separate "tap the hub, then Verbinden" step.
    private async Task DiscoverAsync()
    {
        if (!_lego.IsSupported)
        {
            DiscoverStatus = "Bluetooth wird auf dieser Plattform nicht unterstützt.";
            return;
        }

        _discoverCts?.Cancel();
        _discoverCts = new CancellationTokenSource();

        IsDiscovering = true;
        DiscoveredHubs.Clear();
        DiscoverStatus = "Suche läuft… (Stopp zum Abbrechen)";

        IReadOnlyList<DiscoveredHub> hubs;
        try
        {
            hubs = await _lego.DiscoverAsync(_discoverCts.Token);
        }
        catch (Exception ex)
        {
            DiscoverStatus = $"Fehler: {ex.Message}";
            return;
        }
        finally
        {
            IsDiscovering = false;
        }

        if (hubs.Count == 0)
        {
            DiscoverStatus = "Suche abgebrochen — kein Hub gefunden.";
            return;
        }

        // A hub turned up - show the styled confirmation popup (PendingHub drives the overlay).
        DiscoverStatus = null;
        PendingHub = hubs[0];
    }

    // Popup "Verbinden": adopt the found hub as a train and connect right away.
    private async Task ConfirmAddHubAsync()
    {
        if (PendingHub is not { } hub) return;

        PendingHub = null; // dismiss the popup immediately
        DiscoverStatus = null;
        UseDiscoveredHub(hub);           // add + select the train
        await ConnectSelectedTrainAsync(); // and open the connection
    }

    private void CancelAddHub()
    {
        PendingHub = null;
        DiscoverStatus = "Abgebrochen.";
    }

    private void StopDiscover() => _discoverCts?.Cancel();

    private void ClearDiscoverResults()
    {
        DiscoveredHubs.Clear();
        DiscoverStatus = null;
    }

    private void UseDiscoveredHub(DiscoveredHub? hub)
    {
        if (hub is null) return;

        // Bind the discovered hub to an existing train for the same device, or create a new one.
        var existing = Trains.FirstOrDefault(t =>
            string.Equals(t.HubId, hub.Id, StringComparison.OrdinalIgnoreCase) ||
            (hub.MacAddress is not null && string.Equals(t.BleAddress, hub.MacAddress, StringComparison.OrdinalIgnoreCase)));

        if (existing is not null)
        {
            existing.HubId = hub.Id;
            SelectedTrain = existing;
        }
        else
        {
            var train = new Train
            {
                Id = $"trn-{_sequence++}",
                Name = string.IsNullOrWhiteSpace(hub.Name) ? "Entdeckter Hub" : hub.Name!,
                Hub = hub.HubType ?? HubType.PoweredUpHub,
                HubId = hub.Id,
                BleAddress = hub.MacAddress ?? hub.Id,
                Color = LedColorType.Green, PortA = DeviceType.TrainMotor, PortB = DeviceType.None, Speed = 0,
                AccelFn = SpeedFunctionType.EaseOut, BrakeFn = SpeedFunctionType.EaseIn, IsActive = true,
            };
            Trains.Add(train);
            SelectedTrain = train;
        }

        Filter = TrainFilterType.All;
        Search = "";
        ClearDiscoverResults();
        ApplyFilter();
    }

    private async Task ConnectSelectedTrainAsync()
    {
        if (SelectedTrain is not { } train) return;

        var hubKey = HubKey(train);
        if (string.IsNullOrWhiteSpace(hubKey))
        {
            train.ConnectionStatus = "Kein Hub gewählt — bitte zuerst „Hub suchen“.";
            return;
        }

        train.ConnectionStatus = "Verbinde…";
        try
        {
            await _lego.ConnectAsync(hubKey, train.Hub);
            train.IsHardwareConnected = true;
            train.ConnectionStatus = "Verbunden";
        }
        catch (Exception ex)
        {
            train.IsHardwareConnected = false;
            train.ConnectionStatus = $"Fehler: {ex.Message}";
        }

        NotifyHubCommandsCanExecute();
    }

    private async Task DisconnectSelectedTrainAsync()
    {
        if (SelectedTrain is not { } train) return;

        try { await _lego.DisconnectAsync(HubKey(train)); }
        catch { /* best effort - the local flag below is what the UI reflects either way */ }

        train.IsHardwareConnected = false;
        train.ConnectionStatus = "Getrennt";

        NotifyHubCommandsCanExecute();
    }

    private void NotifyHubCommandsCanExecute()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
    }

    // Reacts to changes on the selected train that should reach the hardware while connected:
    // the connection flag flips the commands, the slider drives the motor, the color drives the LED.
    private void OnSelectedTrainHubPropertyChanged(Train train, string? propertyName)
    {
        if (propertyName == nameof(Train.IsHardwareConnected))
            NotifyHubCommandsCanExecute();

        if (!train.IsHardwareConnected) return;

        if (propertyName == nameof(Train.Speed)) DebounceSendSpeed(train);
        if (propertyName == nameof(Train.Color)) _ = SendLedAsync(train);
    }

    // The slider fires rapidly while dragging; only send the last value after a short pause.
    private void DebounceSendSpeed(Train train)
    {
        _speedDebounceCts?.Cancel();
        var cts = _speedDebounceCts = new CancellationTokenSource();
        _ = SendSpeedDebouncedAsync(train, cts.Token);
    }

    private async Task SendSpeedDebouncedAsync(Train train, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        var hubKey = HubKey(train);
        if (string.IsNullOrWhiteSpace(hubKey)) return;

        try
        {
            // Port A always drives the motor bound to the single "Standard-Geschwindigkeit" slider.
            await _lego.SetSpeedAsync(hubKey, 0, (sbyte)Math.Clamp(train.Speed, -100, 100));
        }
        catch
        {
            // Best-effort live control - a dropped command just means the next slider move resends it.
        }
    }

    private async Task SendLedAsync(Train train)
    {
        var hubKey = HubKey(train);
        if (string.IsNullOrWhiteSpace(hubKey)) return;

        var (r, g, b) = ParseHexColor(LegoinoCatalog.Color(train.Color).Hex);

        try
        {
            await _lego.SetLedAsync(hubKey, r, g, b);
        }
        catch
        {
            // Best-effort live control - a dropped command just means the next color pick resends it.
        }
    }

    // The key a hub is addressed by: the platform device id captured during discovery, or the
    // typed-in BLE address as a fallback (Android accepts a MAC string here; iOS needs discovery).
    private static string HubKey(Train train)
        => !string.IsNullOrWhiteSpace(train.HubId) ? train.HubId : train.BleAddress;

    private static (byte R, byte G, byte B) ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        return (
            byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
