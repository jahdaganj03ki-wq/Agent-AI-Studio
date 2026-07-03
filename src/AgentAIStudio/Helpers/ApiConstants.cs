namespace AgentAIStudio.Helpers;

public static class ApiConstants
{
    public const string BaseUrl = "https://apihub.agnes-ai.com/v1";
    public const string ImageBaseUrl = "https://apihub.agnes-ai.com";
    public const string ChatEndpoint = "/v1/chat/completions";
    public const string ImageGenerationsEndpoint = "/v1/images/generations";
    public const string VideoCreateEndpoint = "/v1/videos";
    public const string VideoResultEndpoint = "/agnesapi";

    public const string ChatModel = "agnes-2.0-flash";
    public const string ImageModel20 = "agnes-image-2.0-flash";
    public const string ImageModel21 = "agnes-image-2.1-flash";
    public const string VideoModel = "agnes-video-v2.0";

    public const int DefaultTimeoutSeconds = 120;
    public const int VideoPollIntervalMs = 5000;
    public const int MaxVideoPollAttempts = 60;

    public static readonly string[] ImageSizes = ["1024x768", "1024x1024", "768x1024", "1920x1080", "1080x1920", "1536x1024"];

    public static readonly string[] VideoAspectRatios = ["16:9", "9:16", "1:1", "4:3", "3:4"];
}
