using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AgentAIStudio.Models;
using AgentAIStudio.Services;
using System.Collections.ObjectModel;

namespace AgentAIStudio.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly CredentialService _credentialService;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private bool _hasApiKey;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private ObservableCollection<ConversationSummary> _chatConversations = [];

    [ObservableProperty]
    private ObservableCollection<ConversationSummary> _studioConversations = [];

    private readonly AgnesApiClient _apiClient;

    public SettingsViewModel(CredentialService credentialService, AgnesApiClient apiClient)
    {
        _credentialService = credentialService;
        _apiClient = apiClient;
    }

    public async Task InitializeAsync()
    {
        var key = await _credentialService.LoadApiKeyAsync();
        HasApiKey = key != null;
        ApiKey = key ?? string.Empty;
        if (key != null)
        {
            _apiClient.SetApiKey(key);
        }
    }

    [RelayCommand]
    public async Task SaveApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "Please enter a valid API key.";
            return;
        }

        await _credentialService.SaveApiKeyAsync(ApiKey);
        _apiClient.SetApiKey(ApiKey);
        HasApiKey = true;
        StatusMessage = "API key saved successfully to Windows Credential Manager.";
    }

    [RelayCommand]
    public async Task ClearApiKey()
    {
        await _credentialService.ClearApiKeyAsync();
        ApiKey = string.Empty;
        HasApiKey = false;
        StatusMessage = "API key removed.";
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        var themeService = new ThemeService();
        themeService.ToggleTheme();
    }

    [RelayCommand]
    public async Task LoadConversations(ConversationStore store)
    {
        var chats = await store.GetConversationsAsync("chat");
        ChatConversations = new ObservableCollection<ConversationSummary>(
            chats.Select(c => new ConversationSummary { Id = c.Id, Title = c.Title, UpdatedAt = c.UpdatedAt }));

        var studios = await store.GetConversationsAsync("imagestudio");
        StudioConversations = new ObservableCollection<ConversationSummary>(
            studios.Select(c => new ConversationSummary { Id = c.Id, Title = c.Title, UpdatedAt = c.UpdatedAt }));
    }

    [RelayCommand]
    public async Task DeleteConversation(Guid id, ConversationStore store)
    {
        await store.DeleteConversationAsync(id);
        ChatConversations.Remove(ChatConversations.FirstOrDefault(c => c.Id == id)!);
        StudioConversations.Remove(StudioConversations.FirstOrDefault(c => c.Id == id)!);
    }
}

public class ConversationSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
