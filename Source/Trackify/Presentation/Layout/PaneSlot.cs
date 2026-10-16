namespace Trackify.Presentation.Layout;

/// <summary>
/// Where one side pane sits and whether it is shown. The same control is a column in the wide layout
/// and a sheet laid over the canvas in the narrow one, so the placement has to be data rather than
/// two branches of imperative grid assignments.
/// </summary>
/// <param name="Column">Grid column.</param>
/// <param name="Row">Grid row.</param>
/// <param name="RowSpan">How many rows it spans — 2 as a sheet, so it covers the canvas and toolbar.</param>
/// <param name="IsSheet">Whether it docks to the bottom edge over the canvas instead of filling a column.</param>
/// <param name="MaxHeight">A sheet's height cap; <see cref="double.PositiveInfinity"/> in a column.</param>
/// <param name="IsVisible">Whether it is shown at all.</param>
public record PaneSlot(int Column, int Row, int RowSpan, bool IsSheet, double MaxHeight, bool IsVisible)
{
    /// <summary>A pane filling its own column, always visible.</summary>
    public static PaneSlot InColumn(int column)
        => new(column, Row: 0, RowSpan: 1, IsSheet: false, double.PositiveInfinity, IsVisible: true);

    /// <summary>A pane docked over the canvas, shown only when it has something to say.</summary>
    public static PaneSlot Sheet(double maxHeight, bool isVisible)
        => new(Column: 0, Row: 0, RowSpan: 2, IsSheet: true, maxHeight, isVisible);
}
