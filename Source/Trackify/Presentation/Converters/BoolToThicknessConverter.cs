using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Trackify.Presentation.Converters;

/// <summary>bool -&gt; Thickness: ConverterParameter (default 2) when true, else 0. Used for the selected-swatch ring.</summary>
public sealed partial class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isTrue = value is bool b && b;
        var thickness = double.TryParse(parameter as string, NumberStyles.Float, CultureInfo.InvariantCulture, out var t) ? t : 2;
        return isTrue ? new Thickness(thickness) : new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
