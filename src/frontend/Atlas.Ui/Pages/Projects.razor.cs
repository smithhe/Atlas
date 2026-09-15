using Microsoft.AspNetCore.Components;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class Projects : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private SelectionState _selection { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IAiStateService _ai { get; set; } = null!;
        [Inject] private IProjectService _projectService { get; set; } = null!;

        [Parameter] public string? ProjectId { get; set; }

        private bool Creating { get; set; }
        private Guid? AutoEditId { get; set; }

        private bool IsFocusMode => !string.IsNullOrEmpty(ProjectId);

        private Project? Selected
        {
            get
            {
                Guid? id = IsFocusMode && Guid.TryParse(ProjectId, out Guid focusId)
                    ? focusId
                    : this._selection.SelectedProjectId;
                if (id is null)
                {
                    return null;
                }

                return this._cache.Projects.FirstOrDefault(p => p.Id == id);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            this._cache.Changed += OnChangedAsync;
            this._selection.Changed += OnChangedAsync;
            this._ai.SetContext("Context: Projects",
            [
                new AiAction("project-summary", "Summarize project status"),
                new AiAction("identify-risks", "Identify risks"),
            ]);
            await this._cache.EnsureHydratedAsync();
        }

        protected override void OnParametersSet()
        {
            if (IsFocusMode && Guid.TryParse(ProjectId, out Guid id))
            {
                this._selection.SelectProject(id);
                if (this._cache.ProjectsReady && this._cache.Projects.All(p => p.Id != id))
                {
                    this._nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
                }
            }
        }

        private async void OnChangedAsync()
        {
            try
            {
                await InvokeAsync(() =>
                {
                    if (IsFocusMode && Guid.TryParse(ProjectId, out Guid id) && this._cache.ProjectsReady && this._cache.Projects.All(p => p.Id != id))
                    {
                        this._nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
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

        private void SelectFromList(Guid id) => this._selection.SelectProject(id);

        private void ClearAutoEdit() => this.AutoEditId = null;

        private async Task OnProjectDeleted()
        {
            if (IsFocusMode)
            {
                this._nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
            }

            if (this.AutoEditId is not null)
            {
                this.AutoEditId = null;
            }
        }

        private void GoFocus(Guid id) => this._nav.NavigateTo($"/projects/{id}");

        private string CurrentSearch
        {
            get
            {
                var uri = this._nav.Uri;
                var qIndex = uri.IndexOf('?', StringComparison.Ordinal);
                if (qIndex < 0)
                {
                    return "";
                }

                var query = uri[qIndex..];
                var hashIndex = query.IndexOf('#', StringComparison.Ordinal);
                return hashIndex >= 0 ? query[..hashIndex] : query;
            }
        }

        private void EnterFocus()
        {
            if (Selected is null)
            {
                return;
            }

            this._nav.NavigateTo($"/projects/{Selected.Id}{CurrentSearch}");
        }

        private void ExitFocus() => this._nav.NavigateTo($"/projects{CurrentSearch}");

        private async Task HandleAddProject()
        {
            if (this.Creating)
            {
                return;
            }

            var requested = await this._dialogs.PromptAsync("Project name", "New project");
            if (requested is null)
            {
                return;
            }

            var name = string.IsNullOrWhiteSpace(requested) ? "New project" : requested.Trim();
            this.Creating = true;
            try
            {
                var draft = new Project
                {
                    Name = name,
                    Summary = "New project summary",
                    Description = "",
                    Status = ProjectStatus.Active,
                    Health = HealthSignal.Green,
                    Tags = Array.Empty<string>(),
                    Links = Array.Empty<ProjectLink>(),
                    LinkedTaskIds = Array.Empty<Guid>(),
                    LinkedRiskIds = Array.Empty<Guid>(),
                    TeamMemberIds = Array.Empty<Guid>(),
                    LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o")
                };
                Project created = await this._projectService.CreateAsync(draft);
                Guid id = created.Id;
                this.AutoEditId = id;
                SelectFromList(id);
            }
            catch (Exception)
            {
                await this._dialogs.AlertAsync("Unable to create project right now. Please try again.");
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
