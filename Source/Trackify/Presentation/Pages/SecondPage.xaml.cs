using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Trackify.Presentation.Pages;

/// <summary>
/// Responsive layout for the Streckenplaner, same reasoning/shape as <see cref="MainPage"/>: reacts
/// to both width and a selection change, which VisualStateManager/AdaptiveTrigger can't express
/// declaratively. Wide: canvas | strand list | inspector as three columns. Narrow: canvas + strand
/// list stacked, inspector as a bottom sheet shown only while a segment is selected.
/// </summary>
public sealed partial class SecondPage : Page
{
    private const double WideThreshold = 720;

    private SecondViewModel? _viewModel;

    public SecondPage()
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

        _viewModel = args.NewValue as SecondViewModel;

        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        ApplyResponsiveLayout();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SecondViewModel.SelectedSegment))
            ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        var isWide = ActualWidth >= WideThreshold;
        var hasSelection = _viewModel?.SelectedSegment is not null;

        if (isWide)
        {
            // Three columns side by side: canvas + toolbar, strand-list sidebar (drag-resizable via
            // Splitter1Handle), inspector (drag-resizable via Splitter2Handle).
            if (ListColumn.Width.Value == 0) ListColumn.Width = new GridLength(236);
            if (InspectorColumn.Width.Value == 0) InspectorColumn.Width = new GridLength(318);
            Splitter1Column.Width = new GridLength(6);
            Splitter2Column.Width = new GridLength(6);
            CanvasRow.Height = new GridLength(1, GridUnitType.Star);
            ListRow.Height = new GridLength(0);

            Grid.SetColumn(ListHost, 2);
            Grid.SetRow(ListHost, 0);
            Grid.SetColumn(InspectorHost, 4);
            Grid.SetRow(InspectorHost, 0);
            Grid.SetRowSpan(InspectorHost, 1);

            Splitter1Handle.Visibility = Visibility.Visible;
            Splitter2Handle.Visibility = Visibility.Visible;
            InspectorHost.VerticalAlignment = VerticalAlignment.Stretch;
            InspectorHost.MaxHeight = double.PositiveInfinity;
            InspectorHost.Visibility = Visibility.Visible;
        }
        else
        {
            // Stacked: canvas + toolbar on top, strand list below; inspector docks as a bottom sheet
            // over both, only while a segment is selected.
            ListColumn.Width = new GridLength(0);
            InspectorColumn.Width = new GridLength(0);
            Splitter1Column.Width = new GridLength(0);
            Splitter2Column.Width = new GridLength(0);
            CanvasRow.Height = new GridLength(3, GridUnitType.Star);
            ListRow.Height = new GridLength(2, GridUnitType.Star);

            Grid.SetColumn(ListHost, 0);
            Grid.SetRow(ListHost, 1);
            Grid.SetColumn(InspectorHost, 0);
            Grid.SetRow(InspectorHost, 0);
            Grid.SetRowSpan(InspectorHost, 2);

            Splitter1Handle.Visibility = Visibility.Collapsed;
            Splitter2Handle.Visibility = Visibility.Collapsed;
            InspectorHost.VerticalAlignment = VerticalAlignment.Bottom;
            InspectorHost.MaxHeight = 520;
            InspectorHost.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
