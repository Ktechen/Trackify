
namespace Trackify.Application.Catalog;

/// <summary>Selectable LED-colour option (value + display name + hex swatch).</summary>
public sealed record ColorOption(LedColorType Value, string Name, string Hex);
