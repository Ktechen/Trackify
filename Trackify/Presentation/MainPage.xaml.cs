using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Trackify.Presentation;

public sealed partial class MainPage : Page
{
    // Below this width (in effective pixels) the two-column layout collapses to a single
    // column that shows the list OR the editor (master-detail) - the phone/portrait case.
    private const double WideThreshold = 720;

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
        var isWide = ActualWidth >= WideThreshold;
        var hasSelection = _viewModel?.SelectedTrain is not null;

        if (isWide)
        {
            // Two columns side by side: list rail + editor.
            ListColumn.Width = new GridLength(346);
            EditorColumn.Width = new GridLength(1, GridUnitType.Star);
            ListPanel.Visibility = Visibility.Visible;
            EditorHost.Visibility = Visibility.Visible;
        }
        else if (hasSelection)
        {
            // Narrow + a train selected: show the editor full width.
            ListColumn.Width = new GridLength(0);
            EditorColumn.Width = new GridLength(1, GridUnitType.Star);
            ListPanel.Visibility = Visibility.Collapsed;
            EditorHost.Visibility = Visibility.Visible;
        }
        else
        {
            // Narrow + nothing selected: show the list full width (the "home" pane).
            ListColumn.Width = new GridLength(1, GridUnitType.Star);
            EditorColumn.Width = new GridLength(0);
            ListPanel.Visibility = Visibility.Visible;
            EditorHost.Visibility = Visibility.Collapsed;
        }

        // The back arrow only makes sense in the narrow layout while viewing the editor.
        BackButton.Visibility = (!isWide && hasSelection) ? Visibility.Visible : Visibility.Collapsed;

        // The header stats/connection chip get cramped on a phone - hide them when narrow.
        HeaderStats.Visibility = isWide ? Visibility.Visible : Visibility.Collapsed;
        HeaderChip.Visibility = isWide
            ? (hasSelection ? Visibility.Visible : Visibility.Collapsed)
            : Visibility.Collapsed;
    }
}
