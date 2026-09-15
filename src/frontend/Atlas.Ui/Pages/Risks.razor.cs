using Microsoft.AspNetCore.Components;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class Risks : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private SelectionState _selection { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IAiStateService _ai { get; set; } = null!;
        [Inject] private IRiskService _riskService { get; set; } = null!;

        [Parameter] public string? RiskId { get; set; }

        private bool Creating { get; set; }
        private Guid? AutoEditId { get; set; }

        private string StatusFilter { get; set; } = "All";
        private string ProjectFilter { get; set; } = "";
        private string SeverityFilter { get; set; } = "All";

        private bool IsFocusMode => !string.IsNullOrEmpty(RiskId);
        private bool ShowDetail => IsFocusMode || this._selection.SelectedRiskId is not null;

        private IReadOnlyList<string> ProjectOptions =>
            this._cache.Projects.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

        private IReadOnlyList<Risk> Filtered => this._cache.Risks.Where(MatchesFilters).ToList();

        private Risk? Selected
        {
            get
            {
                Guid? id = IsFocusMode && Guid.TryParse(RiskId, out Guid focusId)
                    ? focusId
                    : this._selection.SelectedRiskId;
                if (id is null)
                {
                    return null;
                }

                return this._cache.Risks.FirstOrDefault(r => r.Id == id);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            this._cache.Changed += OnChangedAsync;
            this._selection.Changed += OnChangedAsync;
            this._ai.SetContext("Context: Risks",
            [
                new AiAction("summarize-impact", "Summarize impact"),
                new AiAction("suggest-mitigations", "Suggest mitigations"),
                new AiAction("why-matters", "Explain “why this matters”"),
            ]);
            await this._cache.EnsureHydratedAsync();
        }

        protected override void OnParametersSet()
        {
            if (IsFocusMode && Guid.TryParse(RiskId, out Guid id))
            {
                this._selection.SelectRisk(id);
                if (this._cache.RisksReady && this._cache.Risks.All(r => r.Id != id))
                {
                    this._nav.NavigateTo("/risks", replace: true);
                }
            }
        }

        private async void OnChangedAsync()
        {
            try
            {
                await InvokeAsync(() =>
                {
                    if (IsFocusMode && Guid.TryParse(RiskId, out Guid id) && this._cache.RisksReady && this._cache.Risks.All(r => r.Id != id))
                    {
                        this._nav.NavigateTo("/risks", replace: true);
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
            if (this.StatusFilter != "All" && r.Status.ToString() != this.StatusFilter)
            {
                return false;
            }

            if (this.SeverityFilter != "All" && r.Severity != this.SeverityFilter)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(this.ProjectFilter)
                && !(r.Project ?? "").Contains(this.ProjectFilter.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private void OnStatusFilterChange(ChangeEventArgs e) => this.StatusFilter = e.Value?.ToString() ?? "All";
        private void OnSeverityFilterChange(ChangeEventArgs e) => this.SeverityFilter = e.Value?.ToString() ?? "All";
        private void OnProjectFilterInput(ChangeEventArgs e) => this.ProjectFilter = e.Value?.ToString() ?? "";

        private void SelectFromList(Guid id) => this._selection.SelectRisk(id);

        private void CloseDetail() => this._selection.SelectRisk(null);

        private void ClearAutoEdit() => this.AutoEditId = null;

        private async Task OnRiskDeleted()
        {
            if (IsFocusMode)
            {
                this._nav.NavigateTo("/risks", replace: true);
            }

            if (this.AutoEditId is not null)
            {
                this.AutoEditId = null;
            }
        }

        private void GoFocus(Guid id) => this._nav.NavigateTo($"/risks/{id}");

        private void EnterFocus()
        {
            if (Selected is null)
            {
                return;
            }

            this._nav.NavigateTo($"/risks/{Selected.Id}");
        }

        private void ExitFocus() => this._nav.NavigateTo("/risks");

        private async Task HandleAddRisk()
        {
            if (this.Creating)
            {
                return;
            }

            this.Creating = true;
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
                Risk created = await this._riskService.CreateAsync(draft);
                Guid id = created.Id;
                this.AutoEditId = id;
                this._selection.SelectRisk(id);
            }
            catch (Exception)
            {
                await this._dialogs.AlertAsync("Unable to create risk right now. Please try again.");
            }
            finally
            {
                this.Creating = false;
            }
        }

        public void Dispose()
        {
            this._cache.Changed -= OnChangedAsync;
            this._selection.Changed -= OnChangedAsync;
        }
    }
}
