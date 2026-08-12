using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Atlas.Application.Abstractions.Ai;
using Microsoft.Extensions.Options;

namespace Atlas.Api.Ai;

public sealed class OpenAiChatModelClient : IChatModelClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatModelClient> _logger;

    public OpenAiChatModelClient(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiChatModelClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(AiModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            yield return "OpenAI is not configured. Set OpenAI__ApiKey (environment), or run: dotnet user-secrets set \"OpenAI:ApiKey\" \"<key>\" in the Atlas.Api project, or add OpenAI__ApiKey=... to Compose .env.";
            yield break;
        }

        var payload = new
        {
            model = _options.Model,
            temperature = 0.2,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
        };

        using HttpClient client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync("chat/completions", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI request failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException("OpenAI request failed.");
        }

        var completion = ExtractCompletionText(body);
        foreach (var chunk in Chunk(completion, 160))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk;
        }
    }

    private static string ExtractCompletionText(string json)
    {
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        JsonElement choices = root.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        JsonElement message = choices[0].GetProperty("message");
        if (message.TryGetProperty("content", out JsonElement content) && content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static IEnumerable<string> Chunk(string text, int size)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        for (var i = 0; i < text.Length; i += size)
        {
            var take = Math.Min(size, text.Length - i);
            yield return text.Substring(i, take);
        }
    }
}
