using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Shared
{
public partial class GlobalSearch : IDisposable
{
    [Inject] private IAppCacheService _cache { get; set; } = null!;
    [Inject] private SelectionState _selection { get; set; } = null!;
    [Inject] private NavigationManager _nav { get; set; } = null!;

    private sealed record SearchResult(string Id, string Kind, string Title, string Meta, string To, Guid EntityId);

    private static readonly string[] KindOrder = ["Task", "Risk", "Person", "Project"];
    private const int MaxResults = 12;

    private string Query { get; set; } = "";
    private bool Open { get; set; }
    private int Active { get; set; }
    private List<SearchResult> Results { get; set; } = [];

    private bool HasQuery => !string.IsNullOrWhiteSpace(this.Query);

    protected override void OnInitialized()
    {
        this._cache.Changed += OnCacheChangedAsync;
    }

    private async void OnCacheChangedAsync()
    {
        try
        {
            await InvokeAsync(() =>
            {
                Rebuild();
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void OnQueryInput(ChangeEventArgs e)
    {
        this.Query = e.Value?.ToString() ?? "";
        this.Open = true;
        this.Active = 0;
        Rebuild();
    }

    private void Rebuild()
    {
        var q = this.Query.Trim().ToLowerInvariant();
        if (q.Length == 0)
        {
            this.Results = [];
            return;
        }

        List<SearchResult> results = new();

        foreach (AtlasTask task in this._cache.Tasks)
        {
            var haystack = string.Join(' ', new[]
            {
                task.Title, task.Project, task.Risk, DisplayLabels.FormatTaskStatus(task.Status),
                task.Priority.ToString(), task.Notes
            }.Where(s => !string.IsNullOrEmpty(s)));
            if (!haystack.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new SearchResult(
                $"task:{task.Id}",
                "Task",
                task.Title,
                string.Join(" · ", new[] { DisplayLabels.FormatTaskStatus(task.Status), task.Priority.ToString(), task.Project }.Where(s => !string.IsNullOrEmpty(s))),
                $"/tasks/{task.Id}",
                task.Id));
        }

        foreach (Risk risk in this._cache.Risks)
        {
            var haystack = string.Join(' ', new[]
            {
                risk.Title, risk.Project, risk.Description, risk.Evidence, risk.Status.ToString(), risk.Severity
            }.Where(s => !string.IsNullOrEmpty(s)));
            if (!haystack.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new SearchResult(
                $"risk:{risk.Id}",
                "Risk",
                risk.Title,
                string.Join(" · ", new[] { risk.Status.ToString(), risk.Severity, risk.Project }.Where(s => !string.IsNullOrEmpty(s))),
                $"/risks/{risk.Id}",
                risk.Id));
        }

        foreach (TeamMember member in this._cache.Team)
        {
            var haystack = string.Join(' ', new[] { member.Name, member.Role, member.CurrentFocus }.Where(s => !string.IsNullOrEmpty(s)));
            if (!haystack.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new SearchResult(
                $"person:{member.Id}",
                "Person",
                member.Name,
                string.Join(" · ", new[] { member.Role, member.CurrentFocus }.Where(s => !string.IsNullOrEmpty(s))),
                $"/team/{member.Id}",
                member.Id));
        }

        foreach (Project project in this._cache.Projects)
        {
            var haystack = string.Join(' ', new[]
            {
                project.Name, project.Summary, project.Description, project.Status?.ToString()
            }.Concat(project.Tags).Where(s => !string.IsNullOrEmpty(s)));
            if (!haystack.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new SearchResult(
                $"project:{project.Id}",
                "Project",
                project.Name,
                string.Join(" · ", new[] { project.Status?.ToString(), project.Summary }.Where(s => !string.IsNullOrEmpty(s))),
                $"/projects/{project.Id}",
                project.Id));
        }

        results.Sort((a, b) =>
        {
            var kindDiff = Array.IndexOf(KindOrder, a.Kind) - Array.IndexOf(KindOrder, b.Kind);
            return kindDiff != 0 ? kindDiff : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });

        this.Results = results.Take(MaxResults).ToList();
        if (this.Active >= this.Results.Count)
        {
            this.Active = 0;
        }
    }

    private void SelectResult(SearchResult result)
    {
        if (result.Kind == "Task")
        {
            this._selection.SelectTask(result.EntityId);
        }

        if (result.Kind == "Risk")
        {
            this._selection.SelectRisk(result.EntityId);
        }

        if (result.Kind == "Person")
        {
            this._selection.SelectTeamMember(result.EntityId);
        }

        if (result.Kind == "Project")
        {
            this._selection.SelectProject(result.EntityId);
        }

        this._nav.NavigateTo(result.To);
        this.Query = "";
        this.Open = false;
        this.Active = 0;
        this.Results = [];
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            if (this.Open && HasQuery)
            {
                this.Open = false;
                this.Active = 0;
            }
            else if (HasQuery)
            {
                this.Query = "";
                this.Results = [];
            }
            return;
        }

        if (!this.Open || !HasQuery || this.Results.Count == 0)
        {
            return;
        }

        if (e.Key == "ArrowDown")
        {
            this.Active = (this.Active + 1) % this.Results.Count;
        }
        else if (e.Key == "ArrowUp")
        {
            this.Active = (this.Active - 1 + this.Results.Count) % this.Results.Count;
        }
        else if (e.Key == "Enter")
        {
            SelectResult(this.Results[this.Active]);
        }
    }

    private void OnRootKeyDown(KeyboardEventArgs e) { }

    public void Dispose() => this._cache.Changed -= OnCacheChangedAsync;
}
}
