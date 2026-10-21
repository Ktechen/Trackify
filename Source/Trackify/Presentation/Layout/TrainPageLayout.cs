namespace Trackify.Presentation.Layout;

/// <summary>
/// The Zugkonfiguration page's responsive layout, resolved from width and selection alone — a pure
/// value, for the same reason as <see cref="EditorPageLayout"/>.
/// <para>
/// Wide: list rail | editor, side by side. Narrow it becomes master-detail: the list is the home pane
/// and a selected train replaces it full width, with the back arrow as the way out.
/// </para>
/// </summary>
public record TrainPageLayout(
    bool IsWide,
    ColumnWidth ListColumn,
    ColumnWidth SplitterColumn,
    ColumnWidth EditorColumn,
    bool ShowList,
    bool ShowSplitter,
    bool ShowEditor,
    bool ShowBackButton,
    bool ShowHeaderEmblem,
    bool ShowHeaderStats,
    bool ShowConnectionChip)
{
    /// <summary>Below this width the page becomes master-detail.</summary>
    public const double WideThreshold = 720;

    /// <summary>The list rail's default column width.</summary>
    public const double DefaultListPixels = 346;

    /// <summary>The grab strip between rail and editor.</summary>
    public const double SplitterPixels = 6;

    /// <summary>
    /// Works out the layout. <paramref name="currentListPixels"/> is the rail's present width, so a
    /// width the user dragged is not thrown away when the window passes through the narrow layout.
    /// </summary>
    public static TrainPageLayout Resolve(double width, bool hasSelection, double currentListPixels = 0)
    {
        var isWide = width >= WideThreshold;

        // The emblem, the counts and the connection chip all cost header width that a phone does not
        // have; the chip additionally needs a selected train to say anything about.
        var header = (
            Back: !isWide && hasSelection,
            Emblem: isWide,
            Stats: isWide,
            Chip: isWide && hasSelection);

        if (isWide)
        {
            return new TrainPageLayout(
                IsWide: true,
                ListColumn: ColumnWidth.Pixels(currentListPixels > 0 ? currentListPixels : DefaultListPixels),
                SplitterColumn: ColumnWidth.Pixels(SplitterPixels),
                EditorColumn: ColumnWidth.Star,
                ShowList: true,
                ShowSplitter: true,
                ShowEditor: true,
                header.Back, header.Emblem, header.Stats, header.Chip);
        }

        if (hasSelection)
        {
            // Narrow with a train chosen: the editor takes the window.
            return new TrainPageLayout(
                IsWide: false,
                ListColumn: ColumnWidth.Collapsed,
                SplitterColumn: ColumnWidth.Collapsed,
                EditorColumn: ColumnWidth.Star,
                ShowList: false,
                ShowSplitter: false,
                ShowEditor: true,
                header.Back, header.Emblem, header.Stats, header.Chip);
        }

        // Narrow with nothing chosen: the list is the home pane.
        return new TrainPageLayout(
            IsWide: false,
            ListColumn: ColumnWidth.Star,
            SplitterColumn: ColumnWidth.Collapsed,
            EditorColumn: ColumnWidth.Collapsed,
            ShowList: true,
            ShowSplitter: false,
            ShowEditor: false,
            header.Back, header.Emblem, header.Stats, header.Chip);
    }
}
