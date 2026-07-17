using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Trackify.Presentation.Converters;

/// <summary>Background brush for a manually-driven segmented pill button: SecondaryContainer when selected, else transparent.</summary>
public sealed partial class SegmentBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isSelected = string.Equals(value?.ToString(), parameter as string, StringComparison.Ordinal);
        var key = isSelected ? "SecondaryContainerBrush" : "SurfaceBrush";
        return Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(key, out var brush) ? brush : new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
