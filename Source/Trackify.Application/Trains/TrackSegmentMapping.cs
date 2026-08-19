namespace Trackify.Application.Trains;

/// <summary>Maps between the Domain entity <see cref="TrackSegment"/> and the boundary <see cref="TrackSegmentDto"/>.</summary>
public static class TrackSegmentMapping
{
    public static TrackSegmentDto ToDto(this TrackSegment segment) => new()
    {
        Id = segment.Id,
        Name = segment.Name,
        Type = segment.Type,
        MaxSpeed = segment.MaxSpeed,
        Direction = segment.Direction,
        AccelFn = segment.AccelFn,
        BrakeFn = segment.BrakeFn,
        Sensor = segment.Sensor,
        Action = segment.Action,
        SlowTarget = segment.SlowTarget,
        StrandId = segment.StrandId,
        Order = segment.Order,
        Curve = segment.Curve,
        BranchStrandId = segment.BranchStrandId,
        BranchDirection = segment.BranchDirection,
        Route = segment.Route,
        BranchLinkSegmentId = segment.BranchLinkSegmentId,
    };

    public static TrackSegment ToEntity(this TrackSegmentDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Type = dto.Type,
        MaxSpeed = dto.MaxSpeed,
        Direction = dto.Direction,
        AccelFn = dto.AccelFn,
        BrakeFn = dto.BrakeFn,
        Sensor = dto.Sensor,
        Action = dto.Action,
        SlowTarget = dto.SlowTarget,
        StrandId = dto.StrandId,
        Order = dto.Order,
        Curve = dto.Curve,
        BranchStrandId = dto.BranchStrandId,
        BranchDirection = dto.BranchDirection,
        Route = dto.Route,
        BranchLinkSegmentId = dto.BranchLinkSegmentId,
    };
}
