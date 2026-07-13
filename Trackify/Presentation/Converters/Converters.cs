using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Trackify.Presentation.Converters;

/// <summary>"#RRGGBB" -&gt; SolidColorBrush.</summary>
public sealed partial class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => new SolidColorBrush(TryParse(value as string, out var color) ? color : Colors.Transparent);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();

    private static bool TryParse(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(hex)) return false;
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return false;
        if (!byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)) return false;
        if (!byte.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)) return false;
        if (!byte.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)) return false;
        color = Color.FromArgb(255, r, g, b);
        return true;
    }
}

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

/// <summary>bool -&gt; inverted bool. Used to disable a control while a flag (e.g. IsDiscovering) is true.</summary>
public sealed partial class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is not bool b || !b;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is not bool b || !b;
}

/// <summary>bool -&gt; Visibility. Pass ConverterParameter="Invert" to flip.</summary>
public sealed partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var visible = value is bool b && b;
        if ("Invert".Equals(parameter as string, StringComparison.OrdinalIgnoreCase)) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

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
