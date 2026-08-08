using System.Globalization;
using Trackify.Models.Trains;
using Train = Trackify.Models.Trains.Train;

namespace Trackify.Presentation.ViewModels;

/// <summary>
/// Hub (Bluetooth) control for the main screen: discovering a hub, connecting the selected train's
/// hub, and pushing live speed/LED commands. Talks only to <see cref="ILegoService"/>, so it is
/// unaware of whether that runs on-device BLE or anything else.
/// </summary>
public partial class MainViewModel
{
    private const byte MotorPort = 0; // Port A - the motor driven by the speed slider.

    private readonly ILegoService _lego;
    private CancellationTokenSource? _speedDebounceCts;
    private CancellationTokenSource? _discoverCts;

    [ObservableProperty] private bool isDiscovering;
    [ObservableProperty] private string? discoverStatus;
    // Non-null while the "add this hub?" popup is showing: the found hub awaiting confirmation.
    [ObservableProperty] private DiscoveredHubDto? pendingHub;

    // Scans until a hub turns up or the user stops it, then offers it for confirmation (PendingHub).
    private async Task DiscoverAsync()
    {
        if (!_lego.IsSupported)
        {
            DiscoverStatus = "Bluetooth wird auf dieser Plattform nicht unterstützt.";
            return;
        }

        // Own the source locally and dispose it in the finally, so it is released exactly once by the
        // scan that created it — cancelling a previous scan lets that one clean up after itself.
        var cts = new CancellationTokenSource();
        if (_discoverCts is not null) await _discoverCts.CancelAsync();
        _discoverCts = cts;

        IsDiscovering = true;
        DiscoverStatus = "Suche läuft… (Stopp zum Abbrechen)";
        Log.DiscoverStarted(_log);
        try
        {
            var hubs = await _lego.DiscoverAsync(cts.Token);
            if (hubs.Count == 0)
            {
                DiscoverStatus = "Suche abgebrochen — kein Hub gefunden.";
                return;
            }

            DiscoverStatus = null;
            PendingHub = hubs[0]; // shows the styled confirmation popup
        }
        catch (Exception ex)
        {
            DiscoverStatus = $"Fehler: {ex.Message}";
        }
        finally
        {
            IsDiscovering = false;
            if (ReferenceEquals(_discoverCts, cts)) _discoverCts = null;
            cts.Dispose();
        }
    }

    private void StopDiscover() => _discoverCts?.Cancel();

    private void HideDiscoverStatus() => DiscoverStatus = null;

    // Popup "Verbinden": adopt the found hub as a train and connect right away.
    private async Task ConfirmAddHubAsync()
    {
        if (PendingHub is not { } hub) return;

        PendingHub = null;
        AdoptHubAsTrain(hub);
        await ConnectSelectedTrainAsync();
    }

    private void CancelAddHub()
    {
        PendingHub = null;
        DiscoverStatus = "Abgebrochen.";
    }

    // Binds the hub to a matching train (or creates one) and selects it.
    private void AdoptHubAsTrain(DiscoveredHubDto hubDtoDto)
    {
        var train = Trains.FirstOrDefault(t => IsSameDevice(t, hubDtoDto));
        if (train is not null)
        {
            train.HubId = hubDtoDto.Id;
        }
        else
        {
            train = CreateTrainFor(hubDtoDto);
            Trains.Add(train);
        }

        SelectAfresh(train);
    }

    private static bool IsSameDevice(Train train, DiscoveredHubDto hubDtoDto)
        => string.Equals(train.HubId, hubDtoDto.Id, StringComparison.OrdinalIgnoreCase)
        || (hubDtoDto.MacAddress is not null && string.Equals(train.BleAddress, hubDtoDto.MacAddress, StringComparison.OrdinalIgnoreCase));

    private Train CreateTrainFor(DiscoveredHubDto hubDtoDto) => new()
    {
        Id = $"trn-{_sequence++}",
        Name = string.IsNullOrWhiteSpace(hubDtoDto.Name) ? "Entdeckter Hub" : hubDtoDto.Name!,
        Hub = hubDtoDto.HubType ?? HubType.PoweredUpHub,
        HubId = hubDtoDto.Id,
        BleAddress = hubDtoDto.MacAddress ?? hubDtoDto.Id,
        Color = LedColorType.Green, PortA = DeviceType.TrainMotor, PortB = DeviceType.None, Speed = 0,
        AccelFn = SpeedFunctionType.EaseOut, BrakeFn = SpeedFunctionType.EaseIn, IsActive = true,
    };

    private async Task ConnectSelectedTrainAsync()
    {
        if (SelectedTrain is not { } train) return;

        if (string.IsNullOrWhiteSpace(HubKey(train)))
        {
            train.ConnectionStatus = "Kein Hub gewählt — bitte zuerst „Hub suchen“.";
            return;
        }

        train.ConnectionStatus = "Verbinde…";
        try
        {
            await _lego.ConnectAsync(HubKey(train), train.Hub, CancellationToken.None);
            train.IsHardwareConnected = true;
            train.ConnectionStatus = "Verbunden";
            Log.HubConnected(_log, train.Id);
        }
        catch (Exception ex)
        {
            train.IsHardwareConnected = false;
            train.ConnectionStatus = $"Fehler: {ex.Message}";
            Log.HubConnectFailed(_log, train.Id, ex);
        }

        NotifyHubCommandsCanExecute();
    }

    private async Task DisconnectSelectedTrainAsync()
    {
        if (SelectedTrain is not { } train) return;

        // No token on purpose: disconnecting must still run to completion when the user navigates away.
        try { await _lego.DisconnectAsync(HubKey(train), CancellationToken.None); }
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

    // Sends changes on the connected train to the hardware: slider -> motor, color -> LED.
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
        // As in DiscoverAsync: the send owns its source and disposes it when it finishes, so each
        // superseded debounce cleans itself up instead of leaking a CancellationTokenSource.
        var cts = new CancellationTokenSource();
        _speedDebounceCts?.Cancel();
        _speedDebounceCts = cts;
        _ = SendSpeedAsync(train, cts);
    }

    private async Task SendSpeedAsync(Train train, CancellationTokenSource cts)
    {
        try
        {
            try { await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token); }
            catch (TaskCanceledException) { return; }

            await SendToHubAsync(train, key => _lego.SetSpeedAsync(key, MotorPort, (sbyte)Math.Clamp(train.Speed, -100, 100), cts.Token));
        }
        finally
        {
            if (ReferenceEquals(_speedDebounceCts, cts)) _speedDebounceCts = null;
            cts.Dispose();
        }
    }

    private Task SendLedAsync(Train train)
    {
        var (r, g, b) = ParseHexColor(LegoinoCatalog.Color(train.Color).Hex);
        // No token: a colour change is a single fire-and-forget write, nothing supersedes it.
        return SendToHubAsync(train, key => _lego.SetLedAsync(key, r, g, b, CancellationToken.None));
    }

    // Best-effort live control: a dropped command just means the next change resends it.
    private async Task SendToHubAsync(Train train, Func<string, Task> command)
    {
        var hubKey = HubKey(train);
        if (string.IsNullOrWhiteSpace(hubKey)) return;

        try { await command(hubKey); }
        catch { /* ignore - the next slider/color change will resend */ }
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
