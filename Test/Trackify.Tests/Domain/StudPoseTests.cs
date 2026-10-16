using Trackify.Domain.Tracks;

namespace Trackify.Tests.Domain;

public class StudPoseTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(16, 0)]
    [InlineData(-1, 15)]
    [InlineData(17, 1)]
    [InlineData(-17, 15)]
    public void NormalizeHeading_wraps_into_the_sixteen_steps(int heading, int expected)
        => Assert.Equal(expected, TrackMetrics.NormalizeHeading(heading));

    [Fact]
    public void Advance_moves_along_the_heading()
    {
        var moved = new StudPose(0, 0, 0).Advance(16);

        Assert.Equal(16, moved.X, 6);
        Assert.Equal(0, moved.Y, 6);
        Assert.Equal(0, moved.Heading);
    }

    [Fact]
    public void Advance_at_a_quarter_turn_moves_down_because_Y_runs_down()
    {
        // Heading 4 of 16 is 90 degrees, which is +Y on a canvas.
        var moved = new StudPose(0, 0, TrackMetrics.HeadingCount / 4).Advance(16);

        Assert.Equal(0, moved.X, 6);
        Assert.Equal(16, moved.Y, 6);
    }

    [Fact]
    public void Compose_with_the_inverse_returns_to_the_origin()
    {
        var pose = new StudPose(13.5, -7.25, 5);

        var round = pose.Compose(pose.Inverse());

        Assert.Equal(0, round.X, 9);
        Assert.Equal(0, round.Y, 9);
        Assert.Equal(0, round.Heading);
    }

    [Fact]
    public void Compose_is_associative_so_parts_can_be_nested_safely()
    {
        var a = new StudPose(4, 9, 3);
        var b = new StudPose(-2, 5, 7);
        var c = new StudPose(11, -3, 2);

        var left = a.Compose(b).Compose(c);
        var right = a.Compose(b.Compose(c));

        Assert.Equal(left.X, right.X, 9);
        Assert.Equal(left.Y, right.Y, 9);
        Assert.Equal(left.Heading, right.Heading);
    }

    [Fact]
    public void Reversed_keeps_the_point_and_flips_the_facing()
    {
        var reversed = new StudPose(5, -3, 2).Reversed();

        Assert.Equal(5, reversed.X, 9);
        Assert.Equal(-3, reversed.Y, 9);
        Assert.Equal(10, reversed.Heading);
    }

    [Fact]
    public void Joins_is_true_for_two_ports_facing_each_other_at_one_point()
    {
        var port = new StudPose(16, 0, 0);
        var facing = new StudPose(16, 0, 8);

        Assert.True(port.Joins(facing));
        Assert.True(facing.Joins(port));
    }

    [Fact]
    public void Joins_is_false_when_the_facings_are_not_opposite()
        => Assert.False(new StudPose(16, 0, 0).Joins(new StudPose(16, 0, 7)));

    [Fact]
    public void Joins_is_false_when_the_points_are_further_apart_than_the_tolerance()
        => Assert.False(new StudPose(0, 0, 0).Joins(new StudPose(TrackMetrics.JoinToleranceStuds * 2, 0, 8)));
}
