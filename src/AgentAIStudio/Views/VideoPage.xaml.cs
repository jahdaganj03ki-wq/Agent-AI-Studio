using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AgentAIStudio.ViewModels;
using AgentAIStudio.Services;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Views;

public sealed partial class VideoPage : Page
{
    public VideoViewModel ViewModel { get; }

    private readonly AgnesApiClient _apiClient;

    public VideoPage()
    {
        InitializeComponent();

        _apiClient = new AgnesApiClient();
        var videoService = new VideoService(_apiClient);
        ViewModel = new VideoViewModel(videoService);

        _ = LoadApiKeyAsync();
    }

    private async Task LoadApiKeyAsync()
    {
        var credService = new CredentialService();
        var key = await credService.LoadApiKeyAsync();
        if (key != null) _apiClient.SetApiKey(key);
    }

    private void OnAdvancedToggle(object sender, RoutedEventArgs e)
    {
        AdvancedPanel.Visibility = AdvancedPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        if (AdvancedPanel.Visibility == Visibility.Visible)
            AdvancedPanel.SetMode("video");
    }

    private void OnUploadImage(object sender, RoutedEventArgs e)
    {
        // Image upload handled via ViewModel
    }

    private void OnAspectClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            var parts = tag.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
            {
                ViewModel.Parameters.Width = w;
                ViewModel.Parameters.Height = h;
            }
        }
    }
}
