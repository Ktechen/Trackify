using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Trackify.Presentation.Converters;

/// <summary>Foreground brush counterpart to <see cref="SegmentBackgroundConverter"/>.</summary>
public sealed partial class SegmentForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isSelected = string.Equals(value?.ToString(), parameter as string, StringComparison.Ordinal);
        var key = isSelected ? "OnSecondaryContainerBrush" : "OnSurfaceVariantBrush";
        return Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(key, out var brush) ? brush : new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
