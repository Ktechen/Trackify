using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Trackify.Presentation.Converters;

/// <summary>null -&gt; Collapsed, non-null -&gt; Visible. Pass ConverterParameter="Invert" to flip.</summary>
public sealed partial class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var visible = value is not null;
        if ("Invert".Equals(parameter as string, StringComparison.OrdinalIgnoreCase)) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
