using System.Text.Json;
using Microsoft.JSInterop;

namespace Atlas.Ui.Services;

/// <summary>
/// JS interop wrapper around browser <c>EventSource</c> for AI session SSE.
/// </summary>
public sealed class AiSessionEventsClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IJSRuntime _js;
    private readonly string _apiBaseUrl;
    private DotNetObjectReference<AiSessionEventsClient>? _selfRef;
    private string? _activeStreamId;
    private Action<AiSessionEventPayload>? _onEvent;
    private Action? _onError;

    public AiSessionEventsClient(IJSRuntime js, IConfiguration config)
    {
        _js = js;
        _apiBaseUrl = (config["ApiBaseUrl"] ?? "http://localhost:5012").TrimEnd('/');
    }

    public async Task ConnectAsync(Guid sessionId, Action<AiSessionEventPayload> onEvent, Action? onError = null)
    {
        await CloseAsync();

        _onEvent = onEvent;
        _onError = onError;
        _selfRef = DotNetObjectReference.Create(this);
        _activeStreamId = Guid.NewGuid().ToString("N");

        var url = $"{_apiBaseUrl}/ai/sessions/{sessionId:D}/events";
        await _js.InvokeVoidAsync("atlasAiEvents.open", _activeStreamId, url, _selfRef);
    }

    public async Task CloseAsync()
    {
        if (_activeStreamId is not null)
        {
            try
            {
                await _js.InvokeVoidAsync("atlasAiEvents.close", _activeStreamId);
            }
            catch
            {
                // Ignore dispose races during navigation.
            }

            _activeStreamId = null;
        }

        _onEvent = null;
        _onError = null;
        _selfRef?.Dispose();
        _selfRef = null;
    }

    [JSInvokable]
    public void OnSessionEventJson(string json)
    {
        if (_onEvent is null || string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            AiSessionEventPayload? evt = JsonSerializer.Deserialize<AiSessionEventPayload>(json, JsonOptions);
            if (evt is not null)
            {
                _onEvent(evt);
            }
        }
        catch
        {
            // Ignore malformed payloads (match React).
        }
    }

    [JSInvokable]
    public void OnSessionEventError() => _onError?.Invoke();

    public async ValueTask DisposeAsync() => await CloseAsync();
}

public sealed class AiSessionEventPayload
{
    public Guid EventId { get; set; }
    public Guid SessionId { get; set; }
    public int Sequence { get; set; }
    public string Type { get; set; } = "";
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? Delta { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public bool IsTerminal { get; set; }
}
