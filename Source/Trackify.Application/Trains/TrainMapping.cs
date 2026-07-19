namespace Trackify.Application.Trains;

/// <summary>
/// Maps between the Domain entity <see cref="Train"/> and the boundary <see cref="TrainDto"/>.
/// The repository works in entities (a persistence detail); everything above the use-case layer
/// works in DTOs. <c>ToEntity</c> preserves the <see cref="Train.Id"/> so a loaded-edited-saved
/// round-trip updates the same row; the audit timestamps are managed by the entity/repository.
/// </summary>
public static class TrainMapping
{
    public static TrainDto ToDto(this Train train) => new()
    {
        Id = train.Id,
        Name = train.Name,
        Hub = train.Hub,
        BleAddress = train.BleAddress,
        HubId = train.HubId,
        Color = train.Color,
        PortA = train.PortA,
        PortB = train.PortB,
        Speed = train.Speed,
        AccelFn = train.AccelFn,
        AccelExpression = train.AccelExpression,
        BrakeFn = train.BrakeFn,
        BrakeExpression = train.BrakeExpression,
        IsActive = train.IsActive,
    };

    public static Train ToEntity(this TrainDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Hub = dto.Hub,
        BleAddress = dto.BleAddress,
        HubId = dto.HubId,
        Color = dto.Color,
        PortA = dto.PortA,
        PortB = dto.PortB,
        Speed = dto.Speed,
        AccelFn = dto.AccelFn,
        AccelExpression = dto.AccelExpression,
        BrakeFn = dto.BrakeFn,
        BrakeExpression = dto.BrakeExpression,
        IsActive = dto.IsActive,
    };
}
