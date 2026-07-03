using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AgentAIStudio.Models;
using AgentAIStudio.Services;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.ViewModels;

public partial class ImagesViewModel : ObservableObject
{
    private readonly ImageService _imageService;

    [ObservableProperty]
    private ObservableCollection<ImageResultItem> _results = [];

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _selectedModel = ApiConstants.ImageModel21;

    [ObservableProperty]
    private string _selectedSize = "1024x768";

    public ImagesViewModel(ImageService imageService)
    {
        _imageService = imageService;
    }

    [RelayCommand]
    public async Task Generate()
    {
        if (string.IsNullOrWhiteSpace(Prompt)) return;

        LogService.Instance.LogInfo("ViewModel", "Image generation requested", new
        {
            promptLength = Prompt.Length,
            model = SelectedModel,
            size = SelectedSize
        });

        IsGenerating = true;

        try
        {
            var result = await _imageService.GenerateAsync(Prompt, SelectedModel, SelectedSize);
            if (result.Url != null)
            {
                Results.Insert(0, new ImageResultItem
                {
                    Url = result.Url,
                    Prompt = Prompt,
                    Model = SelectedModel,
                    CreatedAt = DateTime.UtcNow
                });
                LogService.Instance.LogInfo("ViewModel", "Image generation succeeded", new
                {
                    hasUrl = true
                });
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("ViewModel", "Image generation failed", ex, new
            {
                model = SelectedModel,
                size = SelectedSize
            });
        }

        IsGenerating = false;
        Prompt = string.Empty;
    }

    [RelayCommand]
    public async Task Regenerate(string url)
    {
        if (string.IsNullOrWhiteSpace(Prompt) && !string.IsNullOrEmpty(url))
        {
            var item = Results.FirstOrDefault(r => r.Url == url);
            if (item != null)
            {
                Prompt = item.Prompt;
                await Generate();
            }
        }
    }
}

public class ImageResultItem
{
    public string Url { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
