using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AgentAIStudio.ViewModels;
using AgentAIStudio.Services;

namespace AgentAIStudio.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    private readonly AgnesApiClient _apiClient;
    private readonly ConversationStore _store;

    public SettingsPage()
    {
        InitializeComponent();

        _apiClient = new AgnesApiClient();
        _store = new ConversationStore();
        var credentialService = new CredentialService();
        ViewModel = new SettingsViewModel(credentialService, _apiClient);

        _ = ViewModel.InitializeAsync();
        _ = ViewModel.LoadConversationsCommand.ExecuteAsync(_store);
    }

    private void OnThemeToggled(object sender, RoutedEventArgs e)
    {
        var themeService = new ThemeService();
        themeService.ToggleTheme();
    }

    private async void OnRefreshConversations(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadConversationsCommand.ExecuteAsync(_store);
    }

    private async void OnDeleteConversation(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            await ViewModel.DeleteConversationCommand.ExecuteAsync((id, _store));
        }
    }
}
