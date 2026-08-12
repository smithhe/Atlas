using Atlas.Ui.Api.Generated;

namespace Atlas.Ui.Services;

public sealed record AiAction(string Id, string Label, string? Description = null);

public sealed record AiTranscriptTurn(string Id, string Prompt, string Response, Guid? SessionId = null);

public sealed class AiDraftTarget
{
    public required Action<string> Insert { get; init; }
    public string? Label { get; init; }
}

/// <summary>
/// AI panel state mirroring React <c>AiState.tsx</c>: conversations via NSwag,
/// session streaming via <see cref="AiSessionEventsClient"/> (EventSource).
/// </summary>
public sealed class AiStateService : IAsyncDisposable
{
    private readonly IAtlasApiClient _api;
    private readonly SelectionState _selection;
    private readonly AppCacheService _cache;
    private readonly AiSessionEventsClient _events;

    private readonly List<AiTranscriptTurn> _turns = [];
    private readonly List<AiSessionEventPayload> _eventsBuffer = [];
    private readonly List<AtlasApiDTOsAiAiConversationListItemDto> _conversations = [];
    private readonly List<AiAction> _actions = [];

    private AiDraftTarget? _draftTarget;
    private string? _activeTurnId;
    private Guid? _activeConversationId;
    private Guid? _activeSessionId;
    private bool _userChangedIsOpen;
    private bool _appliedStartupPreference;
    private bool _subscribedCache;

    public AiStateService(
        IAtlasApiClient api,
        SelectionState selection,
        AppCacheService cache,
        AiSessionEventsClient events)
    {
        _api = api;
        _selection = selection;
        _cache = cache;
        _events = events;
    }

    public event Action? Changed;

    public bool IsOpen { get; private set; }
    public string ContextTitle { get; private set; } = "Context: Dashboard";
    public IReadOnlyList<AiAction> Actions => _actions;
    public IReadOnlyList<AtlasApiDTOsAiAiConversationListItemDto> Conversations => _conversations;
    public IReadOnlyList<AiTranscriptTurn> Turns => _turns;
    public string? Notice { get; private set; }
    public string Status { get; private set; } = "Idle";
    public bool IsRunning { get; private set; }
    public bool IsLoadingHistory { get; private set; }
    public Guid? ActiveConversationId => _activeConversationId;
    public Guid? ActiveSessionId => _activeSessionId;
    public string PromptDraft { get; private set; } = "";
    public int? PanelWidthPx { get; private set; }
    public string? DraftTargetLabel => _draftTarget?.Label;
    public bool HasDraftTarget => _draftTarget is not null;

    public bool IsContextSupported => ResolveView(ContextTitle) is not null;

    public string? ContextSupportMessage => IsContextSupported
        ? null
        : "AI context is available for Dashboard, Tasks, Team, Risks, Projects, and Settings.";

    public void EnsureStartupPreference()
    {
        if (_subscribedCache)
        {
            return;
        }

        _subscribedCache = true;
        _cache.Changed += OnCacheChanged;
        TryApplyStartupPreference();
    }

    private void OnCacheChanged() => TryApplyStartupPreference();

    private void TryApplyStartupPreference()
    {
        if (_appliedStartupPreference || _userChangedIsOpen)
        {
            return;
        }

        if (_cache.IsHydrating)
        {
            return;
        }

        if (_cache.Settings is null)
        {
            return;
        }

        IsOpen = _cache.Settings.DefaultAiPanelOpen;
        _appliedStartupPreference = true;
        Notify();
    }

    public void SetIsOpen(bool isOpen)
    {
        _userChangedIsOpen = true;
        if (IsOpen == isOpen)
        {
            return;
        }

        IsOpen = isOpen;
        Notify();
        if (isOpen)
        {
            _ = RefreshConversationsAsync();
        }
    }

    public void SetPanelWidthPx(int? px)
    {
        PanelWidthPx = px;
        Notify();
    }

