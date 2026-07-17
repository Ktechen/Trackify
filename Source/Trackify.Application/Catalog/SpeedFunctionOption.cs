using Trackify.Domain.Enums;

namespace Trackify.Application.Catalog;

/// <summary>Selectable speed-curve option (value + display label + formula text).</summary>
public sealed record SpeedFunctionOption(SpeedFunctionType Value, string Label, string Formula);
