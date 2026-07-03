using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Views;

public sealed partial class FullscreenView : Page
{
    public string? MediaUrl { get; set; }
    public bool IsVideo { get; set; }
    public event Action<string>? EditFurtherRequested;

    public FullscreenView()
    {
        InitializeComponent();
    }

    public async Task LoadMediaAsync(string url, bool isVideo)
    {
        MediaUrl = url;
        IsVideo = isVideo;

        if (isVideo)
        {
            FullscreenVideo.Visibility = Visibility.Visible;
            FullscreenImage.Visibility = Visibility.Collapsed;
            FullscreenVideo.Source = Microsoft.UI.Xaml.Media.MediaSource.CreateFromUri(new Uri(url));
            FullscreenVideo.MediaPlayer.Play();
            EditBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            FullscreenImage.Visibility = Visibility.Visible;
            FullscreenVideo.Visibility = Visibility.Collapsed;
            var bitmap = new BitmapImage(new Uri(url));
            FullscreenImage.Source = bitmap;
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        if (FullscreenVideo.MediaPlayer != null)
        {
            FullscreenVideo.MediaPlayer.Pause();
        }
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private async void OnDownload(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(MediaUrl)) return;

        var ext = IsVideo ? ".mp4" : ImageHelper.GetImageExtension(MediaUrl);
        var path = await FilePickerHelper.SaveFileAsync($"agent_{DateTime.Now:yyyyMMdd_HHmmss}{ext}", ext);
        if (path != null)
        {
            var bytes = await ImageHelper.DownloadImageAsync(MediaUrl);
            await File.WriteAllBytesAsync(path, bytes);
        }
    }

    private void OnEditFurther(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MediaUrl))
        {
            EditFurtherRequested?.Invoke(MediaUrl);
        }
        if (Frame.CanGoBack)
            Frame.GoBack();
    }
}
