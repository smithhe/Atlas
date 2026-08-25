using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Risks : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] RiskService RiskService { get; set; } = default!;

    [Parameter] public string? RiskId { get; set; }

    bool _editing;
    bool _creating;
    bool _deleting;
    Guid? _autoEditId;
    Guid? _trackedRiskId;

    string _statusFilter = "All";
    string _projectFilter = "";
    string _severityFilter = "All";

    bool _isAddingNote;
    string _newNoteText = "";
    Guid? _selectedHistoryId;
    string _historyDraftText = "";
    string _historyEditTab = "Write";

    bool IsFocusMode => !string.IsNullOrEmpty(RiskId);
    bool ShowDetail => IsFocusMode || Selection.SelectedRiskId is not null;

    IReadOnlyList<string> ProjectOptions =>
        Cache.Projects.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

    IReadOnlyList<Risk> Filtered => Cache.Risks.Where(MatchesFilters).ToList();

    Risk? Selected
    {
        get
        {
            Guid? id = IsFocusMode && Guid.TryParse(RiskId, out Guid focusId)
                ? focusId
                : Selection.SelectedRiskId;
            if (id is null) return null;
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

    void OnChanged() => InvokeAsync(() =>
    {
        if (IsFocusMode && Guid.TryParse(RiskId, out Guid id) && Cache.RisksReady && Cache.Risks.All(r => r.Id != id))
        {
            Nav.NavigateTo("/risks", replace: true);
            return;
        }

        SyncDetailUiForRisk(Selected?.Id);
        StateHasChanged();
    });

    void SyncDetailUiForRisk(Guid? riskId)
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

    void ResetDetailUiState()
    {
        _isAddingNote = false;
        _newNoteText = "";
        _selectedHistoryId = null;
        _historyDraftText = "";
        _historyEditTab = "Write";
    }

    bool MatchesFilters(Risk r)
    {
        if (_statusFilter != "All" && r.Status.ToString() != _statusFilter) return false;
        if (_severityFilter != "All" && r.Severity != _severityFilter) return false;
        if (!string.IsNullOrWhiteSpace(_projectFilter)
            && !(r.Project ?? "").Contains(_projectFilter.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    void OnStatusFilterChange(ChangeEventArgs e) => _statusFilter = e.Value?.ToString() ?? "All";
    void OnSeverityFilterChange(ChangeEventArgs e) => _severityFilter = e.Value?.ToString() ?? "All";
    void OnProjectFilterInput(ChangeEventArgs e) => _projectFilter = e.Value?.ToString() ?? "";

    IReadOnlyList<AtlasTask> GetLinkedTasks(Risk risk) =>
        Cache.Tasks.Where(t => t.Risk == risk.Title).ToList();

    IReadOnlyList<TeamMember> GetLinkedMembers(Risk risk)
    {
        var byId = Cache.Team.ToDictionary(m => m.Id);
        return risk.LinkedTeamMemberIds
            .Select(id => byId.GetValueOrDefault(id))
            .Where(m => m is not null)
            .Cast<TeamMember>()
            .ToList();
    }

    void SelectFromList(Guid id)
    {
        if (Selection.SelectedRiskId != id)
        {
            ResetDetailUiState();
            _editing = _autoEditId == id;
        }

        Selection.SelectRisk(id);
        _trackedRiskId = id;
    }

    void CloseDetail()
    {
        Selection.SelectRisk(null);
        _trackedRiskId = null;
        ResetDetailUiState();
    }

    void ToggleEdit()
    {
        _editing = !_editing;
        if (!_editing && Selected is not null && _autoEditId == Selected.Id) _autoEditId = null;
    }

    void GoFocus(Guid id) => Nav.NavigateTo($"/risks/{id}");
    void GoTask(Guid id) => Nav.NavigateTo($"/tasks/{id}");
    void GoTeamMember(Guid id) => Nav.NavigateTo($"/team/{id}");

    void EnterFocus()
    {
        if (Selected is null) return;
        Nav.NavigateTo($"/risks/{Selected.Id}");
    }

    void ExitFocus() => Nav.NavigateTo("/risks");

    async Task HandleAddRisk()
    {
        if (_creating) return;
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

    async Task HandleDelete()
    {
        if (Selected is null || _deleting) return;
        Risk risk = Selected;
        if (!await Dialogs.ConfirmAsync($"Delete risk \"{risk.Title}\"? This cannot be undone.")) return;

        _deleting = true;
        try
        {
            await RiskService.DeleteAsync(risk.Id);
            if (IsFocusMode) Nav.NavigateTo("/risks", replace: true);
            if (_autoEditId == risk.Id) _autoEditId = null;
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

    void OnTitleInput(ChangeEventArgs e) => Persist(EntityClone.Risk(Selected!, title: e.Value?.ToString() ?? ""));
    void OnDescriptionInput(ChangeEventArgs e) => Persist(EntityClone.Risk(Selected!, description: e.Value?.ToString() ?? ""));
    void OnEvidenceInput(ChangeEventArgs e) => Persist(EntityClone.Risk(Selected!, evidence: e.Value?.ToString() ?? ""));

    void OnStatusChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out RiskStatus s))
            Persist(EntityClone.Risk(Selected!, status: s));
    }

    void OnSeverityChange(ChangeEventArgs e) => Persist(EntityClone.Risk(Selected!, severity: e.Value?.ToString() ?? "Low"));

    void OnProjectChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        Persist(EntityClone.Risk(Selected!, project: string.IsNullOrEmpty(v) ? null : v, setProject: true));
    }

    void OnTeamMemberToggle(Guid memberId, ChangeEventArgs e)
    {
        if (Selected is null) return;
        bool linked = e.Value is bool b && b;
        var current = new HashSet<Guid>(Selected.LinkedTeamMemberIds);
        if (linked) current.Add(memberId);
        else current.Remove(memberId);
        var memberIds = current.ToList();
        var next = EntityClone.Risk(Selected, linkedTeamMemberIds: memberIds, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
        Cache.UpdateRisk(next);
        _ = PersistTeamMembersAsync(next.Id, memberIds);
    }

    void ToggleAddingNote() => _isAddingNote = !_isAddingNote;
    void OnNewNoteInput(ChangeEventArgs e) => _newNoteText = e.Value?.ToString() ?? "";

    void AddNote()
    {
        if (Selected is null) return;
        string text = _newNoteText.Trim();
        if (string.IsNullOrEmpty(text)) return;

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

    void OpenHistoryNote(Guid historyId, string text)
    {
        _selectedHistoryId = historyId;
        _historyDraftText = text;
        _historyEditTab = "Write";
    }

    void CloseHistoryNote()
    {
        _selectedHistoryId = null;
        _historyDraftText = "";
        _historyEditTab = "Write";
    }

    void OnHistoryDraftInput(ChangeEventArgs e) => _historyDraftText = e.Value?.ToString() ?? "";

    void SaveHistoryNote()
    {
        if (Selected is null || _selectedHistoryId is null) return;
        string text = _historyDraftText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var nextHistory = Selected.History
            .Select(h => h.Id == _selectedHistoryId ? new RiskHistoryEntry { Id = h.Id, CreatedIso = h.CreatedIso, Text = text } : h)
            .ToList();
        Persist(EntityClone.Risk(Selected, history: nextHistory));
        CloseHistoryNote();
    }

    void Persist(Risk next, bool persistRisk = true)
    {
        next = EntityClone.Risk(next, lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
        Cache.UpdateRisk(next);
        if (persistRisk)
        {
            _ = PersistAsync(next);
        }
    }

    async Task PersistAsync(Risk next)
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

    async Task PersistTeamMembersAsync(Guid riskId, IReadOnlyList<Guid> memberIds)
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
