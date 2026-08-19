using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Trackify.Presentation.Behaviors;

/// <summary>
/// Attached behavior: turns a thin element (a hairline divider) into a drag handle that resizes a
/// <see cref="ColumnDefinition"/> — a hand-rolled GridSplitter, since none ships with
/// Uno.Toolkit/WinUI in this project (adding one means a new third-party package). Usage:
/// <c>local:ColumnSplitterBehavior.Target="{Binding ElementName=ListColumn}"</c>, optionally with
/// <c>local:ColumnSplitterBehavior.MinWidth</c>/<c>MaxWidth</c>. Set
/// <c>local:ColumnSplitterBehavior.Invert="True"</c> when <c>Target</c> is the column to the
/// splitter's *right* (dragging right then shrinks it instead of growing it) — the default assumes
/// <c>Target</c> is the column on the splitter's left.
/// </summary>
public static class ColumnSplitterBehavior
{
    private const double DefaultMinWidth = 160;
    private const double DefaultMaxWidth = 640;

    private static readonly ConditionalWeakTable<FrameworkElement, DragState> States = [];

    public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
        "Target", typeof(ColumnDefinition), typeof(ColumnSplitterBehavior), new PropertyMetadata(null, OnTargetChanged));

    public static readonly DependencyProperty MinWidthProperty = DependencyProperty.RegisterAttached(
        "MinWidth", typeof(double), typeof(ColumnSplitterBehavior), new PropertyMetadata(DefaultMinWidth));

    public static readonly DependencyProperty MaxWidthProperty = DependencyProperty.RegisterAttached(
        "MaxWidth", typeof(double), typeof(ColumnSplitterBehavior), new PropertyMetadata(DefaultMaxWidth));

    public static readonly DependencyProperty InvertProperty = DependencyProperty.RegisterAttached(
        "Invert", typeof(bool), typeof(ColumnSplitterBehavior), new PropertyMetadata(false));

    public static ColumnDefinition? GetTarget(DependencyObject element) => (ColumnDefinition?)element.GetValue(TargetProperty);

    public static void SetTarget(DependencyObject element, ColumnDefinition? value) => element.SetValue(TargetProperty, value);

    public static double GetMinWidth(DependencyObject element) => (double)element.GetValue(MinWidthProperty);

    public static void SetMinWidth(DependencyObject element, double value) => element.SetValue(MinWidthProperty, value);

    public static double GetMaxWidth(DependencyObject element) => (double)element.GetValue(MaxWidthProperty);

    public static void SetMaxWidth(DependencyObject element, double value) => element.SetValue(MaxWidthProperty, value);

    public static bool GetInvert(DependencyObject element) => (bool)element.GetValue(InvertProperty);

    public static void SetInvert(DependencyObject element, bool value) => element.SetValue(InvertProperty, value);

    private static void OnTargetChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not FrameworkElement fe)
            return;

        fe.PointerPressed -= OnPointerPressed;
        fe.PointerMoved -= OnPointerMoved;
        fe.PointerReleased -= OnPointerReleased;

        if (e.NewValue is ColumnDefinition)
        {
            fe.PointerPressed += OnPointerPressed;
            fe.PointerMoved += OnPointerMoved;
            fe.PointerReleased += OnPointerReleased;
        }
    }

    private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || GetTarget(fe) is not { } column)
            return;

        States.AddOrUpdate(fe, new DragState(e.GetCurrentPoint(fe).Position.X, column.ActualWidth));
        fe.CapturePointer(e.Pointer);
    }

    private static void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || GetTarget(fe) is not { } column || !States.TryGetValue(fe, out var state))
            return;

        var delta = e.GetCurrentPoint(fe).Position.X - state.StartX;
        if (GetInvert(fe)) delta = -delta;
        var width = Math.Clamp(state.StartWidth + delta, GetMinWidth(fe), GetMaxWidth(fe));
        column.Width = new GridLength(width);
    }

    private static void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe)
            return;

        States.Remove(fe);
        fe.ReleasePointerCapture(e.Pointer);
    }

    private sealed record DragState(double StartX, double StartWidth);
}
