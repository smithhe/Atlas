using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Ai;

public partial class AiPanel : IAsyncDisposable
{
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private ElementReference _scrollRef;
    private string? _copiedTurnId;
    private CancellationTokenSource? _copyResetCts;
    private AiPanelInterop? _aiPanelInterop;

    private string LatestAssistantText
    {
        get
        {
            for (int i = Ai.Turns.Count - 1; i >= 0; i--)
            {
                string? text = Ai.Turns[i].Response?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }
            return "";
        }
    }

    private bool CanInsertDraft =>
        Ai.HasDraftTarget
        && !string.IsNullOrEmpty(LatestAssistantText)
        && !IsOpenAiNotConfigured(LatestAssistantText);

    private string InsertDraftTitle
    {
        get
        {
            if (!Ai.HasDraftTarget)
            {
                return "Edit a field first";
            }

            if (string.IsNullOrEmpty(LatestAssistantText))
            {
                return "No assistant response to insert";
            }

            if (!string.IsNullOrEmpty(Ai.DraftTargetLabel))
            {
                return $"Insert into {Ai.DraftTargetLabel}";
            }

            return "Insert Draft";
        }
    }

    private bool ShowOpenAiSetup
    {
        get
        {
            if (!string.IsNullOrEmpty(Ai.Notice) && IsOpenAiNotConfigured(Ai.Notice))
            {
                return true;
            }

            return Ai.Turns.Any(t => IsOpenAiNotConfigured(t.Response));
        }
    }

    private bool ShouldStickToBottom
    {
        get
        {
            if (Ai.IsRunning)
            {
                return true;
            }

            AiTranscriptTurn? last = Ai.Turns.LastOrDefault();
            return last is not null && string.IsNullOrWhiteSpace(last.Response);
        }
    }

    protected override void OnInitialized()
    {
        Ai.Changed += OnAiChangedAsync;
        Ai.EnsureStartupPreference();
        if (Ai.IsOpen)
        {
            Ai.LoadConversations();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _aiPanelInterop = new AiPanelInterop(Js);
        }
    }

    private async void OnAiChangedAsync()
    {
        try
        {
            await InvokeAsync(async () =>
            {
                StateHasChanged();
                if (ShouldStickToBottom && Ai.IsOpen && _aiPanelInterop is not null)
                {
                    try
                    {
                        await _aiPanelInterop.ScrollToBottomAsync(_scrollRef);
                    }
                    catch (JSDisconnectedException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void OnPromptInput(ChangeEventArgs e) => Ai.SetPromptDraft(e.Value?.ToString() ?? "");

    private void SendPrompt()
    {
        string prompt = Ai.PromptDraft;
        Ai.SetPromptDraft("");
        Ai.SendPrompt(prompt);
    }

    private async Task HandleInsertDraft()
    {
        if (!CanInsertDraft)
        {
            if (!Ai.HasDraftTarget)
            {
                Ai.AppendOutput("\nEdit a task note or note body first.\n");
            }

            return;
        }

        if (!await Ai.InsertDraftAsync(LatestAssistantText))
        {
            Ai.AppendOutput("\nEdit a task note or note body first.\n");
        }
    }

    private async Task CopyTurnResponse(AiTranscriptTurn turn)
    {
        if (string.IsNullOrWhiteSpace(turn.Response))
        {
            return;
        }

        if (_aiPanelInterop is null)
        {
            _aiPanelInterop = new AiPanelInterop(Js);
        }

        try
        {
            bool ok = await _aiPanelInterop.CopyTextAsync(turn.Response);
            if (!ok)
            {
                Ai.AppendOutput("Copy failed — clipboard permission unavailable.");
                return;
            }

            _copiedTurnId = turn.Id;
            _copyResetCts?.Cancel();
            _copyResetCts?.Dispose();
            _copyResetCts = new CancellationTokenSource();
            CancellationToken token = _copyResetCts.Token;
            try
            {
                await Task.Delay(1500, token);
                if (_copiedTurnId == turn.Id)
                {
                    _copiedTurnId = null;
                }

                StateHasChanged();
            }
            catch (OperationCanceledException)
            {
                // superseded
            }
        }
        catch (JSDisconnectedException)
        {
            Ai.AppendOutput("Copy failed — clipboard permission unavailable.");
        }
        catch (ObjectDisposedException)
        {
            Ai.AppendOutput("Copy failed — clipboard permission unavailable.");
        }
        catch (InvalidOperationException)
        {
            Ai.AppendOutput("Copy failed — clipboard permission unavailable.");
        }
    }

    private async Task OnConversationChange(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out Guid id))
        {
            await Ai.OpenConversationAsync(id);
        }
    }

    private async Task StartNewSessionAsync()
    {
        await Ai.StartNewSessionAsync();
    }

    private static bool IsOpenAiNotConfigured(string text) => text.Contains("OpenAI is not configured", StringComparison.Ordinal);

    public async ValueTask DisposeAsync()
    {
        Ai.Changed -= OnAiChangedAsync;
        if (_copyResetCts is not null)
        {
            await _copyResetCts.CancelAsync();
            _copyResetCts.Dispose();
            _copyResetCts = null;
        }

        if (_aiPanelInterop is not null)
        {
            await _aiPanelInterop.DisposeAsync();
            _aiPanelInterop = null;
        }
    }
}
