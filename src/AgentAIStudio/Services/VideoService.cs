using System.Text.Json;
using System.Threading.Channels;
using AgentAIStudio.Helpers;
using AgentAIStudio.Models;

namespace AgentAIStudio.Services;

public class VideoService
{
    private readonly AgnesApiClient _client;
    private readonly Channel<VideoTaskItem> _taskChannel;
    private readonly Dictionary<string, VideoTaskItem> _tasks = [];

    public event Action<VideoTaskItem>? TaskStatusChanged;

    public VideoService(AgnesApiClient client)
    {
        _client = client;
        _taskChannel = Channel.CreateUnbounded<VideoTaskItem>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
        StartPollingLoop();
    }

    public async Task<VideoTaskInfo> CreateTaskAsync(
        string prompt, GenerationParameters parameters, List<string>? imageUrls = null)
    {
        LogService.Instance.LogDebug("VideoService", "CreateTaskAsync", new
        {
            promptLength = prompt.Length,
            hasImage = imageUrls?.Count > 0,
            imageCount = imageUrls?.Count ?? 0,
            width = parameters.Width,
            height = parameters.Height,
            numFrames = parameters.NumFrames,
            frameRate = parameters.FrameRate
        });

        var body = new Dictionary<string, object>
        {
            ["model"] = ApiConstants.VideoModel,
            ["prompt"] = prompt,
            ["height"] = parameters.Height,
            ["width"] = parameters.Width,
            ["num_frames"] = parameters.NumFrames,
            ["frame_rate"] = parameters.FrameRate
        };

        if (!string.IsNullOrEmpty(parameters.NegativePrompt))
            body["negative_prompt"] = parameters.NegativePrompt;

        if (parameters.Seed.HasValue)
            body["seed"] = parameters.Seed.Value;

        if (imageUrls is { Count: 1 })
        {
            body["image"] = imageUrls[0];
        }
        else if (imageUrls is { Count: > 1 })
        {
            body["extra_body"] = new Dictionary<string, object>
            {
                ["image"] = imageUrls,
                ["mode"] = string.IsNullOrEmpty(parameters.VideoMode) ? null! : parameters.VideoMode
            };
        }

        var response = await _client.PostAsync<VideoCreateResponse>(
            ApiConstants.VideoCreateEndpoint, body);

        LogService.Instance.LogInfo("VideoService", "Video task created", new
        {
            videoId = response.VideoId ?? response.Id,
            status = response.Status,
            seconds = response.Seconds
        });

        return new VideoTaskInfo
        {
            Id = response.Id ?? string.Empty,
            TaskId = response.TaskId ?? string.Empty,
            VideoId = response.VideoId ?? string.Empty,
            Status = response.Status ?? "queued",
            Progress = response.Progress,
            Seconds = response.Seconds,
            Size = response.Size,
            CreatedAt = response.CreatedAt
        };
    }

    public async Task EnqueueTaskAsync(string prompt, GenerationParameters parameters, List<string>? imageUrls = null)
    {
        var info = await CreateTaskAsync(prompt, parameters, imageUrls);
        var videoId = info.VideoId;

        LogService.Instance.LogInfo("VideoService", "Enqueuing video task", new
        {
            videoId,
            status = info.Status,
            seconds = info.Seconds
        });

        var item = new VideoTaskItem
        {
            VideoId = videoId,
            Prompt = prompt,
            Status = VideoTaskStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        _tasks[videoId] = item;
        TaskStatusChanged?.Invoke(item);
        await _taskChannel.Writer.WriteAsync(item);
    }

    public async Task<VideoResult> PollResultAsync(string videoId)
    {
        var url = $"{ApiConstants.ImageBaseUrl}{ApiConstants.VideoResultEndpoint}?video_id={videoId}";
        LogService.Instance.LogDebug("VideoService", "Polling video result", new { videoId });
        var response = await _client.GetAsync<VideoPollResponse>(url);
        return new VideoResult
        {
            Status = response.Status ?? "queued",
            Progress = response.Progress,
            VideoUrl = response.RemixedFromVideoId,
            Error = response.Error,
            Seconds = response.Seconds,
            Size = response.Size,
            VideoId = response.VideoId
        };
    }

    public VideoTaskItem? GetTask(string videoId)
    {
        return _tasks.TryGetValue(videoId, out var task) ? task : null;
    }

    public List<VideoTaskItem> GetAllTasks()
    {
        return _tasks.Values.OrderByDescending(t => t.CreatedAt).ToList();
    }

    private void StartPollingLoop()
    {
        _ = Task.Run(async () =>
        {
            await foreach (var task in _taskChannel.Reader.ReadAllAsync())
            {
                await PollTaskAsync(task);
            }
        });
    }

    private async Task PollTaskAsync(VideoTaskItem item)
    {
        LogService.Instance.LogDebug("VideoService", "Polling task started", new { videoId = item.VideoId });

        for (int i = 0; i < ApiConstants.MaxVideoPollAttempts; i++)
        {
            await Task.Delay(ApiConstants.VideoPollIntervalMs);

            try
            {
                var result = await PollResultAsync(item.VideoId);
                item.Progress = result.Progress;

                LogService.Instance.LogDebug("VideoService", $"Poll attempt {i + 1}", new
                {
                    videoId = item.VideoId,
                    status = result.Status,
                    progress = result.Progress,
                    attempt = i + 1
                });

                switch (result.Status.ToLower())
                {
                    case "queued":
                        item.Status = VideoTaskStatus.Queued;
                        break;
                    case "in_progress":
                        item.Status = VideoTaskStatus.InProgress;
                        break;
                    case "completed":
                    case "succeeded":
                    case "success":
                    case "done":
                        item.Status = VideoTaskStatus.Completed;
                        item.Progress = 100;
                        item.ResultUrl = result.VideoUrl;
                        LogService.Instance.LogInfo("VideoService", "Video task completed", new
                        {
                            videoId = item.VideoId,
                            url = result.VideoUrl,
                            seconds = result.Seconds,
                            size = result.Size
                        });
                        TaskStatusChanged?.Invoke(item);
                        return;
                    case "failed":
                    case "error":
                    case "cancelled":
                        item.Status = VideoTaskStatus.Failed;
                        item.Error = result.Error;
                        LogService.Instance.LogError("VideoService", "Video task failed", null, new
                        {
                            videoId = item.VideoId,
                            error = result.Error
                        });
                        TaskStatusChanged?.Invoke(item);
                        return;
                }

                TaskStatusChanged?.Invoke(item);
            }
            catch (Exception ex)
            {
                LogService.Instance.LogWarning("VideoService", $"Poll attempt {i + 1} failed", new
                {
                    videoId = item.VideoId,
                    error = ex.Message,
                    attempt = i + 1
                });
                item.Error = ex.Message;
                TaskStatusChanged?.Invoke(item);
            }
        }

        LogService.Instance.LogError("VideoService", "Video polling timed out", null, new
        {
            videoId = item.VideoId,
            maxAttempts = ApiConstants.MaxVideoPollAttempts
        });

        item.Status = VideoTaskStatus.Failed;
        item.Error = "Polling timed out";
        TaskStatusChanged?.Invoke(item);
    }
}
