using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using AgentAIStudio.Models;

namespace AgentAIStudio.Controls;

public sealed partial class ImageStudioBubble : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(Message), typeof(ImageStudioBubble),
            new PropertyMetadata(null, OnMessageChanged));

    public static readonly DependencyProperty HasBranchProperty =
        DependencyProperty.Register(nameof(HasBranch), typeof(bool), typeof(ImageStudioBubble),
            new PropertyMetadata(false));

    public Message? Message
    {
        get => (Message?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool HasBranch
    {
        get => (bool)GetValue(HasBranchProperty);
        set => SetValue(HasBranchProperty, value);
    }

    public event Action<string>? EditFurtherRequested;
    public event Action<string>? RegenerateRequested;
    public event Action<string>? FullscreenRequested;
    public event Action<string, bool>? DownloadRequested;

    public ImageStudioBubble()
    {
        InitializeComponent();
    }

    private static async void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageStudioBubble bubble && e.NewValue is Message msg)
        {
            await bubble.UpdateUI(msg);
        }
    }

    private async Task UpdateUI(Message msg)
    {
        BranchIndicator.Visibility = (msg.ParentId.HasValue && msg.BranchIndex > 0)
            ? Visibility.Visible : Visibility.Collapsed;
        BranchLabel.Text = msg.BranchIndex > 0 ? $"B{msg.BranchIndex}" : "";

        var isUser = msg.Role == "user";
        Bubble.HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        Bubble.Background = new SolidColorBrush(isUser
            ? Microsoft.UI.Color.FromArgb(40, 0, 120, 212)
            : Microsoft.UI.Color.FromArgb(30, 128, 128, 128));

        RoleLabel.Text = isUser ? "You" : "Studio";
        PromptText.Text = msg.Prompt;

        if (!isUser && msg.MediaType == MediaType.Image && !string.IsNullOrEmpty(msg.GeneratedUrl))
        {
            ImageBorder.Visibility = Visibility.Visible;
            ActionBar.Visibility = Visibility.Visible;
            ActionBar.MediaUrl = msg.GeneratedUrl;
            ActionBar.IsVideo = false;
            ActionBar.ModelName = "Agnes AI";

            try
            {
                var bitmap = new BitmapImage(new Uri(msg.GeneratedUrl));
                MediaImage.Source = bitmap;
            }
            catch { }

            ActionBar.EditFurtherRequested += (url) => EditFurtherRequested?.Invoke(url);
            ActionBar.RegenerateRequested += (url) => RegenerateRequested?.Invoke(url);
            ActionBar.FullscreenRequested += (url) => FullscreenRequested?.Invoke(url);
            ActionBar.DownloadRequested += (url, isVideo) => DownloadRequested?.Invoke(url, isVideo);
        }
        else
        {
            ImageBorder.Visibility = Visibility.Collapsed;
            ActionBar.Visibility = Visibility.Collapsed;
        }
    }
}
