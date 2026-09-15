using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Risks
{
public partial class RiskDetail : IDisposable
{
    [Inject] private IAppCacheService _cache { get; set; } = null!;
    [Inject] private NavigationManager _nav { get; set; } = null!;
    [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
    [Inject] private IRiskService _riskService { get; set; } = null!;

    [Parameter, EditorRequired] public Risk? Risk { get; set; }
    [Parameter] public bool IsFocusMode { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnEnterFocus { get; set; }
    [Parameter] public EventCallback OnExitFocus { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public Guid? AutoEditId { get; set; }
    [Parameter] public EventCallback AutoEditCleared { get; set; }

    private bool Editing { get; set; }
    private bool Deleting { get; set; }
    private Guid? TrackedRiskId { get; set; }
    private EntitySaveState SaveState { get; set; } = EntitySaveState.Idle;

    private bool IsAddingNote { get; set; }
    private string NewNoteText { get; set; } = "";
    private Guid? SelectedHistoryId { get; set; }
    private string HistoryDraftText { get; set; } = "";
    private string HistoryEditTab { get; set; } = "Write";

    private IReadOnlyList<Project> ProjectOptions =>
        this._cache.Projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

    protected override void OnInitialized()
    {
        this._riskService.SaveStateChanged += OnSaveStateChangedAsync;
    }

    protected override void OnParametersSet()
    {
        SyncDetailUiForRisk(Risk?.Id);
    }

    private void SyncDetailUiForRisk(Guid? riskId)
    {
        if (this.TrackedRiskId == riskId)
        {
            if (riskId is not null && AutoEditId == riskId)
            {
                this.Editing = true;
            }

            return;
        }

        this.TrackedRiskId = riskId;
        ResetDetailUiState();
        this.Editing = riskId is not null && AutoEditId == riskId;
        this.SaveState = riskId is not null ? this._riskService.GetSaveState(riskId.Value) : EntitySaveState.Idle;
    }

    private async void OnSaveStateChangedAsync(Guid riskId)
    {
        try
        {
            if (Risk?.Id != riskId)
            {
                return;
            }

            await InvokeAsync(() =>
            {
                this.SaveState = this._riskService.GetSaveState(riskId);
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void ResetDetailUiState()
    {
        this.IsAddingNote = false;
        this.NewNoteText = "";
        this.SelectedHistoryId = null;
        this.HistoryDraftText = "";
        this.HistoryEditTab = "Write";
    }

    private IReadOnlyList<AtlasTask> GetLinkedTasks(Risk risk) =>
        this._cache.Tasks.Where(t => t.RiskId == risk.Id).ToList();

    private IReadOnlyList<TeamMember> GetLinkedMembers(Risk risk)
    {
        var byId = this._cache.Team.ToDictionary(m => m.Id);
        return risk.LinkedTeamMemberIds
            .Select(id => byId.GetValueOrDefault(id))
            .Where(m => m is not null)
            .Cast<TeamMember>()
            .ToList();
    }

    private async void ClearAutoEditFireAndForget()
    {
        try
        {
            await AutoEditCleared.InvokeAsync();
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void ToggleEdit()
    {
        this.Editing = !this.Editing;
        if (!this.Editing && Risk is not null && AutoEditId == Risk.Id)
        {
            ClearAutoEditFireAndForget();
        }
    }

    private void GoTask(Guid id) => this._nav.NavigateTo($"/tasks/{id}");
    private void GoTeamMember(Guid id) => this._nav.NavigateTo($"/team/{id}");

    private async Task HandleDelete()
    {
        if (Risk is null || this.Deleting)
        {
            return;
        }

        Risk risk = Risk;
        if (!await this._dialogs.ConfirmAsync($"Delete risk \"{risk.Title}\"? This cannot be undone."))
        {
            return;
        }

        this.Deleting = true;
        try
        {
            await this._riskService.DeleteAsync(risk.Id);
            this.TrackedRiskId = null;
            ResetDetailUiState();
            await OnDeleted.InvokeAsync();
        }
        catch (Exception)
        {
            await this._dialogs.AlertAsync("Unable to delete this risk right now. Please try again.");
        }
        finally
        {
            this.Deleting = false;
        }
    }

    private async Task OnTitleInput(ChangeEventArgs e) =>
        await SaveRiskAsync(
            r => EntityClone.Risk(r, title: e.Value?.ToString() ?? "", lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")),
            debounce: true);

    private async Task OnDescriptionInput(ChangeEventArgs e) =>
        await SaveRiskAsync(
            r => EntityClone.Risk(r, description: e.Value?.ToString() ?? "", lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")),
            debounce: true);

    private async Task OnEvidenceInput(ChangeEventArgs e) =>
        await SaveRiskAsync(
            r => EntityClone.Risk(r, evidence: e.Value?.ToString() ?? "", lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")),
            debounce: true);

    private async Task OnStatusChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out RiskStatus s))
        {
            await SaveRiskAsync(
                r => EntityClone.Risk(r, status: s, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));
        }
    }

    private async Task OnSeverityChange(ChangeEventArgs e) =>
        await SaveRiskAsync(
            r => EntityClone.Risk(r, severity: e.Value?.ToString() ?? "Low", lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));

    private async Task OnProjectChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        Guid? projectId = null;
        if (!string.IsNullOrEmpty(v) && Guid.TryParse(v, out Guid parsed))
        {
            projectId = parsed;
        }

        string? projectName = projectId is null ? null : this._cache.Projects.FirstOrDefault(p => p.Id == projectId)?.Name;
        await SaveRiskAsync(r => EntityClone.Risk(
            r,
            projectId: projectId,
            setProjectId: true,
            project: projectName,
            setProject: true,
            lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));
    }

    private async Task OnTeamMemberToggle(Guid memberId, ChangeEventArgs e)
    {
        if (Risk is null)
        {
            return;
        }

        var linked = e.Value is bool b && b;
        Risk latest = this._cache.TryGetRisk(Risk.Id) ?? Risk;
        var current = new HashSet<Guid>(latest.LinkedTeamMemberIds);
        if (linked)
        {
            current.Add(memberId);
        }
        else
        {
            current.Remove(memberId);
        }

        await SaveTeamMembersAsync(current.ToList());
    }

    private void ToggleAddingNote() => this.IsAddingNote = !this.IsAddingNote;
    private void OnNewNoteInput(ChangeEventArgs e) => this.NewNoteText = e.Value?.ToString() ?? "";

    private async Task AddNote()
    {
        if (Risk is null)
        {
            return;
        }

        var text = this.NewNoteText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var entry = new RiskHistoryEntry
        {
            Id = Guid.NewGuid(),
            CreatedIso = DateTimeOffset.UtcNow.ToString("o"),
            Text = text
        };
        await SaveRiskAsync(r =>
        {
            Risk latest = this._cache.TryGetRisk(r.Id) ?? r;
            var nextHistory = new[] { entry }.Concat(latest.History).ToList();
            return EntityClone.Risk(latest, history: nextHistory, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
        });
        this.NewNoteText = "";
        this.IsAddingNote = false;
    }

    private void OpenHistoryNote(Guid historyId, string text)
    {
        this.SelectedHistoryId = historyId;
        this.HistoryDraftText = text;
        this.HistoryEditTab = "Write";
    }

    private void CloseHistoryNote()
    {
        this.SelectedHistoryId = null;
        this.HistoryDraftText = "";
        this.HistoryEditTab = "Write";
    }

    private void OnHistoryDraftInput(ChangeEventArgs e) => this.HistoryDraftText = e.Value?.ToString() ?? "";

    private async Task SaveHistoryNote()
    {
        if (Risk is null || this.SelectedHistoryId is null)
        {
            return;
        }

        var text = this.HistoryDraftText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Guid historyId = this.SelectedHistoryId.Value;
        await SaveRiskAsync(r =>
        {
            Risk latest = this._cache.TryGetRisk(r.Id) ?? r;
            var nextHistory = latest.History
                .Select(h => h.Id == historyId ? new RiskHistoryEntry { Id = h.Id, CreatedIso = h.CreatedIso, Text = text } : h)
                .ToList();
            return EntityClone.Risk(latest, history: nextHistory, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
        });
        CloseHistoryNote();
    }

    private async Task SaveRiskAsync(Func<Risk, Risk> edit, bool debounce = false)
    {
        if (Risk is null)
        {
            return;
        }

        Guid riskId = Risk.Id;
        try
        {
            await this._riskService.UpdateAsync(riskId, edit, debounce);
            this.SaveState = this._riskService.GetSaveState(riskId);
        }
        catch (Exception)
        {
            this.SaveState = this._riskService.GetSaveState(riskId);
            await this._dialogs.AlertAsync("Unable to save risk changes right now. Please try again.");
        }
    }

    private async Task SaveTeamMembersAsync(IReadOnlyList<Guid> memberIds)
    {
        if (Risk is null)
        {
            return;
        }

        try
        {
            await this._riskService.SetTeamMembersAsync(Risk.Id, memberIds);
        }
        catch (Exception)
        {
            this.SaveState = EntitySaveState.Failed;
            await this._dialogs.AlertAsync("Unable to save team member links right now. Please try again.");
        }
    }

    private string SaveStateLabel => this.SaveState switch
    {
        EntitySaveState.Saving => "Saving…",
        EntitySaveState.Saved => "Saved",
        EntitySaveState.Failed => "Save failed",
        EntitySaveState.Idle => "",
        _ => ""
    };

    public void Dispose() => this._riskService.SaveStateChanged -= OnSaveStateChangedAsync;
}
}
