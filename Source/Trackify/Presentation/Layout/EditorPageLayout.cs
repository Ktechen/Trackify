namespace Trackify.Presentation.Layout;

/// <summary>
/// The Streckenplaner's responsive layout, resolved from width and state alone. It is a pure value so
/// the rules are testable: the page it drives is a <c>Page</c>, which cannot be instantiated without a
/// UI thread, and the rendered result cannot be inspected in this environment either — so if these
/// rules were not separable, nothing would check them but a human with a phone.
/// <para>
/// Wide: canvas | parts list | inspector, three columns, both panes drag-resizable. Narrow: the canvas
/// keeps the whole area and both panes dock over it as bottom sheets.
/// </para>
/// </summary>
public record EditorPageLayout(
    bool IsWide,
    ColumnWidth ListColumn,
    ColumnWidth InspectorColumn,
    ColumnWidth SplitterColumns,
    double CanvasRowStars,
    double ListRowStars,
    bool ShowSplitters,
    PaneSlot List,
    PaneSlot Inspector)
{
    /// <summary>Below this width the panes become sheets. Matches <see cref="TrainPageLayout.WideThreshold"/>.</summary>
    public const double WideThreshold = 720;

    /// <summary>
    /// The parts list's default column width. It is a floor, not a preference: the rows carry a real
    /// intrinsic width, so a narrower column clips rather than shrinks.
    /// </summary>
    public const double DefaultListPixels = 260;

    /// <summary>The inspector's default column width, for the same reason (two-up pill buttons).</summary>
    public const double DefaultInspectorPixels = 340;

    /// <summary>The grab strip between panes.</summary>
    public const double SplitterPixels = 6;

    /// <summary>How tall the parts-list sheet may grow.</summary>
    public const double ListSheetMaxHeight = 420;

    /// <summary>How tall the inspector sheet may grow — more, because it has the most to show.</summary>
    public const double InspectorSheetMaxHeight = 520;

    /// <summary>
    /// Works out the layout. <paramref name="currentListPixels"/> and
    /// <paramref name="currentInspectorPixels"/> are what the columns are set to right now, so a width
    /// the user dragged survives a trip through the narrow layout instead of snapping back.
    /// </summary>
    public static EditorPageLayout Resolve(
        double width,
        bool hasSelection,
        bool isPieceListOpen,
        double currentListPixels = 0,
        double currentInspectorPixels = 0)
    {
        if (width >= WideThreshold)
        {
            return new EditorPageLayout(
                IsWide: true,
                ListColumn: ColumnWidth.Pixels(currentListPixels > 0 ? currentListPixels : DefaultListPixels),
                InspectorColumn: ColumnWidth.Pixels(
                    currentInspectorPixels > 0 ? currentInspectorPixels : DefaultInspectorPixels),
                SplitterColumns: ColumnWidth.Pixels(SplitterPixels),
                CanvasRowStars: 1,
                ListRowStars: 0,
                ShowSplitters: true,
                List: PaneSlot.InColumn(2),
                Inspector: PaneSlot.InColumn(4));
        }

        return new EditorPageLayout(
            IsWide: false,
            ListColumn: ColumnWidth.Collapsed,
            InspectorColumn: ColumnWidth.Collapsed,
            SplitterColumns: ColumnWidth.Collapsed,
            CanvasRowStars: 1,
            ListRowStars: 0,
            ShowSplitters: false,
            // One sheet at a time: a selection is the more specific thing to be looking at, so the
            // inspector wins and the parts list steps aside rather than stacking two sheets.
            List: PaneSlot.Sheet(ListSheetMaxHeight, isPieceListOpen && !hasSelection),
            Inspector: PaneSlot.Sheet(InspectorSheetMaxHeight, hasSelection));
    }
}
