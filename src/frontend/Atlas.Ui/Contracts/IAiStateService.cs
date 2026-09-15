using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Contracts
{
    public interface IAiStateService : IAsyncDisposable
    {
        event Action? Changed;

        bool IsOpen { get; }

        string ContextTitle { get; }

        IReadOnlyList<AiAction> Actions { get; }

        IReadOnlyList<AiConversationListItem> Conversations { get; }

        IReadOnlyList<AiTranscriptTurn> Turns { get; }

        string? Notice { get; }

        string Status { get; }

        bool IsRunning { get; }

        bool IsLoadingHistory { get; }

        Guid? ActiveConversationId { get; }

        Guid? ActiveSessionId { get; }

        string PromptDraft { get; }

        int? PanelWidthPx { get; }

        string? DraftTargetLabel { get; }

        bool HasDraftTarget { get; }

        bool IsContextSupported { get; }

        string? ContextSupportMessage { get; }

        void EnsureStartupPreference();

        void SetIsOpen(bool isOpen);

        void SetPanelWidthPx(int? px);

        void SetContext(string contextTitle, IEnumerable<AiAction>? actions = null);

        void SetPromptDraft(string text);

        void RegisterDraftTarget(AiDraftTarget? target);

        Task<bool> InsertDraftAsync(string text);

        void ClearOutput();

        void AppendOutput(string text);

        Task StartNewSessionAsync();

        void LoadConversations();

        Task OpenConversationAsync(Guid conversationId);

        void RunAction(string actionId, string? promptOverride = null);

        void SendPrompt(string prompt);
    }
}