    public void SetContext(string contextTitle, IEnumerable<AiAction>? actions = null)
    {
        ContextTitle = contextTitle;
        _actions.Clear();
        if (actions is not null)
        {
            _actions.AddRange(actions);
        }

        Notify();
    }

    public void SetPromptDraft(string text)
    {
        PromptDraft = text;
        Notify();
    }

    public void RegisterDraftTarget(AiDraftTarget? target)
    {
        _draftTarget = target;
        Notify();
    }

    public bool InsertDraft(string text)
    {
        var trimmed = text.Trim();
        if (_draftTarget is null || trimmed.Length == 0)
        {
            return false;
        }

        _draftTarget.Insert(trimmed);
        return true;
    }

    public void ClearOutput()
    {
        _turns.Clear();
        _activeTurnId = null;
        _eventsBuffer.Clear();
        Notice = null;
        _activeSessionId = null;
        Notify();
    }

    public void AppendOutput(string text)
    {
        Notice = string.IsNullOrEmpty(Notice) ? text.TrimStart() : Notice + text;
        Notify();
    }

    public async Task StartNewSessionAsync()
    {
        await _events.CloseAsync();
        _turns.Clear();
        _activeTurnId = null;
        _eventsBuffer.Clear();
        Notice = null;
        _activeConversationId = null;
        _activeSessionId = null;
        Status = "Idle";
        IsRunning = false;
        Notify();
    }

    public void LoadConversations() => _ = RefreshConversationsAsync();

    public async Task OpenConversationAsync(Guid conversationId)
    {
        await _events.CloseAsync();
        IsLoadingHistory = true;
        Status = "Loading history...";
        IsRunning = false;
        Notify();

        try
        {
            AtlasApiDTOsAiAiConversationDetailDto conversation = await _api.AtlasApiEndpointsAiGetAiConversationEndpointAsync(conversationId);
            _activeConversationId = conversation.ConversationId ?? conversationId;
            _turns.Clear();

            foreach (AtlasApiDTOsAiAiConversationTurnDto turn in conversation.Turns ?? Array.Empty<AtlasApiDTOsAiAiConversationTurnDto>())
            {
                List<AiSessionEventPayload> events = SortEvents(turn.Events?.Select(MapDto).Where(e => e is not null).Cast<AiSessionEventPayload>() ?? []);
                _turns.Add(new AiTranscriptTurn(
                    NewTurnId(),
                    turn.Prompt ?? "",
                    RenderEvents(events),
                    turn.SessionId));
            }

            AtlasApiDTOsAiAiConversationTurnDto? lastTurn = conversation.Turns?.LastOrDefault();
            if (lastTurn is not null && lastTurn.IsTerminal != true && lastTurn.SessionId is { } sid)
            {
                _activeTurnId = _turns.LastOrDefault()?.Id;
                _activeSessionId = sid;
                _eventsBuffer.Clear();
                _eventsBuffer.AddRange(SortEvents(lastTurn.Events?.Select(MapDto).Where(e => e is not null).Cast<AiSessionEventPayload>() ?? []));
                Status = "Reconnecting to stream...";
                IsRunning = true;
                await ConnectStreamAsync(sid);
            }
            else
            {
                _activeTurnId = null;
                _activeSessionId = lastTurn?.SessionId;
                _eventsBuffer.Clear();
                Status = ToDisplayStatus(lastTurn?.Status ?? "completed");
            }

            Notice = null;
        }
        catch (Exception ex)
        {
            Status = "Failed";
            Notice = ex.Message;
        }
        finally
        {
            IsLoadingHistory = false;
            Notify();
        }
    }

    public void RunAction(string actionId, string? promptOverride = null)
    {
        AiAction? action = _actions.FirstOrDefault(a => a.Id == actionId);
        var prompt = string.IsNullOrWhiteSpace(promptOverride)
            ? $"Please help with this action: {action?.Label ?? actionId}"
            : promptOverride.Trim();
        _ = SendTurnAsync(prompt, actionId);
    }

