using System.Diagnostics;
using System.Net;
using System.Net.Http;
using AgentAIStudio.Models;

namespace AgentAIStudio.Services;

public class LoggingHandler : DelegatingHandler
{
    private const string LogCategory = "ApiClient";

    public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var requestBodySize = request.Content?.Headers?.ContentLength ?? 0;

        LogService.Instance.LogDebug(LogCategory, "Request started", new
        {
            method = request.Method.ToString(),
            url = request.RequestUri?.PathAndQuery,
            requestSize = requestBodySize
        });

        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            var responseBodySize = response.Content?.Headers?.ContentLength ?? 0;
            var level = response.IsSuccessStatusCode ? LogLevel.Info : LogLevel.Warning;

            LogService.Instance.Log(level, LogCategory, "Response received", new
            {
                method = request.Method.ToString(),
                url = request.RequestUri?.PathAndQuery,
                statusCode = (int)response.StatusCode,
                durationMs = sw.ElapsedMilliseconds,
                requestSize = requestBodySize,
                responseSize = responseBodySize
            });

            return response;
        }
        catch (TaskCanceledException tce)
        {
            sw.Stop();
            LogService.Instance.LogError(LogCategory,
                $"Request timed out after {sw.ElapsedMilliseconds}ms", tce, new
                {
                    method = request.Method.ToString(),
                    url = request.RequestUri?.PathAndQuery,
                    durationMs = sw.ElapsedMilliseconds
                });
            throw;
        }
        catch (HttpRequestException hre)
        {
            sw.Stop();
            LogService.Instance.LogError(LogCategory, "HTTP request failed", hre, new
            {
                method = request.Method.ToString(),
                url = request.RequestUri?.PathAndQuery,
                durationMs = sw.ElapsedMilliseconds,
                statusCode = hre.StatusCode != null ? (int)hre.StatusCode : 0
            });
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogService.Instance.LogError(LogCategory, "Unexpected request error", ex, new
            {
                method = request.Method.ToString(),
                url = request.RequestUri?.PathAndQuery,
                durationMs = sw.ElapsedMilliseconds
            });
            throw;
        }
    }
}
