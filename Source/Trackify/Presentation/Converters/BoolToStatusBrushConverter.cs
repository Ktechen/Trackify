using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Trackify.Presentation.Converters;

/// <summary>Train.IsActive -&gt; the small green/gray status-dot brush shown on list rows.</summary>
public sealed partial class BoolToStatusBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBrush = new(Color.FromArgb(255, 0x16, 0xA3, 0x4A));
    private static readonly SolidColorBrush InactiveBrush = new(Color.FromArgb(255, 0xC3, 0xC0, 0xCB));

    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? ActiveBrush : InactiveBrush;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
