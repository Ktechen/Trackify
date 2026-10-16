using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Trackify.Presentation.Pages;

/// <summary>
/// Responsive layout for the Streckenplaner, same reasoning/shape as <see cref="MainPage"/>: reacts
/// to both width and a selection change, which VisualStateManager/AdaptiveTrigger can't express
/// declaratively. Wide: canvas | parts list | inspector as three columns. Narrow (a phone, or a
/// window dragged in): the canvas keeps the whole area and both panes dock over it as bottom sheets —
/// the parts list on demand, the inspector while a part is selected.
/// <para>
/// It also publishes <see cref="SecondViewModel.IsCompact"/>, because this is the only place that
/// knows the real width; everything that merely reacts to being narrow binds to that instead.
/// </para>
/// </summary>
public sealed partial class SecondPage : Page
{
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
        if (e.PropertyName is nameof(SecondViewModel.SelectedPiece)
            or nameof(SecondViewModel.IsPieceListOpen))
            ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        var hasSelection = _viewModel?.SelectedPiece is not null;

        // The rules live in EditorPageLayout, which is a pure value and therefore testable; this
        // method only applies them. Everything that merely *reacts* to being narrow — the header
        // emblem, the subtitle, the counts, the speed legend, the words on the toolbar buttons —
        // binds to IsCompact instead of being poked from here.
        var layout = EditorPageLayout.Resolve(
            ActualWidth,
            hasSelection,
            _viewModel?.IsPieceListOpen == true,
            ListColumn.Width.Value,
            InspectorColumn.Width.Value);

        if (_viewModel is not null)
            _viewModel.IsCompact = !layout.IsWide;

        ListColumn.Width = Length(layout.ListColumn);
        InspectorColumn.Width = Length(layout.InspectorColumn);
        Splitter1Column.Width = Length(layout.SplitterColumns);
        Splitter2Column.Width = Length(layout.SplitterColumns);
        CanvasRow.Height = new GridLength(layout.CanvasRowStars, GridUnitType.Star);
        ListRow.Height = new GridLength(layout.ListRowStars);

        Place(ListHost, layout.List);
        Place(InspectorHost, layout.Inspector);

        var handles = layout.ShowSplitters ? Visibility.Visible : Visibility.Collapsed;
        Splitter1Handle.Visibility = handles;
        Splitter2Handle.Visibility = handles;
    }

    private static GridLength Length(ColumnWidth width)
        => width.IsStar ? new GridLength(width.Value, GridUnitType.Star) : new GridLength(width.Value);

    private static void Place(FrameworkElement pane, PaneSlot slot)
    {
        Grid.SetColumn(pane, slot.Column);
        Grid.SetRow(pane, slot.Row);
        Grid.SetRowSpan(pane, slot.RowSpan);

        pane.VerticalAlignment = slot.IsSheet ? VerticalAlignment.Bottom : VerticalAlignment.Stretch;
        pane.MaxHeight = slot.MaxHeight;
        pane.Visibility = slot.IsVisible ? Visibility.Visible : Visibility.Collapsed;
    }
}
