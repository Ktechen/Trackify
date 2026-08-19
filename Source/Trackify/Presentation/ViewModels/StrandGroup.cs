using System.Collections.ObjectModel;
using TrackSegment = Trackify.Models.Trains.TrackSegment;

namespace Trackify.Presentation.ViewModels;

/// <summary>
/// One strand in the Streckenplaner: the main strand (<see cref="Guid.Empty"/>) or a Weiche's branch.
/// Doubles as both the "Baue an" chip (Name/Count/IsActive) and the strand-grouped segment list's
/// section header + rows.
/// </summary>
public partial class StrandGroup : ObservableObject
{
    public required Guid StrandId { get; init; }

    [ObservableProperty] private string name = "";
    [ObservableProperty] private int count;
    [ObservableProperty] private bool isActive;

    public ObservableCollection<TrackSegment> Rows { get; } = [];
}
