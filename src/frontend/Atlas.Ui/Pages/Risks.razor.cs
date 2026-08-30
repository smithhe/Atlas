using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Risks : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private RiskService RiskService { get; set; } = null!;

    [Parameter] public string? RiskId { get; set; }

    private bool _editing;
    private bool _creating;
    private bool _deleting;
    private Guid? _autoEditId;
    private Guid? _trackedRiskId;

    private string _statusFilter = "All";
    private string _projectFilter = "";
    private string _severityFilter = "All";

    private bool _isAddingNote;
    private string _newNoteText = "";
    private Guid? _selectedHistoryId;
    private string _historyDraftText = "";
    private string _historyEditTab = "Write";

    private bool IsFocusMode => !string.IsNullOrEmpty(RiskId);
    private bool ShowDetail => IsFocusMode || Selection.SelectedRiskId is not null;

    private IReadOnlyList<string> ProjectOptions =>
        Cache.Projects.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

    private IReadOnlyList<Risk> Filtered => Cache.Risks.Where(MatchesFilters).ToList();

    private Risk? Selected
    {
        get
        {
            Guid? id = IsFocusMode && Guid.TryParse(RiskId, out Guid focusId)
                ? focusId
                : Selection.SelectedRiskId;
            if (id is null)
            {
                return null;
            }

            return Cache.Risks.FirstOrDefault(r => r.Id == id);
        }
    }

    protected override void OnInitialized()
    {
        Cache.Changed += OnChanged;
        Selection.Changed += OnChanged;
        Ai.SetContext("Context: Risks",
        [
            new AiAction("summarize-impact", "Summarize impact"),
            new AiAction("suggest-mitigations", "Suggest mitigations"),
            new AiAction("why-matters", "Explain “why this matters”"),
        ]);
        _ = Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        if (IsFocusMode && Guid.TryParse(RiskId, out Guid id))
        {
            Selection.SelectRisk(id);
            if (Cache.RisksReady && Cache.Risks.All(r => r.Id != id))
            {
                Nav.NavigateTo("/risks", replace: true);
            }
        }

        SyncDetailUiForRisk(Selected?.Id);
    }

    private void OnChanged() => InvokeAsync(() =>
    {
        if (IsFocusMode && Guid.TryParse(RiskId, out Guid id) && Cache.RisksReady && Cache.Risks.All(r => r.Id != id))
        {
            Nav.NavigateTo("/risks", replace: true);
            return;
        }

        SyncDetailUiForRisk(Selected?.Id);
        StateHasChanged();
    });

    private void SyncDetailUiForRisk(Guid? riskId)
    {
        if (_trackedRiskId == riskId)
        {
            if (riskId is not null && _autoEditId == riskId)
            {
                _editing = true;
            }

            return;
        }

        _trackedRiskId = riskId;
        ResetDetailUiState();
        _editing = riskId is not null && _autoEditId == riskId;
    }

    private void ResetDetailUiState()
    {
        _isAddingNote = false;
        _newNoteText = "";
        _selectedHistoryId = null;
        _historyDraftText = "";
        _historyEditTab = "Write";
    }

    private bool MatchesFilters(Risk r)
    {
        if (_statusFilter != "All" && r.Status.ToString() != _statusFilter)
        {
            return false;
        }

        if (_severityFilter != "All" && r.Severity != _severityFilter)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_projectFilter)
            && !(r.Project ?? "").Contains(_projectFilter.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private void OnStatusFilterChange(ChangeEventArgs e) => _statusFilter = e.Value?.ToString() ?? "All";
    private void OnSeverityFilterChange(ChangeEventArgs e) => _severityFilter = e.Value?.ToString() ?? "All";
    private void OnProjectFilterInput(ChangeEventArgs e) => _projectFilter = e.Value?.ToString() ?? "";

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

    private void SelectFromList(Guid id)
    {
        if (Selection.SelectedRiskId != id)
        {
            ResetDetailUiState();
            _editing = _autoEditId == id;
        }

        Selection.SelectRisk(id);
        _trackedRiskId = id;
    }

    private void CloseDetail()
    {
        Selection.SelectRisk(null);
        _trackedRiskId = null;
        ResetDetailUiState();
    }

    private void ToggleEdit()
    {
        _editing = !_editing;
        if (!_editing && Selected is not null && _autoEditId == Selected.Id)
        {
            _autoEditId = null;
        }
    }

    private void GoFocus(Guid id) => Nav.NavigateTo($"/risks/{id}");
    private void GoTask(Guid id) => Nav.NavigateTo($"/tasks/{id}");
    private void GoTeamMember(Guid id) => Nav.NavigateTo($"/team/{id}");

    private void EnterFocus()
    {
        if (Selected is null)
        {
            return;
        }

        Nav.NavigateTo($"/risks/{Selected.Id}");
    }

    private void ExitFocus() => Nav.NavigateTo("/risks");

    private async Task HandleAddRisk()
    {
        if (_creating)
        {
            return;
        }

        _creating = true;
        try
        {
            var draft = new Risk
            {
                Title = "New risk",
                Status = RiskStatus.Open,
                Severity = "Medium",
                Description = "",
                Evidence = "",
                LinkedTaskIds = Array.Empty<Guid>(),
                LinkedTeamMemberIds = Array.Empty<Guid>(),
                History = Array.Empty<RiskHistoryEntry>(),
                LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o")
            };
            Risk created = await RiskService.CreateAsync(draft);
        Guid id = created.Id;
            _autoEditId = id;
            _editing = true;
            _trackedRiskId = id;
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to create risk right now. Please try again.");
        }
        finally
        {
            _creating = false;
        }
    }

    private async Task HandleDelete()
    {
        if (Selected is null || _deleting)
        {
            return;
        }

        Risk risk = Selected;
        if (!await Dialogs.ConfirmAsync($"Delete risk \"{risk.Title}\"? This cannot be undone."))
        {
            return;
        }

        _deleting = true;
        try
        {
            await RiskService.DeleteAsync(risk.Id);
            if (IsFocusMode)
            {
                Nav.NavigateTo("/risks", replace: true);
            }

            if (_autoEditId == risk.Id)
            {
                _autoEditId = null;
            }

            _trackedRiskId = null;
            ResetDetailUiState();
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

    private void OnTitleInput(ChangeEventArgs e) => Persist(EntityClone.Risk(Selected!, title: e.Value?.ToString() ?? ""));
    private void OnDescriptionInput(ChangeEventArgs e) => Persist(EntityClone.Risk(Selected!, description: e.Value?.ToString() ?? ""));
    private void OnEvidenceInput(ChangeEventArgs e) => Persist(EntityClone.Risk(Selected!, evidence: e.Value?.ToString() ?? ""));

    private void OnStatusChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out RiskStatus s))
        {
            Persist(EntityClone.Risk(Selected!, status: s));
        }
    }

    private void OnSeverityChange(ChangeEventArgs e) => Persist(EntityClone.Risk(Selected!, severity: e.Value?.ToString() ?? "Low"));

    private void OnProjectChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        Persist(EntityClone.Risk(Selected!, project: string.IsNullOrEmpty(v) ? null : v, setProject: true));
    }

    private void OnTeamMemberToggle(Guid memberId, ChangeEventArgs e)
    {
        if (Selected is null)
        {
            return;
        }

        var linked = e.Value is bool b && b;
        var current = new HashSet<Guid>(Selected.LinkedTeamMemberIds);
        if (linked)
        {
            current.Add(memberId);
        }
        else
        {
            current.Remove(memberId);
        }

        var memberIds = current.ToList();
        var next = EntityClone.Risk(Selected, linkedTeamMemberIds: memberIds, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
        Cache.UpdateRisk(next);
        _ = PersistTeamMembersAsync(next.Id, memberIds);
    }

    private void ToggleAddingNote() => _isAddingNote = !_isAddingNote;
    private void OnNewNoteInput(ChangeEventArgs e) => _newNoteText = e.Value?.ToString() ?? "";

    private void AddNote()
    {
        if (Selected is null)
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
        var nextHistory = new[] { entry }.Concat(Selected.History).ToList();
        Persist(EntityClone.Risk(Selected, history: nextHistory));
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
        if (Selected is null || _selectedHistoryId is null)
        {
            return;
        }

        var text = _historyDraftText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var nextHistory = Selected.History
            .Select(h => h.Id == _selectedHistoryId ? new RiskHistoryEntry { Id = h.Id, CreatedIso = h.CreatedIso, Text = text } : h)
            .ToList();
        Persist(EntityClone.Risk(Selected, history: nextHistory));
        CloseHistoryNote();
    }

    private void Persist(Risk next, bool persistRisk = true)
    {
        next = EntityClone.Risk(next, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
        Cache.UpdateRisk(next);
        if (persistRisk)
        {
            _ = PersistAsync(next);
        }
    }

    private async Task PersistAsync(Risk next)
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

    private async Task PersistTeamMembersAsync(Guid riskId, IReadOnlyList<Guid> memberIds)
    {
        try
        {
            await RiskService.SetTeamMembersAsync(riskId, memberIds);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to save team member links right now. Please try again.");
        }
    }

    public void Dispose()
    {
        Cache.Changed -= OnChanged;
        Selection.Changed -= OnChanged;
    }
}
