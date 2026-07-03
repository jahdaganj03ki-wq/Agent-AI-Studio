using System.Text.Json;
using AgentAIStudio.Helpers;
using AgentAIStudio.Models;

namespace AgentAIStudio.Services;

public class ImageService
{
    private readonly AgnesApiClient _client;

    public ImageService(AgnesApiClient client)
    {
        _client = client;
    }

    public async Task<ImageResult> GenerateAsync(
        string prompt, string model, string size, GenerationParameters? parameters = null)
    {
        LogService.Instance.LogDebug("ImageService", "GenerateAsync", new
        {
            promptLength = prompt.Length,
            model,
            size
        });

        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["size"] = size
        };

        body["extra_body"] = new Dictionary<string, object>
        {
            ["response_format"] = "url"
        };

        var response = await _client.PostAsync<ImageGenerationResponse>(
            ApiConstants.ImageGenerationsEndpoint, body);

        var data = response.Data.FirstOrDefault();
        var hasResult = data?.Url != null || data?.B64Json != null;

        LogService.Instance.LogInfo("ImageService", "Image generated", new
        {
            hasUrl = data?.Url != null,
            hasBase64 = data?.B64Json != null,
            hasRevisedPrompt = data?.RevisedPrompt != null
        });

        return new ImageResult
        {
            Url = data?.Url,
            B64Json = data?.B64Json,
            RevisedPrompt = data?.RevisedPrompt
        };
    }

    public async Task<ImageResult> EditAsync(
        string prompt, string imageUrlOrBase64, string model, string size,
        bool isBase64 = false, GenerationParameters? parameters = null)
    {
        LogService.Instance.LogDebug("ImageService", "EditAsync", new
        {
            promptLength = prompt.Length,
            model,
            size,
            isBase64Input = isBase64
        });

        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["size"] = size
        };

        var extraBody = new Dictionary<string, object>
        {
            ["response_format"] = "url"
        };

        extraBody["image"] = new List<string>
        {
            isBase64 ? $"data:image/png;base64,{imageUrlOrBase64}" : imageUrlOrBase64
        };

        body["extra_body"] = extraBody;

        var response = await _client.PostAsync<ImageGenerationResponse>(
            ApiConstants.ImageGenerationsEndpoint, body);

        var data = response.Data.FirstOrDefault();

        LogService.Instance.LogInfo("ImageService", "Image edit completed", new
        {
            hasResult = data?.Url != null
        });

        return new ImageResult
        {
            Url = data?.Url,
            B64Json = data?.B64Json,
            RevisedPrompt = data?.RevisedPrompt
        };
    }

    public async Task<ImageResult> MultiImageComposeAsync(
        string prompt, List<string> imageUrls, string model, string size,
        GenerationParameters? parameters = null)
    {
        LogService.Instance.LogDebug("ImageService", "MultiImageComposeAsync", new
        {
            promptLength = prompt.Length,
            model,
            size,
            imageCount = imageUrls.Count
        });

        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["size"] = size
        };

        body["extra_body"] = new Dictionary<string, object>
        {
            ["response_format"] = "url",
            ["image"] = imageUrls
        };

        var response = await _client.PostAsync<ImageGenerationResponse>(
            ApiConstants.ImageGenerationsEndpoint, body);

        var data = response.Data.FirstOrDefault();

        LogService.Instance.LogInfo("ImageService", "Multi-image composition completed", new
        {
            hasResult = data?.Url != null
        });

        return new ImageResult
        {
            Url = data?.Url,
            B64Json = data?.B64Json,
            RevisedPrompt = data?.RevisedPrompt
        };
    }
}
