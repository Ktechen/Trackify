namespace Trackify.Models.Trains;

/// <summary>
/// An open end as the canvas shows it: the big "Andockpunkt" dot a drag starts from, and the target
/// a deliberate part (point, crossing, station) is appended to when tapped.
/// </summary>
public partial class TrackAnchor : ObservableObject
{
    /// <summary>Diameter of the dot, so the model can pre-compute its top-left corner.</summary>
    public const double DotSize = 30;

    [ObservableProperty] private double x;
    [ObservableProperty] private double y;
    [ObservableProperty] private bool isActive;

    /// <summary>The part this port belongs to.</summary>
    public required Guid PartId { get; init; }

    /// <summary>Which port of it (see <c>TrackPortNames</c>).</summary>
    public required string PortName { get; init; }

    /// <summary>Canvas position of the dot's top-left corner, since Canvas.Left is not centre-based.</summary>
    public double Left => X - (DotSize / 2);

    /// <inheritdoc cref="Left" />
    public double Top => Y - (DotSize / 2);

    /// <summary>Whether this anchor and <paramref name="other"/> are the same port.</summary>
    public bool Matches(Guid other, string otherPort)
        => PartId == other && string.Equals(PortName, otherPort, StringComparison.Ordinal);

    partial void OnXChanged(double value) => OnPropertyChanged(nameof(Left));

    partial void OnYChanged(double value) => OnPropertyChanged(nameof(Top));
}
