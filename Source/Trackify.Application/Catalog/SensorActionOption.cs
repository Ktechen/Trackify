using Trackify.Domain.Enums;

namespace Trackify.Application.Catalog;

/// <summary>Selectable sensor-action option (value + display label).</summary>
public sealed record SensorActionOption(SensorActionType Value, string Label);
