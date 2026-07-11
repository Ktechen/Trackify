namespace Trackify.Presentation;

/// <summary>A single LED color swatch shown in the Farbe &amp; Hub-LED section, tracking whether it's the train's current color.</summary>
public partial class ColorSwatchItem : ObservableObject
{
    public required LedColor Value { get; init; }
    public required string Name { get; init; }
    public required string Hex { get; init; }

    [ObservableProperty] private bool isSelected;
}
