using Microsoft.AspNetCore.Components;
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

    private bool _creating;
    private Guid? _autoEditId;

    private string _statusFilter = "All";
    private string _projectFilter = "";
    private string _severityFilter = "All";

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

    protected override async Task OnInitializedAsync()
    {
        Cache.Changed += OnChangedAsync;
        Selection.Changed += OnChangedAsync;
        Ai.SetContext("Context: Risks",
        [
            new AiAction("summarize-impact", "Summarize impact"),
            new AiAction("suggest-mitigations", "Suggest mitigations"),
            new AiAction("why-matters", "Explain “why this matters”"),
        ]);
        await Cache.EnsureHydratedAsync();
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
    }

    private async void OnChangedAsync()
    {
        try
        {
            await InvokeAsync(() =>
            {
                if (IsFocusMode && Guid.TryParse(RiskId, out Guid id) && Cache.RisksReady && Cache.Risks.All(r => r.Id != id))
                {
                    Nav.NavigateTo("/risks", replace: true);
                    return;
                }

                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
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

    private void SelectFromList(Guid id) => Selection.SelectRisk(id);

    private void CloseDetail() => Selection.SelectRisk(null);

    private void ClearAutoEdit() => _autoEditId = null;

    private async Task OnRiskDeleted()
    {
        if (IsFocusMode)
        {
            Nav.NavigateTo("/risks", replace: true);
        }

        if (_autoEditId is not null)
        {
            _autoEditId = null;
        }
    }

    private void GoFocus(Guid id) => Nav.NavigateTo($"/risks/{id}");

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
            Selection.SelectRisk(id);
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

    public void Dispose()
    {
        Cache.Changed -= OnChangedAsync;
        Selection.Changed -= OnChangedAsync;
    }
}
