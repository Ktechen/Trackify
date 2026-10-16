using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Trackify.Presentation.Pages;

public sealed partial class MainPage : Page
{
    // Below this width (in effective pixels) the two-column layout collapses to a single
    // column that shows the list OR the editor (master-detail) - the phone/portrait case.

    private MainViewModel? _viewModel;

    public MainPage()
    {
        this.InitializeComponent();

        SizeChanged += (_, _) => ApplyResponsiveLayout();
        Loaded += (_, _) => ApplyResponsiveLayout();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = args.NewValue as MainViewModel;

        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        ApplyResponsiveLayout();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Selecting a train (or clearing it) flips which pane is shown in the narrow layout.
        if (e.PropertyName == nameof(MainViewModel.SelectedTrain))
            ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        // The rules live in TrainPageLayout, which is a pure value and therefore testable; this method
        // only applies them.
        var layout = TrainPageLayout.Resolve(
            ActualWidth,
            _viewModel?.SelectedTrain is not null,
            ListColumn.Width.Value);

        ListColumn.Width = Length(layout.ListColumn);
        SplitterColumn.Width = Length(layout.SplitterColumn);
        EditorColumn.Width = Length(layout.EditorColumn);

        ListPanel.Visibility = Show(layout.ShowList);
        SplitterHandle.Visibility = Show(layout.ShowSplitter);
        EditorHost.Visibility = Show(layout.ShowEditor);

        BackButton.Visibility = Show(layout.ShowBackButton);
        HeaderEmblem.Visibility = Show(layout.ShowHeaderEmblem);
        HeaderStats.Visibility = Show(layout.ShowHeaderStats);
        HeaderChip.Visibility = Show(layout.ShowConnectionChip);
    }

    private static GridLength Length(ColumnWidth width)
        => width.IsStar ? new GridLength(width.Value, GridUnitType.Star) : new GridLength(width.Value);

    private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;
}
