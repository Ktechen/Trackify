namespace Trackify.Presentation.ViewModels;

/// <summary>
/// One button in the parts palette: a catalogue part plus the port a deliberate "anhängen" enters it
/// by. Data rather than seven hand-written buttons, because a fixed row of them overflows a phone —
/// as a bound collection the palette wraps into however many rows the width allows.
/// </summary>
public record PaletteItem(string Label, string PartKey, string EntryPort);
