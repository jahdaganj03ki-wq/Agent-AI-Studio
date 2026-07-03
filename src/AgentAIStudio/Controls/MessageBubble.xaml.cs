using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using AgentAIStudio.Models;

namespace AgentAIStudio.Controls;

public sealed partial class MessageBubble : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(Message), typeof(MessageBubble),
            new PropertyMetadata(null, OnMessageChanged));

    public Message? Message
    {
        get => (Message?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public event Action<string>? EditFurtherRequested;
    public event Action<string>? RegenerateRequested;
    public event Action<string>? FullscreenRequested;
    public event Action<string, bool>? DownloadRequested;

    public MessageBubble()
    {
        InitializeComponent();
    }

    private static async void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MessageBubble bubble && e.NewValue is Message msg)
        {
            await bubble.UpdateUI(msg);
        }
    }

    private async Task UpdateUI(Message msg)
    {
        RoleLabel.Text = msg.Role == "user" ? "You" : "Assistant";
        ContentText.Text = msg.Role == "user" ? msg.Prompt : msg.Prompt;

        var isUser = msg.Role == "user";
        BubbleBorder.HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        BubbleBorder.Background = new SolidColorBrush(isUser
            ? Microsoft.UI.Color.FromArgb(40, 0, 120, 212)
            : Microsoft.UI.Color.FromArgb(30, 128, 128, 128));

        if (msg.MediaType == MediaType.Image && !string.IsNullOrEmpty(msg.GeneratedUrl))
        {
            ImageBorder.Visibility = Visibility.Visible;
            ActionBar.Visibility = Visibility.Visible;
            ActionBar.MediaUrl = msg.GeneratedUrl;
            ActionBar.IsVideo = false;
            ActionBar.ModelName = "Agnes AI";

            var bitmap = new BitmapImage(new Uri(msg.GeneratedUrl));
            MediaImage.Source = bitmap;

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
