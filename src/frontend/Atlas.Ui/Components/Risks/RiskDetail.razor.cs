using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Risks;

public partial class RiskDetail
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

    private bool _isAddingNote;
    private string _newNoteText = "";
    private Guid? _selectedHistoryId;
    private string _historyDraftText = "";
    private string _historyEditTab = "Write";

    private IReadOnlyList<string> ProjectOptions =>
        Cache.Projects.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

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
        Cache.Tasks.Where(t => t.Risk == risk.Title).ToList();

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

    private void OnTitleInput(ChangeEventArgs e) =>
        _ = SaveRiskAsync(EntityClone.Risk(Risk!, title: e.Value?.ToString() ?? "", lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));
    private void OnDescriptionInput(ChangeEventArgs e) =>
        _ = SaveRiskAsync(EntityClone.Risk(Risk!, description: e.Value?.ToString() ?? "", lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));
    private void OnEvidenceInput(ChangeEventArgs e) =>
        _ = SaveRiskAsync(EntityClone.Risk(Risk!, evidence: e.Value?.ToString() ?? "", lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));

    private void OnStatusChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out RiskStatus s))
        {
            _ = SaveRiskAsync(EntityClone.Risk(Risk!, status: s, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));
        }
    }

    private void OnSeverityChange(ChangeEventArgs e) =>
        _ = SaveRiskAsync(EntityClone.Risk(Risk!, severity: e.Value?.ToString() ?? "Low", lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));

    private void OnProjectChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        _ = SaveRiskAsync(EntityClone.Risk(Risk!, project: string.IsNullOrEmpty(v) ? null : v, setProject: true, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));
    }

    private void OnTeamMemberToggle(Guid memberId, ChangeEventArgs e)
    {
        if (Risk is null)
        {
            return;
        }

        var linked = e.Value is bool b && b;
        var current = new HashSet<Guid>(Risk.LinkedTeamMemberIds);
        if (linked)
        {
            current.Add(memberId);
        }
        else
        {
            current.Remove(memberId);
        }

        var memberIds = current.ToList();
        var next = EntityClone.Risk(Risk, linkedTeamMemberIds: memberIds, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
        _ = SaveTeamMembersAsync(next);
    }

    private void ToggleAddingNote() => _isAddingNote = !_isAddingNote;
    private void OnNewNoteInput(ChangeEventArgs e) => _newNoteText = e.Value?.ToString() ?? "";

    private void AddNote()
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
        var nextHistory = new[] { entry }.Concat(Risk.History).ToList();
        _ = SaveRiskAsync(EntityClone.Risk(Risk, history: nextHistory, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));
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

    private void SaveHistoryNote()
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

        var nextHistory = Risk.History
            .Select(h => h.Id == _selectedHistoryId ? new RiskHistoryEntry { Id = h.Id, CreatedIso = h.CreatedIso, Text = text } : h)
            .ToList();
        _ = SaveRiskAsync(EntityClone.Risk(Risk, history: nextHistory, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")));
        CloseHistoryNote();
    }

    private async Task SaveRiskAsync(Risk next)
    {
        try
        {
            await RiskService.UpdateAsync(next);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to save risk changes right now. Please try again.");
        }
    }

    private async Task SaveTeamMembersAsync(Risk risk)
    {
        try
        {
            await RiskService.SetTeamMembersAsync(risk);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to save team member links right now. Please try again.");
        }
    }
}
