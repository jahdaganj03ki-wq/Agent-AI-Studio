using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AgentAIStudio.Models;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Controls;

public sealed partial class AdvancedSettingsPanel : UserControl
{
    public string Mode { get; set; } = "chat";

    public AdvancedSettingsPanel()
    {
        InitializeComponent();
    }

    public void SetMode(string mode)
    {
        Mode = mode;
        ChatPanel.Visibility = mode == "chat" ? Visibility.Visible : Visibility.Collapsed;
        ImagePanel.Visibility = mode is "images" or "imagestudio" ? Visibility.Visible : Visibility.Collapsed;
        VideoPanel.Visibility = mode == "video" ? Visibility.Visible : Visibility.Collapsed;

        if (mode is "images" or "imagestudio")
        {
            SizeCombo.ItemsSource = ApiConstants.ImageSizes;
            ModelCombo.ItemsSource = new[] { "agnes-image-2.0-flash", "agnes-image-2.1-flash" };
        }
    }

    public GenerationParameters GetParameters()
    {
        var p = new GenerationParameters();

        if (Mode == "chat")
        {
            p.Temperature = TemperatureSlider.Value;
            p.TopP = TopPSlider.Value;
            if (int.TryParse(MaxTokensBox.Text, out var mt)) p.MaxTokens = mt;
            p.EnableThinking = ThinkingToggle.IsOn;
        }

        if (Mode is "images" or "imagestudio")
        {
            if (SizeCombo.SelectedItem is string size) p.Size = size;
            p.Model = ModelCombo.SelectedItem as string ?? ApiConstants.ImageModel21;
            if (int.TryParse(SeedBox.Text, out var seed)) p.Seed = seed;
        }

        if (Mode == "video")
        {
            if (int.TryParse(WidthBox.Text, out var w)) p.Width = w;
            if (int.TryParse(HeightBox.Text, out var h)) p.Height = h;
            if (int.TryParse(NumFramesBox.Text, out var nf)) p.NumFrames = nf;
            if (double.TryParse(FrameRateBox.Text, out var fr)) p.FrameRate = fr;
            p.NegativePrompt = string.IsNullOrWhiteSpace(NegativePromptBox.Text) ? null : NegativePromptBox.Text;
            if (int.TryParse(SeedBoxVideo.Text, out var sv)) p.Seed = sv;
        }

        return p;
    }
}