    public void SendPrompt(string prompt) => _ = SendTurnAsync(prompt);

    private async Task SendTurnAsync(string prompt, string? actionId = null)
    {
        var trimmed = prompt.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        AtlasApplicationAbstractionsAiAiViewScope? view = ResolveView(ContextTitle);
        if (view is null)
        {
            _userChangedIsOpen = true;
            IsOpen = true;
            Status = "Unsupported context";
            Notice = "AI context is available for Dashboard, Tasks, Team, Risks, Projects, and Settings.";
            Notify();
            return;
        }

        var turnId = NewTurnId();
        _userChangedIsOpen = true;
        IsOpen = true;
        IsRunning = true;
        Status = "Starting...";
        _turns.Add(new AiTranscriptTurn(turnId, trimmed, ""));
        _activeTurnId = turnId;
        _eventsBuffer.Clear();
        Notice = null;
        Notify();

        await _events.CloseAsync();

        try
        {
            Guid turnSessionId;
            if (_activeConversationId is { } conversationId)
            {
                AtlasApiDTOsAiContinueAiConversationResponse res = await _api.AtlasApiEndpointsAiContinueAiConversationEndpointAsync(
                    conversationId,
                    new AtlasApiDTOsAiContinueAiConversationRequest { Prompt = trimmed });
                turnSessionId = res.TurnSessionId ?? throw new InvalidOperationException("Missing turn session id");
            }
            else
            {
                AtlasApiDTOsAiCreateAiConversationResponse res = await _api.AtlasApiEndpointsAiCreateAiConversationEndpointAsync(
                    new AtlasApiDTOsAiCreateAiConversationRequest
                    {
                        Prompt = trimmed,
                        View = view,
                        ActionId = actionId,
                        TaskId = _selection.SelectedTaskId,
                        ProjectId = _selection.SelectedProjectId,
                        RiskId = _selection.SelectedRiskId,
                        TeamMemberId = _selection.SelectedTeamMemberId,
                    });
                _activeConversationId = res.ConversationId;
                turnSessionId = res.TurnSessionId ?? throw new InvalidOperationException("Missing turn session id");
            }

            _activeSessionId = turnSessionId;
            ReplaceTurn(turnId, t => t with { SessionId = turnSessionId });
            Status = "Connecting to stream...";
            Notify();
            await RefreshConversationsAsync();
            await ConnectStreamAsync(turnSessionId);
        }
        catch (Exception ex)
        {
            Status = "Failed";
            IsRunning = false;
            _activeTurnId = null;
            _turns.RemoveAll(t => t.Id == turnId);
            Notice = ex.Message;
            Notify();
        }
    }

    private async Task ConnectStreamAsync(Guid sessionId)
    {
        await _events.ConnectAsync(sessionId, OnSessionEvent, OnStreamError);
    }

    private void OnStreamError()
    {
        // Terminal SSE already completed successfully — ignore end-of-stream onerror.
        if (!IsRunning)
        {
            return;
        }

        Status = "Failed";
        IsRunning = false;
        _ = _events.CloseAsync();
        Notify();
    }

    private void OnSessionEvent(AiSessionEventPayload evt)
    {
        var turnId = _activeTurnId;
        MergeEvent(evt);

        if (turnId is not null)
        {
            var response = RenderEvents(_eventsBuffer);
            ReplaceTurn(turnId, t => t with { Response = response });
        }

        if (!string.IsNullOrEmpty(evt.Status))
        {
            Status = ToDisplayStatus(evt.Status);
        }

        if (evt.IsTerminal)
        {
            IsRunning = false;
            _activeTurnId = null;
            _ = FinishTerminalAsync();
        }

        Notify();
    }

    private async Task FinishTerminalAsync()
    {
        await _events.CloseAsync();
        await RefreshConversationsAsync();
    }

    private async Task RefreshConversationsAsync()
    {
        try
        {
            ICollection<AtlasApiDTOsAiAiConversationListItemDto> recent = await _api.AtlasApiEndpointsAiListAiConversationsEndpointAsync(25);
            _conversations.Clear();
            _conversations.AddRange(recent);
            Notify();
        }
        catch
        {
            // History is useful but non-critical.
        }
    }

