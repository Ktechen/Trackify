using Microsoft.UI.Xaml.Controls;

namespace Trackify.Presentation.Components;

/// <summary>2D track layout (segments + sensors). Binds to the hosting SecondViewModel.</summary>
public sealed partial class TrackCanvas : UserControl
{
    public TrackCanvas() => this.InitializeComponent();
}
