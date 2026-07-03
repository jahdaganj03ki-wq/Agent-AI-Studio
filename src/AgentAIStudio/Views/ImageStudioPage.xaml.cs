using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using AgentAIStudio.ViewModels;
using AgentAIStudio.Services;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Views;

public sealed partial class ImageStudioPage : Page
{
    public ImageStudioViewModel ViewModel { get; }

    public ImageStudioPage()
    {
        InitializeComponent();

        var apiClient = new AgnesApiClient();
        var imageService = new ImageService(apiClient);
        var store = new ConversationStore();
        ViewModel = new ImageStudioViewModel(imageService, store);

        ModelCombo.ItemsSource = new[] { ApiConstants.ImageModel20, ApiConstants.ImageModel21 };
        SizeCombo.ItemsSource = ApiConstants.ImageSizes;

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
            AdvancedPanel.SetMode("imagestudio");
    }

    private void OnClearUploads(object sender, RoutedEventArgs e)
    {
        ViewModel.EditingImageBase64 = null;
        ViewModel.EditingImageUrl = null;
        ViewModel.EditingImageName = null;
        ViewModel.UploadedImageBase64s = [];
        ViewModel.IsMultiImageMode = false;
        UploadPreview.Visibility = Visibility.Collapsed;
    }

    private void OnResetToTextToImage(object sender, RoutedEventArgs e)
    {
        OnClearUploads(sender, e);
        ViewModel.InputPrompt = string.Empty;
    }

    private void PromptBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && !ViewModel.IsGenerating)
        {
            ViewModel.GenerateOrEditCommand.Execute(null);
            e.Handled = true;
        }
    }
}
