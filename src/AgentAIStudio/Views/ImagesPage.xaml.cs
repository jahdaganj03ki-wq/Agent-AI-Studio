using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AgentAIStudio.ViewModels;
using AgentAIStudio.Services;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Views;

public sealed partial class ImagesPage : Page
{
    public ImagesViewModel ViewModel { get; }

    public ImagesPage()
    {
        InitializeComponent();

        var apiClient = new AgnesApiClient();
        var imageService = new ImageService(apiClient);
        ViewModel = new ImagesViewModel(imageService);

        ModelSelector.ItemsSource = new[] { ApiConstants.ImageModel20, ApiConstants.ImageModel21 };
        SizeSelector.ItemsSource = ApiConstants.ImageSizes;

        _ = LoadApiKeyAsync(apiClient);
    }

    private async Task LoadApiKeyAsync(AgnesApiClient client)
    {
        var credService = new CredentialService();
        var key = await credService.LoadApiKeyAsync();
        if (key != null) client.SetApiKey(key);
    }

    private void OnAdvancedToggle(object sender, RoutedEventArgs e)
    {
        AdvancedPanel.Visibility = AdvancedPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        if (AdvancedPanel.Visibility == Visibility.Visible)
            AdvancedPanel.SetMode("images");
    }
}
