using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Trackify.Presentation;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigator _navigator;
    private int _sequence = 4;

    [ObservableProperty] private string search = "";
    [ObservableProperty] private TrainFilter filter = TrainFilter.All;
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

    public IRelayCommand<LedColor?> SetColorCommand { get; }

    public IAsyncRelayCommand GoToStreckenplanerCommand { get; }

    public MainViewModel(INavigator navigator)
    {
        _navigator = navigator;

        SelectTrainCommand = new RelayCommand<Train>(t => SelectedTrain = t);
        AddTrainCommand = new RelayCommand(AddTrain);
        DuplicateTrainCommand = new RelayCommand(DuplicateTrain, () => SelectedTrain is not null);
        DeleteTrainCommand = new RelayCommand(DeleteTrain, () => SelectedTrain is not null);
        ToggleActiveCommand = new RelayCommand(ToggleActive, () => SelectedTrain is not null);
        SetFilterCommand = new RelayCommand<string>(f => Filter = Enum.Parse<TrainFilter>(f!));
        SetColorCommand = new RelayCommand<LedColor?>(c => { if (SelectedTrain is not null && c is { } color) SelectedTrain.Color = color; });
        GoToStreckenplanerCommand = new AsyncRelayCommand(GoToStreckenplaner);

        ColorSwatches = [.. LegoinoCatalog.Colors.Select(c => new ColorSwatchItem { Value = c.Value, Name = c.Name, Hex = c.Hex })];

        Trains.CollectionChanged += TrainsOnCollectionChanged;
        Seed();
        ApplyFilter();
    }

    private void Seed()
    {
        Add(new Train
        {
            Id = "trn-1", Name = "Roter Express", Hub = HubType.PoweredUpHub, BleAddress = "90:84:2B:00:1A:07",
            Color = LedColor.Red, PortA = DeviceType.TrainMotor, PortB = DeviceType.Light, Speed = 80,
            AccelFn = SpeedFunctionType.EaseOut, BrakeFn = SpeedFunctionType.EaseIn, IsActive = true,
        });
        Add(new Train
        {
            Id = "trn-2", Name = "Cargo Blau", Hub = HubType.ControlPlusHub, BleAddress = "90:84:2B:11:4C:2E",
            Color = LedColor.Blue, PortA = DeviceType.TrainMotor, PortB = DeviceType.TrainMotor, Speed = 100,
            AccelFn = SpeedFunctionType.SCurve, BrakeFn = SpeedFunctionType.SCurve, IsActive = true,
        });
        Add(new Train
        {
            Id = "trn-3", Name = "Nacht-Tram", Hub = HubType.BoostMoveHub, BleAddress = "90:84:2B:22:9F:D1",
            Color = LedColor.Green, PortA = DeviceType.TrainMotor, PortB = DeviceType.None, Speed = -40,
            AccelFn = SpeedFunctionType.Linear, BrakeFn = SpeedFunctionType.EaseIn, IsActive = false,
        });

        void Add(Train t) => Trains.Add(t);
    }

    private void AddTrain()
    {
        var train = new Train
        {
            Id = $"trn-{_sequence++}", Name = "Neuer Train", Hub = HubType.PoweredUpHub, BleAddress = "",
            Color = LedColor.Green, PortA = DeviceType.TrainMotor, PortB = DeviceType.None, Speed = 70,
            AccelFn = SpeedFunctionType.EaseOut, BrakeFn = SpeedFunctionType.EaseIn, IsActive = true,
        };
        Trains.Add(train);
        Filter = TrainFilter.All;
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

    partial void OnFilterChanged(TrainFilter value) => ApplyFilter();

    partial void OnSelectedTrainChanged(Train? oldValue, Train? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= SelectedTrainOnPropertyChanged;
        if (newValue is not null) newValue.PropertyChanged += SelectedTrainOnPropertyChanged;

        DuplicateTrainCommand.NotifyCanExecuteChanged();
        DeleteTrainCommand.NotifyCanExecuteChanged();
        ToggleActiveCommand.NotifyCanExecuteChanged();
        UpdateColorSwatchSelection();
    }

    private void SelectedTrainOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Train.Name) or nameof(Train.IsActive)) ApplyFilter();
        if (e.PropertyName is nameof(Train.Color)) UpdateColorSwatchSelection();
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
                TrainFilter.Active => t.IsActive,
                TrainFilter.Inactive => !t.IsActive,
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
