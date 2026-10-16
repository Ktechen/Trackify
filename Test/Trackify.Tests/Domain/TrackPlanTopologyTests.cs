using Trackify.Domain.Tracks;

namespace Trackify.Tests.Domain;

/// <summary>
/// The plan's structural edits. Every one returns a new topology rather than mutating, which is what
/// makes the editor's undo stack a plain stack of these values.
/// </summary>
public class TrackPlanTopologyTests
{
    [Fact]
    public void An_empty_topology_holds_nothing()
    {
        // Act
        var topology = TrackPlanTopology.Empty;

        // Assert
        Assert.Empty(topology.Parts);
        Assert.Empty(topology.Links);
    }

    [Fact]
    public void Adding_a_part_leaves_the_original_untouched()
    {
        // Arrange
        var original = TrackPlanTopology.Empty;

        // Act
        var grown = original.Add(new TrackPlanPart(Guid.NewGuid(), TrackPartCatalog.StraightKey));

        // Assert — immutability is the property the undo stack depends on.
        Assert.Empty(original.Parts);
        Assert.Single(grown.Parts);
    }

    [Fact]
    public void A_part_can_be_found_by_its_id()
    {
        // Arrange
        var part = new TrackPlanPart(Guid.NewGuid(), TrackPartCatalog.CurveLeftKey);
        var topology = TrackPlanTopology.Empty.Add(part);

        // Act
        var found = topology.TryGetPart(part.Id, out var retrieved);

        // Assert
        Assert.True(found);
        Assert.Equal(TrackPartCatalog.CurveLeftKey, retrieved.SpecKey);
    }

    [Fact]
    public void Looking_for_a_part_that_is_not_there_reports_so_instead_of_throwing()
    {
        // Arrange
        var topology = TrackPlanTopology.Empty;

        // Act
        var found = topology.TryGetPart(Guid.NewGuid(), out var retrieved);

        // Assert
        Assert.False(found);
        Assert.Equal(default, retrieved);
    }

    [Fact]
    public void A_connected_port_reads_as_used_from_either_side()
    {
        // Arrange
        var (topology, first, second) = TwoConnectedStraights();

        // Act / Assert
        Assert.True(topology.IsPortUsed(first, TrackPortNames.End));
        Assert.True(topology.IsPortUsed(second, TrackPortNames.Start));
        Assert.False(topology.IsPortUsed(first, TrackPortNames.Start));
        Assert.False(topology.IsPortUsed(second, TrackPortNames.End));
    }

    [Fact]
    public void Disconnecting_a_port_keeps_both_parts()
    {
        // Arrange
        var (topology, first, second) = TwoConnectedStraights();

        // Act
        var unplugged = topology.Disconnect(first, TrackPortNames.End);

        // Assert
        Assert.Equal(2, unplugged.Parts.Count);
        Assert.Empty(unplugged.Links);
        Assert.False(unplugged.IsPortUsed(second, TrackPortNames.Start));
    }

    [Fact]
    public void Disconnecting_a_port_that_holds_nothing_changes_nothing()
    {
        // Arrange
        var (topology, first, _) = TwoConnectedStraights();

        // Act
        var unchanged = topology.Disconnect(first, TrackPortNames.Start);

        // Assert
        Assert.Single(unchanged.Links);
    }

    [Fact]
    public void Removing_a_part_takes_every_connection_that_touched_it()
    {
        // Arrange
        var (topology, first, second) = TwoConnectedStraights();

        // Act
        var trimmed = topology.Remove(second);

        // Assert
        Assert.Equal(first, Assert.Single(trimmed.Parts).Id);
        Assert.Empty(trimmed.Links);
    }

    [Fact]
    public void Removing_a_part_that_is_not_there_changes_nothing()
    {
        // Arrange
        var (topology, _, _) = TwoConnectedStraights();

        // Act
        var unchanged = topology.Remove(Guid.NewGuid());

        // Assert
        Assert.Equal(2, unchanged.Parts.Count);
        Assert.Single(unchanged.Links);
    }

    [Fact]
    public void A_link_can_be_read_from_either_end()
    {
        // Arrange
        var (_, first, second) = TwoConnectedStraights();
        var link = new TrackPlanLink(first, TrackPortNames.End, second, TrackPortNames.Start);

        // Act
        var flipped = link.Flipped();

        // Assert
        Assert.Equal(second, flipped.FromPartId);
        Assert.Equal(TrackPortNames.Start, flipped.FromPort);
        Assert.Equal(first, flipped.ToPartId);
        Assert.Equal(TrackPortNames.End, flipped.ToPort);
        Assert.True(link.Uses(first, TrackPortNames.End));
        Assert.True(link.Uses(second, TrackPortNames.Start));
        Assert.False(link.Uses(first, TrackPortNames.Start));
    }

    private static (TrackPlanTopology Topology, Guid First, Guid Second) TwoConnectedStraights()
    {
        var first = new TrackPlanPart(Guid.NewGuid(), TrackPartCatalog.StraightKey);
        var second = new TrackPlanPart(Guid.NewGuid(), TrackPartCatalog.StraightKey);

        var topology = TrackPlanTopology.Empty
            .Add(first)
            .Add(second)
            .Connect(new TrackPlanLink(first.Id, TrackPortNames.End, second.Id, TrackPortNames.Start));

        return (topology, first.Id, second.Id);
    }
}
