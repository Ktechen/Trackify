using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Trackify.Presentation.Widgets;

/// <summary>
/// Wraps <see cref="FrameContent"/> with the Industry design system's 4-corner "blueprint"
/// registration marks (styles.css ".corner.tl/tr/bl/br") — the wireframe-card decoration used
/// across every card, dialog and canvas frame in the "Trackify Mobile.dc.html" import.
/// </summary>
[ContentProperty(Name = nameof(FrameContent))]
public sealed partial class BlueprintFrame : UserControl
{
    public static readonly DependencyProperty FrameContentProperty = DependencyProperty.Register(
        nameof(FrameContent), typeof(object), typeof(BlueprintFrame), new PropertyMetadata(null));

    public BlueprintFrame() => this.InitializeComponent();

    public object? FrameContent
    {
        get => GetValue(FrameContentProperty);
        set => SetValue(FrameContentProperty, value);
    }
}
