using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Trackify.Presentation.Behaviors;

/// <summary>
/// Attached behavior: turns a press-drag-release on an element into three commands, each given the
/// point in the element's own coordinate space.
///
/// This is the one escape hatch the house rules allow — an event that has to reach a command which
/// is not natively <c>Command</c>-bindable — and it is why drawing needs no code-behind. Attach it to
/// the fixed-size <c>Canvas</c> itself, not to the <c>Viewbox</c> around it: points then arrive in
/// canvas coordinates already, so zoom never has to be undone by hand.
///
/// Usage:
/// <code>
/// behaviors:DrawGestureBehavior.StartCommand="{Binding ElementName=Root, Path=DataContext.DrawStartCommand}"
/// behaviors:DrawGestureBehavior.MoveCommand="{Binding ElementName=Root, Path=DataContext.DrawMoveCommand}"
/// behaviors:DrawGestureBehavior.EndCommand="{Binding ElementName=Root, Path=DataContext.DrawEndCommand}"
/// </code>
/// </summary>
public static class DrawGestureBehavior
{
    /// <summary>Runs when a drag begins, with the point it began at.</summary>
    public static readonly DependencyProperty StartCommandProperty = DependencyProperty.RegisterAttached(
        "StartCommand", typeof(ICommand), typeof(DrawGestureBehavior), new PropertyMetadata(null, OnCommandChanged));

    /// <summary>Runs as the drag moves, with each point it passes.</summary>
    public static readonly DependencyProperty MoveCommandProperty = DependencyProperty.RegisterAttached(
        "MoveCommand", typeof(ICommand), typeof(DrawGestureBehavior), new PropertyMetadata(null, OnCommandChanged));

    /// <summary>Runs when the drag ends.</summary>
    public static readonly DependencyProperty EndCommandProperty = DependencyProperty.RegisterAttached(
        "EndCommand", typeof(ICommand), typeof(DrawGestureBehavior), new PropertyMetadata(null, OnCommandChanged));

    // Tracks which element is mid-drag, so a move that arrives without a press is ignored.
    private static readonly DependencyProperty IsDrawingProperty = DependencyProperty.RegisterAttached(
        "IsDrawing", typeof(bool), typeof(DrawGestureBehavior), new PropertyMetadata(false));

    public static ICommand? GetStartCommand(DependencyObject element)
        => (ICommand?)element.GetValue(StartCommandProperty);

    public static void SetStartCommand(DependencyObject element, ICommand? value)
        => element.SetValue(StartCommandProperty, value);

    public static ICommand? GetMoveCommand(DependencyObject element)
        => (ICommand?)element.GetValue(MoveCommandProperty);

    public static void SetMoveCommand(DependencyObject element, ICommand? value)
        => element.SetValue(MoveCommandProperty, value);

    public static ICommand? GetEndCommand(DependencyObject element)
        => (ICommand?)element.GetValue(EndCommandProperty);

    public static void SetEndCommand(DependencyObject element, ICommand? value)
        => element.SetValue(EndCommandProperty, value);

    private static void OnCommandChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not FrameworkElement target) return;

        // Re-subscribing is idempotent: all three properties share this handler, so a second binding
        // must not attach the pointer handlers twice.
        target.PointerPressed -= OnPointerPressed;
        target.PointerMoved -= OnPointerMoved;
        target.PointerReleased -= OnPointerReleased;
        target.PointerCanceled -= OnPointerReleased;
        target.PointerCaptureLost -= OnPointerReleased;

        if (!HasAnyCommand(target)) return;

        target.PointerPressed += OnPointerPressed;
        target.PointerMoved += OnPointerMoved;
        target.PointerReleased += OnPointerReleased;
        target.PointerCanceled += OnPointerReleased;
        target.PointerCaptureLost += OnPointerReleased;
    }

    private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement target) return;

        target.SetValue(IsDrawingProperty, true);
        target.CapturePointer(e.Pointer);

        Invoke(GetStartCommand(target), PointIn(target, e));
    }

    private static void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement target) return;
        if (!IsDrawing(target)) return;

        Invoke(GetMoveCommand(target), PointIn(target, e));
    }

    private static void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement target) return;

        // Capture loss and cancellation end the stroke too, so an interrupted drag cannot leave the
        // editor stuck in drawing mode with a ghost preview on screen.
        if (!IsDrawing(target)) return;

        target.SetValue(IsDrawingProperty, false);

        Invoke(GetEndCommand(target), PointIn(target, e));
    }

    private static bool IsDrawing(DependencyObject element) => element.GetValue(IsDrawingProperty) is true;

    private static bool HasAnyCommand(DependencyObject element)
        => GetStartCommand(element) is not null
            || GetMoveCommand(element) is not null
            || GetEndCommand(element) is not null;

    private static Point PointIn(UIElement target, PointerRoutedEventArgs e)
        => e.GetCurrentPoint(target).Position;

    private static void Invoke(ICommand? command, Point point)
    {
        if (command is not null && command.CanExecute(point)) command.Execute(point);
    }
}