    private void MergeEvent(AiSessionEventPayload evt)
    {
        var idx = _eventsBuffer.FindIndex(e => e.EventId == evt.EventId);
        if (idx >= 0)
        {
            _eventsBuffer[idx] = evt;
        }
        else
        {
            _eventsBuffer.Add(evt);
        }

        _eventsBuffer.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
    }

    private void ReplaceTurn(string turnId, Func<AiTranscriptTurn, AiTranscriptTurn> map)
    {
        for (var i = 0; i < _turns.Count; i++)
        {
            if (_turns[i].Id == turnId)
            {
                _turns[i] = map(_turns[i]);
                return;
            }
        }
    }

    private void Notify() => Changed?.Invoke();

    private static AtlasApplicationAbstractionsAiAiViewScope? ResolveView(string title)
    {
        var lower = title.ToLowerInvariant();
        if (lower.Contains("tasks"))
        {
            return AtlasApplicationAbstractionsAiAiViewScope.Tasks;
        }

        if (lower.Contains("dashboard"))
        {
            return AtlasApplicationAbstractionsAiAiViewScope.Dashboard;
        }

        if (lower.Contains("team"))
        {
            return AtlasApplicationAbstractionsAiAiViewScope.Team;
        }

        if (lower.Contains("risks") || lower.Contains("risk"))
        {
            return AtlasApplicationAbstractionsAiAiViewScope.Risks;
        }

        if (lower.Contains("projects") || lower.Contains("project"))
        {
            return AtlasApplicationAbstractionsAiAiViewScope.Projects;
        }

        if (lower.Contains("settings"))
        {
            return AtlasApplicationAbstractionsAiAiViewScope.Settings;
        }

        return null;
    }

    private static string RenderEvents(IEnumerable<AiSessionEventPayload> events)
    {
        var text = "";
        foreach (AiSessionEventPayload evt in events.OrderBy(e => e.Sequence))
        {
            if (evt.Type == "model.delta" && !string.IsNullOrEmpty(evt.Delta))
            {
                text += evt.Delta;
                continue;
            }

            if ((evt.Type is "session.failed" or "session.cancelled") && !string.IsNullOrEmpty(evt.Message))
            {
                if (text.Length > 0 && !text.EndsWith('\n'))
                {
                    text += "\n";
                }

                text += evt.Message + "\n";
            }
        }

        return text;
    }

    private static List<AiSessionEventPayload> SortEvents(IEnumerable<AiSessionEventPayload> events) =>
        events.OrderBy(e => e.Sequence).ToList();

    private static AiSessionEventPayload? MapDto(AtlasApiDTOsAiAiSessionEventDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new AiSessionEventPayload
        {
            EventId = dto.EventId ?? Guid.Empty,
            SessionId = dto.SessionId ?? Guid.Empty,
            Sequence = dto.Sequence ?? 0,
            Type = dto.Type ?? "",
            Status = dto.Status,
            Message = dto.Message,
            Delta = dto.Delta,
            OccurredAtUtc = dto.OccurredAtUtc ?? default(DateTimeOffset),
            IsTerminal = dto.IsTerminal ?? false,
        };
    }

    private static string ToDisplayStatus(string status) => status switch
    {
        "gathering_context" => "Gathering context...",
        "using_history" => "Using conversation history...",
        "model_requested" => "Calling model...",
        "streaming" => "Streaming response...",
        "completed" => "Completed",
        "failed" => "Failed",
        "cancelled" => "Cancelled",
        "started" => "Started",
        _ => status,
    };

    private static string NewTurnId() => Guid.NewGuid().ToString("N");

    public async ValueTask DisposeAsync()
    {
        if (_subscribedCache)
        {
            _cache.Changed -= OnCacheChanged;
        }

        await _events.DisposeAsync();
    }
}
