using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Trackify.Models.Trains;
using Train = Trackify.Models.Trains.Train;

namespace Trackify.Presentation.ViewModels;

/// <summary>
/// Main screen: the train list/editor and its commands. Hub (Bluetooth) control lives in the
/// <c>MainViewModel.Hub.cs</c> partial to keep the two concerns separate.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly INavigator _navigator;
    private readonly ILogger<MainViewModel> _log;
    private int _sequence = 1;

    [ObservableProperty] private string search = "";
    [ObservableProperty] private TrainFilterType filter = TrainFilterType.All;
    [ObservableProperty] private Train? selectedTrain;
    [ObservableProperty] private int activeCount;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<Train> Trains { get; } = [];

    public ObservableCollection<Train> FilteredTrains { get; } = [];

    public ObservableCollection<ColorSwatchItemViewModel> ColorSwatches { get; }

    public IReadOnlyList<HubOption> HubOptions => LegoinoCatalog.Hubs;

    public IReadOnlyList<DeviceOption> DeviceOptions => LegoinoCatalog.Devices;

    public IReadOnlyList<SpeedFunctionOption> SpeedFunctionOptions => LegoinoCatalog.SpeedFunctions;

    public IRelayCommand AddTrainCommand { get; }

    public IRelayCommand DuplicateTrainCommand { get; }

    public IRelayCommand DeleteTrainCommand { get; }

    public IRelayCommand<string> SetFilterCommand { get; }

    public IRelayCommand<LedColorType?> SetColorCommand { get; }

    public IAsyncRelayCommand GoToStreckenplanerCommand { get; }

    public IAsyncRelayCommand ConnectCommand { get; }

    public IAsyncRelayCommand DisconnectCommand { get; }

    public IAsyncRelayCommand DiscoverCommand { get; }

    public IRelayCommand StopDiscoverCommand { get; }

    public IRelayCommand ClearDiscoverCommand { get; }

    public IAsyncRelayCommand ConfirmAddHubCommand { get; }

    public IRelayCommand CancelAddHubCommand { get; }

    public IRelayCommand BackToListCommand { get; }

    public MainViewModel(INavigator navigator, ILegoService lego, ILogger<MainViewModel> logger)
    {
        _navigator = navigator;
        _lego = lego;
        _log = logger;

        AddTrainCommand = new RelayCommand(AddTrain);
        DuplicateTrainCommand = new RelayCommand(DuplicateTrain, () => SelectedTrain is not null);
        DeleteTrainCommand = new RelayCommand(DeleteTrain, () => SelectedTrain is not null);
        SetFilterCommand = new RelayCommand<string>(name => Filter = Enum.Parse<TrainFilterType>(name!));
        SetColorCommand = new RelayCommand<LedColorType?>(SetSelectedTrainColor);
        GoToStreckenplanerCommand = new AsyncRelayCommand(GoToStreckenplaner);
        ConnectCommand = new AsyncRelayCommand(ConnectSelectedTrainAsync, () => SelectedTrain is { IsHardwareConnected: false });
        DisconnectCommand = new AsyncRelayCommand(DisconnectSelectedTrainAsync, () => SelectedTrain is { IsHardwareConnected: true });
        DiscoverCommand = new AsyncRelayCommand(DiscoverAsync);
        StopDiscoverCommand = new RelayCommand(StopDiscover);
        ClearDiscoverCommand = new RelayCommand(HideDiscoverStatus);
        ConfirmAddHubCommand = new AsyncRelayCommand(ConfirmAddHubAsync);
        CancelAddHubCommand = new RelayCommand(CancelAddHub);
        BackToListCommand = new RelayCommand(() => SelectedTrain = null);

        ColorSwatches = [.. LegoinoCatalog.Colors.Select(c => new ColorSwatchItemViewModel { Value = c.Value, Name = c.Name, Hex = c.Hex })];

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
        SelectAfresh(train);
    }

    private void DuplicateTrain()
    {
        if (SelectedTrain is null) return;

        var copy = SelectedTrain.Clone($"trn-{_sequence++}");
        Trains.Insert(Trains.IndexOf(SelectedTrain) + 1, copy);
        SelectedTrain = copy;
        ApplyFilter();
    }

    private void DeleteTrain()
    {
        if (SelectedTrain is null) return;

        Trains.Remove(SelectedTrain);
        SelectedTrain = Trains.FirstOrDefault();
        ApplyFilter();
    }

    private void SetSelectedTrainColor(LedColorType? color)
    {
        if (SelectedTrain is not null && color is { } value)
            SelectedTrain.Color = value;
    }

    // Clears search/filter so a just-added train is visible, then selects it.
    private void SelectAfresh(Train train)
    {
        Filter = TrainFilterType.All;
        Search = "";
        SelectedTrain = train;
        ApplyFilter();
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
        foreach (Train t in e.OldItems ?? Array.Empty<object>()) t.PropertyChanged -= TrainOnPropertyChanged;
        foreach (Train t in e.NewItems ?? Array.Empty<object>()) t.PropertyChanged += TrainOnPropertyChanged;
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
        foreach (var swatch in ColorSwatches)
            swatch.IsSelected = SelectedTrain is not null && swatch.Value == SelectedTrain.Color;
    }

    private void ApplyFilter()
    {
        FilteredTrains.Clear();
        foreach (var train in Trains.Where(MatchesFilterAndSearch))
            FilteredTrains.Add(train);

        RefreshCounts();
    }

    private bool MatchesFilterAndSearch(Train train)
    {
        var matchesFilter = Filter switch
        {
            TrainFilterType.Active => train.IsActive,
            TrainFilterType.Inactive => !train.IsActive,
            _ => true,
        };

        var query = Search.Trim();
        var matchesSearch = query.Length == 0
            || train.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || train.Hub.ToString().Contains(query, StringComparison.OrdinalIgnoreCase);

        return matchesFilter && matchesSearch;
    }
}
