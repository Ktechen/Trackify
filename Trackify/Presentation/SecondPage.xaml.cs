using Microsoft.UI.Xaml.Input;

namespace Trackify.Presentation;

public sealed partial class SecondPage : Page
{
    public SecondPage()
    {
        this.InitializeComponent();
    }

    private void SegmentHit_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TrackSegment segment } && DataContext is SecondViewModel viewModel)
        {
            viewModel.SelectSegmentCommand.Execute(segment);
        }
    }
}
