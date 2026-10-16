namespace Trackify.Presentation.Layout;

/// <summary>
/// One grid column's width, as a plain value a test can read. It maps straight onto
/// <c>GridLength</c> but carries no XAML type, which is the whole point: the layout rules can then be
/// resolved and asserted without a UI thread.
/// </summary>
/// <param name="Value">Pixels when <paramref name="IsStar"/> is false, otherwise the star factor.</param>
/// <param name="IsStar">Whether the width is proportional rather than absolute.</param>
public record ColumnWidth(double Value, bool IsStar)
{
    /// <summary>Width zero: the column is still there but takes no space.</summary>
    public static ColumnWidth Collapsed { get; } = new(0, IsStar: false);

    /// <summary>A single star — takes whatever is left over.</summary>
    public static ColumnWidth Star { get; } = new(1, IsStar: true);

    /// <summary>An absolute width.</summary>
    public static ColumnWidth Pixels(double value) => new(value, IsStar: false);
}
