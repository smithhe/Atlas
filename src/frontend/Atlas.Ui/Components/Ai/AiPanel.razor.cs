using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Ai;

public partial class AiPanel : IDisposable
{
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private ElementReference _scrollRef;
    private string? _copiedTurnId;
    private CancellationTokenSource? _copyResetCts;

    private string LatestAssistantText
    {
        get
        {
            for (var i = Ai.Turns.Count - 1; i >= 0; i--)
            {
                var text = Ai.Turns[i].Response?.Trim();
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
        Ai.Changed += OnAiChanged;
        Ai.EnsureStartupPreference();
        if (Ai.IsOpen)
        {
            Ai.LoadConversations();
        }
    }

    private void OnAiChanged() => InvokeAsync(async () =>
    {
        StateHasChanged();
        if (ShouldStickToBottom && Ai.IsOpen)
        {
            try
            {
                await Js.InvokeVoidAsync("atlasAiEvents.scrollToBottom", _scrollRef);
            }
            catch
            {
                // Element may not be mounted yet.
            }
        }
    });

    private void OnPromptInput(ChangeEventArgs e) => Ai.SetPromptDraft(e.Value?.ToString() ?? "");

    private void SendPrompt()
    {
        var prompt = Ai.PromptDraft;
        Ai.SetPromptDraft("");
        Ai.SendPrompt(prompt);
    }

    private void HandleInsertDraft()
    {
        if (!CanInsertDraft)
        {
            if (!Ai.HasDraftTarget)
            {
                Ai.AppendOutput("\nEdit a task note or note body first.\n");
            }

            return;
        }

        if (!Ai.InsertDraft(LatestAssistantText))
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

        try
        {
            var ok = await Js.InvokeAsync<bool>("atlasAiEvents.copyText", turn.Response);
            if (!ok)
            {
                Ai.AppendOutput("Copy failed — clipboard permission unavailable.");
                return;
            }

            _copiedTurnId = turn.Id;
            _copyResetCts?.Cancel();
            _copyResetCts = new CancellationTokenSource();
            CancellationToken token = _copyResetCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1500, token);
                    await InvokeAsync(() =>
                    {
                        if (_copiedTurnId == turn.Id)
                        {
                            _copiedTurnId = null;
                        }

                        StateHasChanged();
                    });
                }
                catch (TaskCanceledException)
                {
                    // superseded
                }
            });
            StateHasChanged();
        }
        catch
        {
            Ai.AppendOutput("Copy failed — clipboard permission unavailable.");
        }
    }

    private void OnConversationChange(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out Guid id))
        {
            _ = Ai.OpenConversationAsync(id);
        }
    }

    private static bool IsOpenAiNotConfigured(string text) => text.Contains("OpenAI is not configured", StringComparison.Ordinal);

    public void Dispose()
    {
        Ai.Changed -= OnAiChanged;
        _copyResetCts?.Cancel();
        _copyResetCts?.Dispose();
    }
}
