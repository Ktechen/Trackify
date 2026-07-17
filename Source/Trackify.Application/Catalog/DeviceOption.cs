
namespace Trackify.Application.Catalog;

/// <summary>Selectable port-device option (value + display labels).</summary>
public sealed record DeviceOption(DeviceType Value, string Label, string Short, string ListLabel);
