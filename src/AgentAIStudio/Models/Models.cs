namespace AgentAIStudio.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Message> Messages { get; set; } = [];
}

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid? ParentId { get; set; }
    public int BranchIndex { get; set; }
    public int SortOrder { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? GeneratedUrl { get; set; }
    public string? Base64Thumb { get; set; }
    public string? VideoId { get; set; }
    public MediaType MediaType { get; set; } = MediaType.None;
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public string? ParametersJson { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ImageResult
{
    public string? Url { get; set; }
    public string? B64Json { get; set; }
    public string? RevisedPrompt { get; set; }
}

public class VideoTaskInfo
{
    public string Id { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public int Progress { get; set; }
    public string? Seconds { get; set; }
    public string? Size { get; set; }
    public long CreatedAt { get; set; }
}

public class VideoResult
{
    public string Status { get; set; } = "queued";
    public int Progress { get; set; }
    public string? VideoUrl { get; set; }
    public string? Error { get; set; }
    public string? Seconds { get; set; }
    public string? Size { get; set; }
    public string? VideoId { get; set; }
}

public class VideoTaskItem
{
    public string VideoId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public VideoTaskStatus Status { get; set; } = VideoTaskStatus.Queued;
    public int Progress { get; set; }
    public string? ResultUrl { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GenerationParameters
{
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 1.0;
    public int MaxTokens { get; set; } = 4096;
    public bool EnableThinking { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Size { get; set; } = "1024x768";
    public int? Seed { get; set; }
    public int Width { get; set; } = 1152;
    public int Height { get; set; } = 768;
    public int NumFrames { get; set; } = 121;
    public double FrameRate { get; set; } = 24;
    public string? NegativePrompt { get; set; }
    public string VideoMode { get; set; } = string.Empty;

    public Dictionary<string, object> ToChatDictionary()
    {
        var d = new Dictionary<string, object>
        {
            ["temperature"] = Temperature,
            ["top_p"] = TopP,
            ["max_tokens"] = MaxTokens
        };
        if (EnableThinking)
            d["chat_template_kwargs"] = new Dictionary<string, object> { ["enable_thinking"] = true };
        return d;
    }

    public Dictionary<string, object> ToImageDictionary()
    {
        var d = new Dictionary<string, object>
        {
            ["seed"] = Seed ?? 0
        };
        return d;
    }

    public Dictionary<string, object> ToVideoDictionary()
    {
        var d = new Dictionary<string, object>
        {
            ["width"] = Width,
            ["height"] = Height,
            ["num_frames"] = NumFrames,
            ["frame_rate"] = FrameRate
        };
        if (Seed.HasValue) d["seed"] = Seed.Value;
        if (!string.IsNullOrEmpty(NegativePrompt)) d["negative_prompt"] = NegativePrompt;
        if (!string.IsNullOrEmpty(VideoMode)) d["mode"] = VideoMode;
        return d;
    }
}

public class ChatChoice
{
    public int Index { get; set; }
    public ChatChoiceMessage? Delta { get; set; }
    public ChatChoiceMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

public class ChatChoiceMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class ChatCompletionResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? Model { get; set; }
    public List<ChatChoice> Choices { get; set; } = [];
    public ChatUsage? Usage { get; set; }
}

public class ChatChunkResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? Model { get; set; }
    public List<ChatChoice> Choices { get; set; } = [];
}

public class ChatUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class ImageGenerationResponse
{
    public long Created { get; set; }
    public List<ImageData> Data { get; set; } = [];
}

public class ImageData
{
    public string? Url { get; set; }
    public string? B64Json { get; set; }
    public string? RevisedPrompt { get; set; }
}

public class VideoCreateResponse
{
    public string? Id { get; set; }
    public string? TaskId { get; set; }
    public string? VideoId { get; set; }
    public string? Object { get; set; }
    public string? Model { get; set; }
    public string? Status { get; set; }
    public int Progress { get; set; }
    public long CreatedAt { get; set; }
    public string? Seconds { get; set; }
    public string? Size { get; set; }
}

public class VideoPollResponse
{
    public string? Id { get; set; }
    public string? VideoId { get; set; }
    public string? Model { get; set; }
    public string? Object { get; set; }
    public string? Status { get; set; }
    public int Progress { get; set; }
    public string? Seconds { get; set; }
    public string? Size { get; set; }
    public string? RemixedFromVideoId { get; set; }
    public string? Error { get; set; }
}
