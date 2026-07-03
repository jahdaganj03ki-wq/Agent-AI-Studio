using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AgentAIStudio.Models;
using AgentAIStudio.Services;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.ViewModels;

public partial class ImageStudioViewModel : ObservableObject
{
    private readonly ImageService _imageService;
    private readonly ConversationStore _store;

    [ObservableProperty]
    private ObservableCollection<Message> _messages = [];

    [ObservableProperty]
    private string _inputPrompt = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _selectedModel = ApiConstants.ImageModel21;

    [ObservableProperty]
    private string _selectedSize = "1024x768";

    [ObservableProperty]
    private string? _editingImageUrl;

    [ObservableProperty]
    private string? _editingImageBase64;

    [ObservableProperty]
    private string? _editingImageName;

    [ObservableProperty]
    private string[] _uploadedImageBase64s = [];

    [ObservableProperty]
    private bool _isMultiImageMode;

    [ObservableProperty]
    private Conversation? _currentConversation;

    public GenerationParameters Parameters { get; set; } = new();

    public ImageStudioViewModel(ImageService imageService, ConversationStore store)
    {
        _imageService = imageService;
        _store = store;
        LogService.Instance.LogInfo("ViewModel", "ImageStudioViewModel initialized");
    }

    [RelayCommand]
    public async Task GenerateOrEdit()
    {
        if (string.IsNullOrWhiteSpace(InputPrompt)) return;

        var isEditing = !string.IsNullOrEmpty(EditingImageBase64) || !string.IsNullOrEmpty(EditingImageUrl);
        LogService.Instance.LogInfo("ViewModel", "ImageStudio generate/edit", new
        {
            promptLength = InputPrompt.Length,
            isEditing,
            isMultiImage = IsMultiImageMode,
            model = SelectedModel,
            size = SelectedSize
        });

        IsGenerating = true;

        try
        {
            ImageResult result;

            if (isEditing)
            {
                var imageData = EditingImageBase64 ?? EditingImageUrl!;
                result = await _imageService.EditAsync(
                    InputPrompt, imageData, SelectedModel, SelectedSize,
                    isBase64: !string.IsNullOrEmpty(EditingImageBase64));
            }
            else if (IsMultiImageMode && UploadedImageBase64s.Length > 0)
            {
                var dataUris = UploadedImageBase64s.Select(b => $"data:image/png;base64,{b}").ToList();
                result = await _imageService.MultiImageComposeAsync(
                    InputPrompt, dataUris, SelectedModel, SelectedSize);
            }
            else
            {
                result = await _imageService.GenerateAsync(
                    InputPrompt, SelectedModel, SelectedSize);
            }

            if (result.Url != null)
            {
                // Add user message
                var userMsg = new Message
                {
                    Role = "user",
                    Prompt = InputPrompt,
                    MediaType = MediaType.None,
                    Status = MessageStatus.Complete,
                    SortOrder = Messages.Count,
                    Timestamp = DateTime.UtcNow,
                    ParentId = GetEditingParentId(),
                    BranchIndex = GetBranchIndex()
                };
                Messages.Add(userMsg);

                // Add result message
                var resultMsg = new Message
                {
                    Role = "assistant",
                    Prompt = InputPrompt,
                    GeneratedUrl = result.Url,
                    MediaType = MediaType.Image,
                    Status = MessageStatus.Complete,
                    SortOrder = Messages.Count,
                    Timestamp = DateTime.UtcNow,
                    ParentId = userMsg.Id
                };
                Messages.Add(resultMsg);
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("ViewModel", "ImageStudio generation failed", ex, new
            {
                prompt = InputPrompt,
                model = SelectedModel,
                size = SelectedSize,
                isEditing,
                isMultiImage = IsMultiImageMode
            });

            var errorMsg = new Message
            {
                Role = "assistant",
                Prompt = $"Error: {ex.Message}",
                MediaType = MediaType.None,
                Status = MessageStatus.Failed,
                SortOrder = Messages.Count,
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(errorMsg);
        }

        IsGenerating = false;
        InputPrompt = string.Empty;
        EditingImageBase64 = null;
        EditingImageUrl = null;
        EditingImageName = null;
        UploadedImageBase64s = [];
        IsMultiImageMode = false;

        await PersistAsync();
    }

    [RelayCommand]
    public void EditFurther(string imageUrl)
    {
        EditingImageUrl = imageUrl;
        EditingImageBase64 = null;
        EditingImageName = null;
        IsMultiImageMode = false;
    }

    [RelayCommand]
    public async Task Regenerate(string imageUrl)
    {
        // Find the message with this image and re-run its prompt
        var msg = Messages.FirstOrDefault(m => m.GeneratedUrl == imageUrl);
        if (msg != null)
        {
            EditingImageUrl = msg.GeneratedUrl;
            InputPrompt = msg.Prompt;
            await GenerateOrEdit();
        }
    }

    [RelayCommand]
    public async Task UploadSourceImage()
    {
        var path = await FilePickerHelper.PickImageFileAsync();
        if (path != null)
        {
            EditingImageBase64 = await ImageHelper.FileToBase64Async(path);
            EditingImageUrl = null;
            EditingImageName = Path.GetFileName(path);
            IsMultiImageMode = false;
        }
    }

    [RelayCommand]
    public async Task UploadMultipleImages()
    {
        var paths = await FilePickerHelper.PickMultipleImageFilesAsync();
        if (paths.Count > 0)
        {
            var base64s = new List<string>();
            foreach (var path in paths)
            {
                base64s.Add(await ImageHelper.FileToBase64Async(path));
            }
            UploadedImageBase64s = [.. base64s];
            IsMultiImageMode = true;
            EditingImageBase64 = null;
            EditingImageUrl = null;
            EditingImageName = $"{paths.Count} images";
        }
    }

    [RelayCommand]
    public async Task NewConversation()
    {
        if (Messages.Count > 0) await PersistAsync();
        Messages.Clear();
        CurrentConversation = null;
        EditingImageBase64 = null;
        EditingImageUrl = null;
        EditingImageName = null;
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

    public async Task DownloadImage(string url)
    {
        try
        {
            var ext = ImageHelper.GetImageExtension(url);
            var path = await FilePickerHelper.SaveFileAsync($"studio_{DateTime.Now:yyyyMMdd_HHmmss}{ext}", ext);
            if (path != null)
            {
                var bytes = await ImageHelper.DownloadImageAsync(url);
                await File.WriteAllBytesAsync(path, bytes);
            }
        }
        catch { }
    }

    private Guid? GetEditingParentId()
    {
        if (!string.IsNullOrEmpty(EditingImageUrl))
        {
            var parent = Messages.FirstOrDefault(m => m.GeneratedUrl == EditingImageUrl);
            return parent?.Id;
        }
        return null;
    }

    private int GetBranchIndex()
    {
        var parentId = GetEditingParentId();
        if (parentId == null) return 0;
        return Messages.Count(m => m.ParentId == parentId) + 1;
    }

    private async Task PersistAsync()
    {
        if (Messages.Count == 0) return;

        CurrentConversation ??= new Conversation
        {
            Category = "imagestudio",
            Title = Messages.FirstOrDefault(m => m.Role == "user")?.Prompt?[..Math.Min(50, (Messages.FirstOrDefault(m => m.Role == "user")?.Prompt?.Length ?? 0))] ?? "Image Studio",
            CreatedAt = DateTime.UtcNow,
        };

        CurrentConversation.Messages = Messages.ToList();
        CurrentConversation.UpdatedAt = DateTime.UtcNow;
        await _store.SaveConversationAsync(CurrentConversation);
    }
}
