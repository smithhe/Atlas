using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Ai
{
    public partial class AiPanel : IAsyncDisposable
    {
        [Inject] private IAiStateService _ai { get; set; } = null!;
        [Inject] private IJSRuntime _js { get; set; } = null!;

        private ElementReference ScrollRef { get; set; }
        private string? CopiedTurnId { get; set; }
        private CancellationTokenSource? CopyResetCts { get; set; }
        private AiPanelInterop? AiPanelInteropRef { get; set; }

        private string LatestAssistantText
        {
            get
            {
                for (int i = this._ai.Turns.Count - 1; i >= 0; i--)
                {
                    string? text = this._ai.Turns[i].Response?.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
                return "";
            }
        }

        private bool CanInsertDraft =>
            this._ai.HasDraftTarget
            && !string.IsNullOrEmpty(LatestAssistantText)
            && !IsOpenAiNotConfigured(LatestAssistantText);

        private string InsertDraftTitle
        {
            get
            {
                if (!this._ai.HasDraftTarget)
                {
                    return "Edit a field first";
                }

                if (string.IsNullOrEmpty(LatestAssistantText))
                {
                    return "No assistant response to insert";
                }

                if (!string.IsNullOrEmpty(this._ai.DraftTargetLabel))
                {
                    return $"Insert into {this._ai.DraftTargetLabel}";
                }

                return "Insert Draft";
            }
        }

        private bool ShowOpenAiSetup
        {
            get
            {
                if (!string.IsNullOrEmpty(this._ai.Notice) && IsOpenAiNotConfigured(this._ai.Notice))
                {
                    return true;
                }

                return this._ai.Turns.Any(t => IsOpenAiNotConfigured(t.Response));
            }
        }

        private bool ShouldStickToBottom
        {
            get
            {
                if (this._ai.IsRunning)
                {
                    return true;
                }

                AiTranscriptTurn? last = this._ai.Turns.LastOrDefault();
                return last is not null && string.IsNullOrWhiteSpace(last.Response);
            }
        }

        protected override void OnInitialized()
        {
            this._ai.Changed += OnAiChangedAsync;
            this._ai.EnsureStartupPreference();
            if (this._ai.IsOpen)
            {
                this._ai.LoadConversations();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                this.AiPanelInteropRef = new AiPanelInterop(this._js);
            }
        }

        private async void OnAiChangedAsync()
        {
            try
            {
                await InvokeAsync(async () =>
                {
                    StateHasChanged();
                    if (ShouldStickToBottom && this._ai.IsOpen && this.AiPanelInteropRef is not null)
                    {
                        try
                        {
                            await this.AiPanelInteropRef.ScrollToBottomAsync(this.ScrollRef);
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

        private void OnPromptInput(ChangeEventArgs e) => this._ai.SetPromptDraft(e.Value?.ToString() ?? "");

        private void SendPrompt()
        {
            string prompt = this._ai.PromptDraft;
            this._ai.SetPromptDraft("");
            this._ai.SendPrompt(prompt);
        }

        private async Task HandleInsertDraft()
        {
            if (!CanInsertDraft)
            {
                if (!this._ai.HasDraftTarget)
                {
                    this._ai.AppendOutput("\nEdit a task note or note body first.\n");
                }

                return;
            }

            if (!await this._ai.InsertDraftAsync(LatestAssistantText))
            {
                this._ai.AppendOutput("\nEdit a task note or note body first.\n");
            }
        }

        private async Task CopyTurnResponse(AiTranscriptTurn turn)
        {
            if (string.IsNullOrWhiteSpace(turn.Response))
            {
                return;
            }

            if (this.AiPanelInteropRef is null)
            {
                this.AiPanelInteropRef = new AiPanelInterop(this._js);
            }

            try
            {
                bool ok = await this.AiPanelInteropRef.CopyTextAsync(turn.Response);
                if (!ok)
                {
                    this._ai.AppendOutput("Copy failed — clipboard permission unavailable.");
                    return;
                }

                this.CopiedTurnId = turn.Id;
                this.CopyResetCts?.Cancel();
                this.CopyResetCts?.Dispose();
                this.CopyResetCts = new CancellationTokenSource();
                CancellationToken token = this.CopyResetCts.Token;
                try
                {
                    await Task.Delay(1500, token);
                    if (this.CopiedTurnId == turn.Id)
                    {
                        this.CopiedTurnId = null;
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
                this._ai.AppendOutput("Copy failed — clipboard permission unavailable.");
            }
            catch (ObjectDisposedException)
            {
                this._ai.AppendOutput("Copy failed — clipboard permission unavailable.");
            }
            catch (InvalidOperationException)
            {
                this._ai.AppendOutput("Copy failed — clipboard permission unavailable.");
            }
        }

        private async Task OnConversationChange(ChangeEventArgs e)
        {
            if (Guid.TryParse(e.Value?.ToString(), out Guid id))
            {
                await this._ai.OpenConversationAsync(id);
            }
        }

        private async Task StartNewSessionAsync()
        {
            await this._ai.StartNewSessionAsync();
        }

        private static bool IsOpenAiNotConfigured(string text) => text.Contains("OpenAI is not configured", StringComparison.Ordinal);

        public async ValueTask DisposeAsync()
        {
            this._ai.Changed -= OnAiChangedAsync;
            if (this.CopyResetCts is not null)
            {
                await this.CopyResetCts.CancelAsync();
                this.CopyResetCts.Dispose();
                this.CopyResetCts = null;
            }

            if (this.AiPanelInteropRef is not null)
            {
                await this.AiPanelInteropRef.DisposeAsync();
                this.AiPanelInteropRef = null;
            }
        }
    }
}
