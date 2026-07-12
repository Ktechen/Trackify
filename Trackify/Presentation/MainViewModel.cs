using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Trackify.Models.Trains;
using Trackify.Models.Trains.Enums;
using Trackify.Services;
using Train = Trackify.Models.Trains.Train;

namespace Trackify.Presentation;

/// <summary>
/// Main screen: the train list/editor and its commands. Hub (Bluetooth) control lives in the
/// <c>MainViewModel.Hub.cs</c> partial to keep the two concerns separate.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly INavigator _navigator;
    private int _sequence = 1;

    [ObservableProperty] private string search = "";
    [ObservableProperty] private TrainFilterType filter = TrainFilterType.All;
    [ObservableProperty] private Train? selectedTrain;
    [ObservableProperty] private int activeCount;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<Train> Trains { get; } = [];

    public ObservableCollection<Train> FilteredTrains { get; } = [];

    public ObservableCollection<ColorSwatchItem> ColorSwatches { get; }

    public IReadOnlyList<HubOption> HubOptions => LegoinoCatalog.Hubs;

    public IReadOnlyList<DeviceOption> DeviceOptions => LegoinoCatalog.Devices;

    public IReadOnlyList<SpeedFunctionOption> SpeedFunctionOptions => LegoinoCatalog.SpeedFunctions;

    public IRelayCommand<Train> SelectTrainCommand { get; }

    public IRelayCommand AddTrainCommand { get; }

    public IRelayCommand DuplicateTrainCommand { get; }

    public IRelayCommand DeleteTrainCommand { get; }

    public IRelayCommand ToggleActiveCommand { get; }

    public IRelayCommand<string> SetFilterCommand { get; }

    public IRelayCommand<LedColorType?> SetColorCommand { get; }

    public IAsyncRelayCommand GoToStreckenplanerCommand { get; }

    public IAsyncRelayCommand ConnectCommand { get; }

    public IAsyncRelayCommand DisconnectCommand { get; }

    public IAsyncRelayCommand DiscoverCommand { get; }

    public IRelayCommand StopDiscoverCommand { get; }

    public IRelayCommand<DiscoveredHub> UseDiscoveredHubCommand { get; }

    public IRelayCommand ClearDiscoverCommand { get; }

    public IAsyncRelayCommand ConfirmAddHubCommand { get; }

    public IRelayCommand CancelAddHubCommand { get; }

    public IRelayCommand BackToListCommand { get; }

    public MainViewModel(INavigator navigator, ILegoService lego)
    {
        _navigator = navigator;
        _lego = lego;

        SelectTrainCommand = new RelayCommand<Train>(t => SelectedTrain = t);
        AddTrainCommand = new RelayCommand(AddTrain);
        DuplicateTrainCommand = new RelayCommand(DuplicateTrain, () => SelectedTrain is not null);
        DeleteTrainCommand = new RelayCommand(DeleteTrain, () => SelectedTrain is not null);
        ToggleActiveCommand = new RelayCommand(ToggleActive, () => SelectedTrain is not null);
        SetFilterCommand = new RelayCommand<string>(f => Filter = Enum.Parse<TrainFilterType>(f!));
        SetColorCommand = new RelayCommand<LedColorType?>(c => { if (SelectedTrain is not null && c is { } color) SelectedTrain.Color = color; });
        GoToStreckenplanerCommand = new AsyncRelayCommand(GoToStreckenplaner);
        ConnectCommand = new AsyncRelayCommand(ConnectSelectedTrainAsync, () => SelectedTrain is { IsHardwareConnected: false });
        DisconnectCommand = new AsyncRelayCommand(DisconnectSelectedTrainAsync, () => SelectedTrain is { IsHardwareConnected: true });
        DiscoverCommand = new AsyncRelayCommand(DiscoverAsync);
        StopDiscoverCommand = new RelayCommand(StopDiscover);
        UseDiscoveredHubCommand = new RelayCommand<DiscoveredHub>(UseDiscoveredHub);
        ClearDiscoverCommand = new RelayCommand(ClearDiscoverResults);
        ConfirmAddHubCommand = new AsyncRelayCommand(ConfirmAddHubAsync);
        CancelAddHubCommand = new RelayCommand(CancelAddHub);
        BackToListCommand = new RelayCommand(() => SelectedTrain = null);

        ColorSwatches = [.. LegoinoCatalog.Colors.Select(c => new ColorSwatchItem { Value = c.Value, Name = c.Name, Hex = c.Hex })];

        Trains.CollectionChanged += TrainsOnCollectionChanged;
        ApplyFilter();
    }

    private void AddTrain()
    {
        var train = new Train
        {
            Id = $"trn-{_sequence++}", Name = "Neuer Train", Hub = HubType.PoweredUpHub, BleAddress = "",
            Color = LedColorType.Green, PortA = DeviceType.TrainMotor, PortB = DeviceType.None, Speed = 70,
            AccelFn = SpeedFunctionType.EaseOut, BrakeFn = SpeedFunctionType.EaseIn, IsActive = true,
        };
        Trains.Add(train);
        Filter = TrainFilterType.All;
        Search = "";
        SelectedTrain = train;
        ApplyFilter();
    }

    private void DuplicateTrain()
    {
        if (SelectedTrain is null) return;
        var copy = SelectedTrain.Clone($"trn-{_sequence++}");
        var index = Trains.IndexOf(SelectedTrain);
        Trains.Insert(index + 1, copy);
        SelectedTrain = copy;
        ApplyFilter();
    }

    private void DeleteTrain()
    {
        if (SelectedTrain is null) return;
        Trains.Remove(SelectedTrain);
        SelectedTrain = Trains.Count > 0 ? Trains[0] : null;
        ApplyFilter();
    }

    private void ToggleActive()
    {
        if (SelectedTrain is null) return;
        SelectedTrain.IsActive = !SelectedTrain.IsActive;
    }

    private async Task GoToStreckenplaner() => await _navigator.NavigateViewModelAsync<SecondViewModel>(this);

    partial void OnSearchChanged(string value) => ApplyFilter();

    partial void OnFilterChanged(TrainFilterType value) => ApplyFilter();

    partial void OnSelectedTrainChanged(Train? oldValue, Train? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= SelectedTrainOnPropertyChanged;
        if (newValue is not null) newValue.PropertyChanged += SelectedTrainOnPropertyChanged;

        DuplicateTrainCommand.NotifyCanExecuteChanged();
        DeleteTrainCommand.NotifyCanExecuteChanged();
        ToggleActiveCommand.NotifyCanExecuteChanged();
        NotifyHubCommandsCanExecute();
        UpdateColorSwatchSelection();
    }

    private void SelectedTrainOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Train.Name) or nameof(Train.IsActive)) ApplyFilter();
        if (e.PropertyName is nameof(Train.Color)) UpdateColorSwatchSelection();

        if (sender is Train train)
            OnSelectedTrainHubPropertyChanged(train, e.PropertyName);
    }

    private void TrainsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (Train t in e.OldItems) t.PropertyChanged -= TrainOnPropertyChanged;
        }
        if (e.NewItems is not null)
        {
            foreach (Train t in e.NewItems) t.PropertyChanged += TrainOnPropertyChanged;
        }
        RefreshCounts();
    }

    private void TrainOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Train.IsActive)) RefreshCounts();
    }

    private void RefreshCounts()
    {
        TotalCount = Trains.Count;
        ActiveCount = Trains.Count(t => t.IsActive);
    }

    private void UpdateColorSwatchSelection()
    {
        foreach (var swatch in ColorSwatches) swatch.IsSelected = SelectedTrain is not null && swatch.Value == SelectedTrain.Color;
    }

    private void ApplyFilter()
    {
        var query = Search.Trim();
        var matches = Trains.Where(t =>
            Filter switch
            {
                TrainFilterType.Active => t.IsActive,
                TrainFilterType.Inactive => !t.IsActive,
                _ => true,
            } &&
            (string.IsNullOrEmpty(query) ||
             t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             t.Hub.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();

        FilteredTrains.Clear();
        foreach (var t in matches) FilteredTrains.Add(t);
        RefreshCounts();
    }
}
