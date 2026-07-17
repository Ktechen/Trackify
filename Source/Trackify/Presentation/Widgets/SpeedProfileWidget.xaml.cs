using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Trackify.Presentation.Widgets;

/// <summary>
/// Reusable f(x) speed-profile chart. Set <see cref="Graph"/> to the pre-computed
/// <see cref="SpeedProfileGraph"/> of a train or track segment; the paths bind to its path data.
/// </summary>
public sealed partial class SpeedProfileWidget : UserControl
{
    public static readonly DependencyProperty GraphProperty = DependencyProperty.Register(
        nameof(Graph), typeof(SpeedProfileGraph), typeof(SpeedProfileWidget), new PropertyMetadata(null));

    public SpeedProfileWidget() => this.InitializeComponent();

    public SpeedProfileGraph? Graph
    {
        get => (SpeedProfileGraph?)GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }
}
