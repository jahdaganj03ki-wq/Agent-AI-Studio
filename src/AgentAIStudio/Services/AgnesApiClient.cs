using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polly;
using Polly.Extensions.Http;
using AgentAIStudio.Helpers;

namespace AgentAIStudio.Services;

public class AgnesApiClient
{
    private readonly HttpClient _httpClient;
    private string? _apiKey;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public AgnesApiClient()
    {
        var handler = new LoggingHandler(new HttpClientHandler());
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(ApiConstants.DefaultTimeoutSeconds)
        };
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public bool HasApiKey => !string.IsNullOrEmpty(_apiKey);

    public async Task<T> PostAsync<T>(string endpoint, object body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        LogService.Instance.LogDebug("ApiClient", $"POST {endpoint}", new { bodySize = json.Length });
        var response = await _httpClient.PostAsync($"{ApiConstants.BaseUrl}{endpoint}", content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, JsonOptions)!;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        LogService.Instance.LogDebug("ApiClient", $"GET {url}");
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
    }

    public async IAsyncEnumerable<string> PostStreamingAsync(string endpoint, object body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        LogService.Instance.LogDebug("ApiClient", $"POST streaming {endpoint}", new { bodySize = json.Length });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiConstants.BaseUrl}{endpoint}")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        LogService.Instance.LogDebug("ApiClient", $"Streaming response started", new { statusCode = (int)response.StatusCode });

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            try
            {
                var chunk = JsonSerializer.Deserialize<ChatChunkResponse>(data, JsonOptions);
                var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                if (!string.IsNullOrEmpty(delta))
                    yield return delta;
            }
            catch { }
        }
    }

    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
