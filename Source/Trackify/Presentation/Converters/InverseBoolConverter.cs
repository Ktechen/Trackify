using Microsoft.UI.Xaml.Data;

namespace Trackify.Presentation.Converters;

/// <summary>bool -&gt; inverted bool. Used to disable a control while a flag (e.g. IsDiscovering) is true.</summary>
public sealed partial class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is not bool b || !b;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is not bool b || !b;
}
