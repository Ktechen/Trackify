using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Trackify.Presentation.Converters;

/// <summary>bool isValid -&gt; muted caption brush, or the error brush when false.</summary>
public sealed partial class ValidityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isValid = value is bool b && b;
        var key = isValid ? "MutedTextBrush" : "ErrorBrush";
        return Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(key, out var brush) ? brush : new SolidColorBrush(Colors.Red);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
