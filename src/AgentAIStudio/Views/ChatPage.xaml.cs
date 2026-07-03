using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using AgentAIStudio.ViewModels;
using AgentAIStudio.Services;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Views;

public sealed partial class ChatPage : Page
{
    public ChatViewModel ViewModel { get; }

    public ChatPage()
    {
        InitializeComponent();

        var apiClient = new AgnesApiClient();
        var chatService = new ChatService(apiClient);
        var store = new ConversationStore();
        ViewModel = new ChatViewModel(chatService, store);

        // Load API key from credential manager
        _ = LoadApiKeyAsync(apiClient);

        // Listen for input changes to auto-scroll
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.Messages))
            {
                ScrollToBottom();
            }
        };
    }

    private async Task LoadApiKeyAsync(AgnesApiClient client)
    {
        var credService = new CredentialService();
        var key = await credService.LoadApiKeyAsync();
        if (key != null)
        {
            client.SetApiKey(key);
        }
    }

    private void ScrollToBottom()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            MessageScroll.ChangeView(null, MessageScroll.ScrollableHeight, null);
        });
    }

    private void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && !ViewModel.IsStreaming)
        {
            ViewModel.SendMessageCommand.Execute(null);
            e.Handled = true;
        }
    }
}
