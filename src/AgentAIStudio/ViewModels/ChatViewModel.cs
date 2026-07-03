using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AgentAIStudio.Models;
using AgentAIStudio.Services;

namespace AgentAIStudio.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly ChatService _chatService;
    private readonly ConversationStore _store;

    [ObservableProperty]
    private ObservableCollection<Message> _messages = [];

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private string? _attachedImageBase64;

    [ObservableProperty]
    private string? _attachedImageName;

    [ObservableProperty]
    private Conversation? _currentConversation;

    [ObservableProperty]
    private bool _useThinking;

    public GenerationParameters Parameters { get; set; } = new();

    public ChatViewModel(ChatService chatService, ConversationStore store)
    {
        _chatService = chatService;
        _store = store;
        LogService.Instance.LogInfo("ViewModel", "ChatViewModel initialized");
    }

    [RelayCommand]
    public async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputText) && string.IsNullOrEmpty(AttachedImageBase64))
            return;

        var text = InputText;
        var image = AttachedImageBase64;
        InputText = string.Empty;
        AttachedImageBase64 = null;
        AttachedImageName = null;

        LogService.Instance.LogInfo("ViewModel", "Sending chat message", new
        {
            textLength = text.Length,
            hasImage = image != null
        });

        var userMsg = new Message
        {
            Role = "user",
            Prompt = text,
            MediaType = MediaType.None,
            Status = MessageStatus.Complete,
            SortOrder = Messages.Count,
            Timestamp = DateTime.UtcNow
        };
        Messages.Add(userMsg);

        IsStreaming = true;

        var assistantMsg = new Message
        {
            Role = "assistant",
            Prompt = string.Empty,
            MediaType = MediaType.None,
            Status = MessageStatus.Generating,
            SortOrder = Messages.Count,
            Timestamp = DateTime.UtcNow
        };
        Messages.Add(assistantMsg);

        try
        {
            var fullResponse = new System.Text.StringBuilder();
            await foreach (var chunk in _chatService.CreateStreamingAsync(text, Parameters, image))
            {
                fullResponse.Append(chunk);
                assistantMsg.Prompt = fullResponse.ToString();

                var idx = Messages.IndexOf(assistantMsg);
                if (idx >= 0)
                {
                    Messages.RemoveAt(idx);
                    Messages.Insert(idx, assistantMsg);
                }
            }

            assistantMsg.Status = MessageStatus.Complete;
            var idx2 = Messages.IndexOf(assistantMsg);
            if (idx2 >= 0)
            {
                Messages.RemoveAt(idx2);
                Messages.Insert(idx2, assistantMsg);
            }

            LogService.Instance.LogInfo("ViewModel", "Chat response completed", new
            {
                responseLength = fullResponse.Length
            });
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("ViewModel", "Chat send failed", ex);
            assistantMsg.Prompt = $"Error: {ex.Message}";
            assistantMsg.Status = MessageStatus.Failed;
            var idx = Messages.IndexOf(assistantMsg);
            if (idx >= 0)
            {
                Messages.RemoveAt(idx);
                Messages.Insert(idx, assistantMsg);
            }
        }

        IsStreaming = false;
        await PersistAsync();
    }

    [RelayCommand]
    public async Task AttachImage()
    {
        var path = await Helpers.FilePickerHelper.PickImageFileAsync();
        if (path != null)
        {
            AttachedImageBase64 = await Helpers.ImageHelper.FileToBase64Async(path);
            AttachedImageName = Path.GetFileName(path);
        }
    }

    [RelayCommand]
    public void ClearAttachedImage()
    {
        AttachedImageBase64 = null;
        AttachedImageName = null;
    }

    [RelayCommand]
    public async Task NewConversation()
    {
        if (Messages.Count > 0)
        {
            await PersistAsync();
        }
        Messages.Clear();
        CurrentConversation = null;
    }

    [RelayCommand]
    public async Task LoadConversation(Guid id)
    {
        var conv = await _store.GetConversationAsync(id);
        if (conv != null)
        {
            CurrentConversation = conv;
            Messages = new ObservableCollection<Message>(conv.Messages);
        }
    }

    private async Task PersistAsync()
    {
        if (Messages.Count == 0) return;

        CurrentConversation ??= new Conversation
        {
            Category = "chat",
            Title = Messages.FirstOrDefault(m => m.Role == "user")?.Prompt?[..Math.Min(50, (Messages.FirstOrDefault(m => m.Role == "user")?.Prompt?.Length ?? 0))] ?? "Chat",
            CreatedAt = DateTime.UtcNow,
        };

        CurrentConversation.Messages = Messages.ToList();
        CurrentConversation.UpdatedAt = DateTime.UtcNow;
        await _store.SaveConversationAsync(CurrentConversation);
    }
}
