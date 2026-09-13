using Microsoft.AspNetCore.Components;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Projects : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private ProjectService ProjectService { get; set; } = null!;

    [Parameter] public string? ProjectId { get; set; }

    private bool _creating;
    private Guid? _autoEditId;

    private bool IsFocusMode => !string.IsNullOrEmpty(ProjectId);

    private Project? Selected
    {
        get
        {
            Guid? id = IsFocusMode && Guid.TryParse(ProjectId, out Guid focusId)
                ? focusId
                : Selection.SelectedProjectId;
            if (id is null)
            {
                return null;
            }

            return Cache.Projects.FirstOrDefault(p => p.Id == id);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        Cache.Changed += OnChangedAsync;
        Selection.Changed += OnChangedAsync;
        Ai.SetContext("Context: Projects",
        [
            new AiAction("project-summary", "Summarize project status"),
            new AiAction("identify-risks", "Identify risks"),
        ]);
        await Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        if (IsFocusMode && Guid.TryParse(ProjectId, out Guid id))
        {
            Selection.SelectProject(id);
            if (Cache.ProjectsReady && Cache.Projects.All(p => p.Id != id))
            {
                Nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
            }
        }
    }

    private async void OnChangedAsync()
    {
        try
        {
            await InvokeAsync(() =>
            {
                if (IsFocusMode && Guid.TryParse(ProjectId, out Guid id) && Cache.ProjectsReady && Cache.Projects.All(p => p.Id != id))
                {
                    Nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
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

    private void SelectFromList(Guid id) => Selection.SelectProject(id);

    private void ClearAutoEdit() => _autoEditId = null;

    private async Task OnProjectDeleted()
    {
        if (IsFocusMode)
        {
            Nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
        }

        if (_autoEditId is not null)
        {
            _autoEditId = null;
        }
    }

    private void GoFocus(Guid id) => Nav.NavigateTo($"/projects/{id}");

    private string CurrentSearch
    {
        get
        {
            var uri = Nav.Uri;
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

        Nav.NavigateTo($"/projects/{Selected.Id}{CurrentSearch}");
    }

    private void ExitFocus() => Nav.NavigateTo($"/projects{CurrentSearch}");

    private async Task HandleAddProject()
    {
        if (_creating)
        {
            return;
        }

        var requested = await Dialogs.PromptAsync("Project name", "New project");
        if (requested is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(requested) ? "New project" : requested.Trim();
        _creating = true;
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
            Project created = await ProjectService.CreateAsync(draft);
            Guid id = created.Id;
            _autoEditId = id;
            SelectFromList(id);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to create project right now. Please try again.");
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
