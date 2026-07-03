using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AgentAIStudio.Models;
using AgentAIStudio.Services;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.ViewModels;

public partial class VideoViewModel : ObservableObject
{
    private readonly VideoService _videoService;

    [ObservableProperty]
    private ObservableCollection<VideoTaskItem> _tasks = [];

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private bool _isQueueOpen = true;

    public GenerationParameters Parameters { get; set; } = new();

    public VideoViewModel(VideoService videoService)
    {
        _videoService = videoService;
        _videoService.TaskStatusChanged += OnTaskStatusChanged;
    }

    private void OnTaskStatusChanged(VideoTaskItem item)
    {
        // Refresh on UI thread
        App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            var existing = Tasks.FirstOrDefault(t => t.VideoId == item.VideoId);
            if (existing != null)
            {
                var idx = Tasks.IndexOf(existing);
                Tasks.RemoveAt(idx);
                Tasks.Insert(idx, item);
            }
            else
            {
                Tasks.Insert(0, item);
            }
        });
    }

    [RelayCommand]
    public async Task CreateVideo()
    {
        if (string.IsNullOrWhiteSpace(Prompt)) return;
        IsCreating = true;

        try
        {
            await _videoService.EnqueueTaskAsync(Prompt, Parameters);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Video task creation failed: {ex.Message}");
        }

        IsCreating = false;
        Prompt = string.Empty;
    }

    [RelayCommand]
    public void ToggleQueue()
    {
        IsQueueOpen = !IsQueueOpen;
    }
}
