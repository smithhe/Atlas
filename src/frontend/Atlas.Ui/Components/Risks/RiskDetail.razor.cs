using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Risks;

public partial class RiskDetail : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private RiskService RiskService { get; set; } = null!;

    [Parameter] public Risk? Risk { get; set; }
    [Parameter] public bool IsFocusMode { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnEnterFocus { get; set; }
    [Parameter] public EventCallback OnExitFocus { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public Guid? AutoEditId { get; set; }
    [Parameter] public EventCallback AutoEditCleared { get; set; }

    private bool _editing;
    private bool _deleting;
    private Guid? _trackedRiskId;
    private EntitySaveState _saveState = EntitySaveState.Idle;

    private bool _isAddingNote;
    private string _newNoteText = "";
    private Guid? _selectedHistoryId;
    private string _historyDraftText = "";
    private string _historyEditTab = "Write";

    private IReadOnlyList<Project> ProjectOptions =>
        Cache.Projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

    protected override void OnInitialized()
    {
        RiskService.SaveStateChanged += OnSaveStateChangedAsync;
    }

    protected override void OnParametersSet()
    {
        SyncDetailUiForRisk(Risk?.Id);
    }

    private void SyncDetailUiForRisk(Guid? riskId)
    {
        if (_trackedRiskId == riskId)
        {
            if (riskId is not null && AutoEditId == riskId)
            {
                _editing = true;
            }

            return;
        }

        _trackedRiskId = riskId;
        ResetDetailUiState();
        _editing = riskId is not null && AutoEditId == riskId;
        _saveState = riskId is not null ? RiskService.GetSaveState(riskId.Value) : EntitySaveState.Idle;
    }

    private async void OnSaveStateChangedAsync(Guid riskId)
    {
        try
        {
            if (Risk?.Id != riskId)
            {
                return;
            }

            _saveState = RiskService.GetSaveState(riskId);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void ResetDetailUiState()
    {
        _isAddingNote = false;
        _newNoteText = "";
        _selectedHistoryId = null;
        _historyDraftText = "";
        _historyEditTab = "Write";
    }

    private IReadOnlyList<AtlasTask> GetLinkedTasks(Risk risk) =>
        Cache.Tasks.Where(t => t.RiskId == risk.Id).ToList();

    private IReadOnlyList<TeamMember> GetLinkedMembers(Risk risk)
    {
        var byId = Cache.Team.ToDictionary(m => m.Id);
        return risk.LinkedTeamMemberIds
            .Select(id => byId.GetValueOrDefault(id))
            .Where(m => m is not null)
            .Cast<TeamMember>()
            .ToList();
    }

    private void ToggleEdit()
    {
        _editing = !_editing;
        if (!_editing && Risk is not null && AutoEditId == Risk.Id)
        {
            _ = AutoEditCleared.InvokeAsync();
        }
    }

    private void GoTask(Guid id) => Nav.NavigateTo($"/tasks/{id}");
    private void GoTeamMember(Guid id) => Nav.NavigateTo($"/team/{id}");

    private async Task HandleDelete()
    {
        if (Risk is null || _deleting)
        {
            return;
        }

        Risk risk = Risk;
        if (!await Dialogs.ConfirmAsync($"Delete risk \"{risk.Title}\"? This cannot be undone."))
        {
            return;
        }

        _deleting = true;
        try
        {
            await RiskService.DeleteAsync(risk.Id);
            _trackedRiskId = null;
            ResetDetailUiState();
            await OnDeleted.InvokeAsync();
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to delete this risk right now. Please try again.");
        }
        finally
        {
            _deleting = false;
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

        string? projectName = projectId is null ? null : Cache.Projects.FirstOrDefault(p => p.Id == projectId)?.Name;
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
        Risk latest = Cache.TryGetRisk(Risk.Id) ?? Risk;
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

    private void ToggleAddingNote() => _isAddingNote = !_isAddingNote;
    private void OnNewNoteInput(ChangeEventArgs e) => _newNoteText = e.Value?.ToString() ?? "";

    private async Task AddNote()
    {
        if (Risk is null)
        {
            return;
        }

        var text = _newNoteText.Trim();
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
            Risk latest = Cache.TryGetRisk(r.Id) ?? r;
            var nextHistory = new[] { entry }.Concat(latest.History).ToList();
            return EntityClone.Risk(latest, history: nextHistory, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
        });
        _newNoteText = "";
        _isAddingNote = false;
    }

    private void OpenHistoryNote(Guid historyId, string text)
    {
        _selectedHistoryId = historyId;
        _historyDraftText = text;
        _historyEditTab = "Write";
    }

    private void CloseHistoryNote()
    {
        _selectedHistoryId = null;
        _historyDraftText = "";
        _historyEditTab = "Write";
    }

    private void OnHistoryDraftInput(ChangeEventArgs e) => _historyDraftText = e.Value?.ToString() ?? "";

    private async Task SaveHistoryNote()
    {
        if (Risk is null || _selectedHistoryId is null)
        {
            return;
        }

        var text = _historyDraftText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Guid historyId = _selectedHistoryId.Value;
        await SaveRiskAsync(r =>
        {
            Risk latest = Cache.TryGetRisk(r.Id) ?? r;
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
            await RiskService.UpdateAsync(riskId, edit, debounce);
            _saveState = RiskService.GetSaveState(riskId);
        }
        catch (Exception)
        {
            _saveState = RiskService.GetSaveState(riskId);
            await Dialogs.AlertAsync("Unable to save risk changes right now. Please try again.");
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
            await RiskService.SetTeamMembersAsync(Risk.Id, memberIds);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to save team member links right now. Please try again.");
        }
    }

    private string SaveStateLabel => _saveState switch
    {
        EntitySaveState.Saving => "Saving…",
        EntitySaveState.Saved => "Saved",
        EntitySaveState.Failed => "Save failed",
        EntitySaveState.Idle => "",
        _ => ""
    };

    public void Dispose() => RiskService.SaveStateChanged -= OnSaveStateChangedAsync;
}
