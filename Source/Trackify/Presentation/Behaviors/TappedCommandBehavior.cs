using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Trackify.Presentation.Behaviors;

/// <summary>
/// Attached behavior: run a command when an element is tapped, passing the element's DataContext as
/// the parameter. Lets item templates invoke a view-model command without page code-behind.
/// Usage: <c>local:TappedCommandBehavior.Command="{Binding ElementName=Root, Path=DataContext.SomeCommand}"</c>
/// </summary>
public static class TappedCommandBehavior
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(TappedCommandBehavior), new PropertyMetadata(null, OnCommandChanged));

    public static ICommand? GetCommand(DependencyObject element) => (ICommand?)element.GetValue(CommandProperty);

    public static void SetCommand(DependencyObject element, ICommand? value) => element.SetValue(CommandProperty, value);

    private static void OnCommandChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not FrameworkElement fe)
            return;

        fe.Tapped -= OnTapped;
        if (e.NewValue is ICommand)
            fe.Tapped += OnTapped;
    }

    private static void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || GetCommand(fe) is not { } command)
            return;

        var parameter = fe.DataContext;
        if (command.CanExecute(parameter))
            command.Execute(parameter);
    }
}
