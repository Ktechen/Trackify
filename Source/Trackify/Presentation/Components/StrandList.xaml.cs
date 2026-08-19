using Microsoft.UI.Xaml.Controls;

namespace Trackify.Presentation.Components;

/// <summary>Strand-grouped segment list (numbered rows, reorder/duplicate/delete). Binds to the hosting SecondViewModel.</summary>
public sealed partial class StrandList : UserControl
{
    public StrandList() => this.InitializeComponent();
}
