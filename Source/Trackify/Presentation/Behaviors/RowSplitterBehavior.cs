using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Trackify.Presentation.Behaviors;

/// <summary>
/// Attached behavior: turns a thin element (a hairline divider) into a drag handle that resizes a
/// <see cref="RowDefinition"/> — the vertical counterpart of <see cref="ColumnSplitterBehavior"/> (see
/// its remarks for why this is hand-rolled instead of a package GridSplitter). Usage:
/// <c>local:RowSplitterBehavior.Target="{Binding ElementName=SomeRow}"</c>, optionally with
/// <c>local:RowSplitterBehavior.MinHeight</c>/<c>MaxHeight</c>. <c>Target</c> defaults to the row
/// *above* the splitter (dragging down grows it, dragging up shrinks it) — set
/// <c>local:RowSplitterBehavior.Invert="True"</c> when <c>Target</c> is the row *below* instead.
/// </summary>
public static class RowSplitterBehavior
{
    private const double DefaultMinHeight = 90;
    private const double DefaultMaxHeight = 800;

    private static readonly ConditionalWeakTable<FrameworkElement, DragState> States = [];

    public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
        "Target", typeof(RowDefinition), typeof(RowSplitterBehavior), new PropertyMetadata(null, OnTargetChanged));

    public static readonly DependencyProperty MinHeightProperty = DependencyProperty.RegisterAttached(
        "MinHeight", typeof(double), typeof(RowSplitterBehavior), new PropertyMetadata(DefaultMinHeight));

    public static readonly DependencyProperty MaxHeightProperty = DependencyProperty.RegisterAttached(
        "MaxHeight", typeof(double), typeof(RowSplitterBehavior), new PropertyMetadata(DefaultMaxHeight));

    public static readonly DependencyProperty InvertProperty = DependencyProperty.RegisterAttached(
        "Invert", typeof(bool), typeof(RowSplitterBehavior), new PropertyMetadata(false));

    public static RowDefinition? GetTarget(DependencyObject element) => (RowDefinition?)element.GetValue(TargetProperty);

    public static void SetTarget(DependencyObject element, RowDefinition? value) => element.SetValue(TargetProperty, value);

    public static double GetMinHeight(DependencyObject element) => (double)element.GetValue(MinHeightProperty);

    public static void SetMinHeight(DependencyObject element, double value) => element.SetValue(MinHeightProperty, value);

    public static double GetMaxHeight(DependencyObject element) => (double)element.GetValue(MaxHeightProperty);

    public static void SetMaxHeight(DependencyObject element, double value) => element.SetValue(MaxHeightProperty, value);

    public static bool GetInvert(DependencyObject element) => (bool)element.GetValue(InvertProperty);

    public static void SetInvert(DependencyObject element, bool value) => element.SetValue(InvertProperty, value);

    private static void OnTargetChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not FrameworkElement fe)
            return;

        fe.PointerPressed -= OnPointerPressed;
        fe.PointerMoved -= OnPointerMoved;
        fe.PointerReleased -= OnPointerReleased;

        if (e.NewValue is RowDefinition)
        {
            fe.PointerPressed += OnPointerPressed;
            fe.PointerMoved += OnPointerMoved;
            fe.PointerReleased += OnPointerReleased;
        }
    }

    private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || GetTarget(fe) is not { } row)
            return;

        States.AddOrUpdate(fe, new DragState(e.GetCurrentPoint(fe).Position.Y, row.ActualHeight));
        fe.CapturePointer(e.Pointer);
    }

    private static void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || GetTarget(fe) is not { } row || !States.TryGetValue(fe, out var state))
            return;

        var delta = e.GetCurrentPoint(fe).Position.Y - state.StartY;
        if (GetInvert(fe)) delta = -delta;
        var height = Math.Clamp(state.StartHeight + delta, GetMinHeight(fe), GetMaxHeight(fe));
        row.Height = new GridLength(height);
    }

    private static void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe)
            return;

        States.Remove(fe);
        fe.ReleasePointerCapture(e.Pointer);
    }

    private sealed record DragState(double StartY, double StartHeight);
}
