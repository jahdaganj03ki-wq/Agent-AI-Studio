namespace AgentAIStudio.Models;

public enum MediaType
{
    None,
    Image,
    Video
}

public enum MessageStatus
{
    Pending,
    Generating,
    Complete,
    Failed
}

public enum VideoTaskStatus
{
    Queued,
    InProgress,
    Completed,
    Failed
}
