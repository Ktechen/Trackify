
namespace Trackify.Application.Catalog;

/// <summary>Selectable hub-model option (value + display labels).</summary>
public sealed record HubOption(HubType Value, string Label, string Short);
