namespace Trackify.Helpers;

/// <summary>A whole plan laid out on the canvas: the pieces, the open ends, and the ballast behind them.</summary>
public sealed record FittedPlan
{
    /// <summary>Nothing laid yet.</summary>
    public static FittedPlan Empty { get; } = new()
    {
        Pieces = [],
        Anchors = [],
        BedPathData = "",
        PreviewPathData = "",
        StudsPerPixel = 1,
    };

    /// <summary>Every part, committed ones and the ghost preview alike.</summary>
    public required IReadOnlyList<FittedPiece> Pieces { get; init; }

    /// <summary>The open ends, as tappable dots.</summary>
    public required IReadOnlyList<FittedAnchor> Anchors { get; init; }

    /// <summary>All committed rail as one path, drawn thick underneath as ballast.</summary>
    public required string BedPathData { get; init; }

    /// <summary>The ghost preview as one path, drawn on top while a drag is in flight.</summary>
    public required string PreviewPathData { get; init; }

    /// <summary>
    /// How many studs one canvas pixel is worth. Part of turning a drag in canvas space back into
    /// the studs the geometry layer works in — see <see cref="ToStuds"/>.
    /// </summary>
    public required double StudsPerPixel { get; init; }

    /// <summary>Canvas X the plan's world origin landed on.</summary>
    public double OriginX { get; init; }

    /// <summary>Canvas Y the plan's world origin landed on.</summary>
    public double OriginY { get; init; }

    /// <summary>
    /// A canvas point back in world studs — the inverse of the fit. A gesture arrives in canvas
    /// coordinates, but the snapper only ever reasons in studs, so this is the one place the two
    /// spaces meet.
    /// </summary>
    public StudPoint ToStuds(double canvasX, double canvasY)
        => new((canvasX - OriginX) * StudsPerPixel, (canvasY - OriginY) * StudsPerPixel);
}
