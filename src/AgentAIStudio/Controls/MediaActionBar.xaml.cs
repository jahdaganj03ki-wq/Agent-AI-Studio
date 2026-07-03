using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Controls;

public sealed partial class MediaActionBar : UserControl
{
    public static readonly DependencyProperty MediaUrlProperty =
        DependencyProperty.Register(nameof(MediaUrl), typeof(string), typeof(MediaActionBar), new PropertyMetadata(null));

    public static readonly DependencyProperty IsVideoProperty =
        DependencyProperty.Register(nameof(IsVideo), typeof(bool), typeof(MediaActionBar), new PropertyMetadata(false));

    public static readonly DependencyProperty ModelNameProperty =
        DependencyProperty.Register(nameof(ModelName), typeof(string), typeof(MediaActionBar), new PropertyMetadata(null, OnModelChanged));

    public string? MediaUrl
    {
        get => (string?)GetValue(MediaUrlProperty);
        set => SetValue(MediaUrlProperty, value);
    }

    public bool IsVideo
    {
        get => (bool)GetValue(IsVideoProperty);
        set => SetValue(IsVideoProperty, value);
    }

    public string? ModelName
    {
        get => (string?)GetValue(ModelNameProperty);
        set => SetValue(ModelNameProperty, value);
    }

    public event Action<string>? EditFurtherRequested;
    public event Action<string>? RegenerateRequested;
    public event Action<string>? FullscreenRequested;
    public event Action<string, bool>? DownloadRequested;

    public MediaActionBar()
    {
        InitializeComponent();
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MediaActionBar bar && bar.ModelLabel != null)
        {
            bar.ModelLabel.Text = e.NewValue?.ToString() ?? "";
        }
    }

    private void OnEditFurtherClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MediaUrl))
            EditFurtherRequested?.Invoke(MediaUrl);
    }

    private void OnRegenerateClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MediaUrl))
            RegenerateRequested?.Invoke(MediaUrl);
    }

    private void OnFullscreenClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MediaUrl))
            FullscreenRequested?.Invoke(MediaUrl);
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(MediaUrl)) return;

        var ext = IsVideo ? ".mp4" : ImageHelper.GetImageExtension(MediaUrl);
        var suggestedName = $"agent_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
        var path = await FilePickerHelper.SaveFileAsync(suggestedName, ext);
        if (path != null)
        {
            DownloadRequested?.Invoke(MediaUrl, IsVideo);
        }
    }
}
