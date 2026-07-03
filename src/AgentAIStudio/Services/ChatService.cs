using System.Text.Json;
using AgentAIStudio.Helpers;
using AgentAIStudio.Models;

namespace AgentAIStudio.Services;

public class ChatService
{
    private readonly AgnesApiClient _client;

    public ChatService(AgnesApiClient client)
    {
        _client = client;
    }

    public async Task<ChatCompletionResponse> CreateCompletionAsync(
        string userMessage, GenerationParameters parameters, string? imageBase64 = null)
    {
        LogService.Instance.LogDebug("ChatService", "CreateCompletionAsync", new
        {
            messageLength = userMessage.Length,
            model = ApiConstants.ChatModel,
            streaming = false,
            hasImage = imageBase64 != null
        });

        var messages = BuildMessages(userMessage, imageBase64);

        var body = new Dictionary<string, object>
        {
            ["model"] = ApiConstants.ChatModel,
            ["messages"] = messages,
            ["temperature"] = parameters.Temperature,
            ["top_p"] = parameters.TopP,
            ["max_tokens"] = parameters.MaxTokens,
            ["stream"] = false
        };

        if (parameters.EnableThinking)
        {
            body["chat_template_kwargs"] = new Dictionary<string, object>
            {
                ["enable_thinking"] = true
            };
        }

        var response = await _client.PostAsync<ChatCompletionResponse>(
            ApiConstants.ChatEndpoint, body);

        LogService.Instance.LogInfo("ChatService", "Chat completion completed", new
        {
            totalTokens = response.Usage?.TotalTokens,
            finishReason = response.Choices?.FirstOrDefault()?.FinishReason
        });

        return response;
    }

    public IAsyncEnumerable<string> CreateStreamingAsync(
        string userMessage, GenerationParameters parameters, string? imageBase64 = null)
    {
        LogService.Instance.LogDebug("ChatService", "CreateStreamingAsync", new
        {
            messageLength = userMessage.Length,
            model = ApiConstants.ChatModel,
            streaming = true,
            hasImage = imageBase64 != null
        });

        var messages = BuildMessages(userMessage, imageBase64);

        var body = new Dictionary<string, object>
        {
            ["model"] = ApiConstants.ChatModel,
            ["messages"] = messages,
            ["temperature"] = parameters.Temperature,
            ["top_p"] = parameters.TopP,
            ["max_tokens"] = parameters.MaxTokens,
            ["stream"] = true
        };

        if (parameters.EnableThinking)
        {
            body["chat_template_kwargs"] = new Dictionary<string, object>
            {
                ["enable_thinking"] = true
            };
        }

        return _client.PostStreamingAsync(ApiConstants.ChatEndpoint, body);
    }

    private static List<object> BuildMessages(string userMessage, string? imageBase64)
    {
        if (string.IsNullOrEmpty(imageBase64))
        {
            return
            [
                new
                {
                    role = "system",
                    content = "You are a helpful AI assistant powered by Agnes AI. You can help with text generation, coding, reasoning, and creative tasks."
                },
                new
                {
                    role = "user",
                    content = userMessage
                }
            ];
        }

        return
        [
            new
            {
                role = "system",
                content = "You are a helpful AI assistant powered by Agnes AI. You can understand images and help with visual analysis."
            },
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = userMessage },
                    new
                    {
                        type = "image_url",
                        image_url = new { url = $"data:image/png;base64,{imageBase64}" }
                    }
                }
            }
        ];
    }
}
