using Microsoft.UI.Xaml.Controls;

namespace Trackify.Presentation.Components;

/// <summary>Train editor (connection / ports / drive / colour). Binds to the hosting MainViewModel.</summary>
public sealed partial class TrainEditor : UserControl
{
    public TrainEditor() => this.InitializeComponent();
}
