using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Shared;

public partial class GlobalSearch : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;

    sealed record SearchResult(string Id, string Kind, string Title, string Meta, string To, Guid EntityId);

    static readonly string[] KindOrder = ["Task", "Risk", "Person", "Project"];
    const int MaxResults = 12;

    string _query = "";
    bool _open;
    int _active;
    List<SearchResult> _results = [];

    bool HasQuery => !string.IsNullOrWhiteSpace(_query);

    protected override void OnInitialized()
    {
        Cache.Changed += OnCacheChanged;
    }

    void OnCacheChanged() => InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    void OnQueryInput(ChangeEventArgs e)
    {
        _query = e.Value?.ToString() ?? "";
        _open = true;
        _active = 0;
        Rebuild();
    }

    void Rebuild()
    {
        string q = _query.Trim().ToLowerInvariant();
        if (q.Length == 0)
        {
            _results = [];
            return;
        }

        List<SearchResult> results = new();

        foreach (AtlasTask task in Cache.Tasks)
        {
            string haystack = string.Join(' ', new[]
            {
                task.Title, task.Project, task.Risk, DisplayLabels.FormatTaskStatus(task.Status),
                task.Priority.ToString(), task.Notes
            }.Where(s => !string.IsNullOrEmpty(s)));
            if (!haystack.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
            results.Add(new SearchResult(
                $"task:{task.Id}",
                "Task",
                task.Title,
                string.Join(" · ", new[] { DisplayLabels.FormatTaskStatus(task.Status), task.Priority.ToString(), task.Project }.Where(s => !string.IsNullOrEmpty(s))),
                $"/tasks/{task.Id}",
                task.Id));
        }

        foreach (Risk risk in Cache.Risks)
        {
            string haystack = string.Join(' ', new[]
            {
                risk.Title, risk.Project, risk.Description, risk.Evidence, risk.Status.ToString(), risk.Severity
            }.Where(s => !string.IsNullOrEmpty(s)));
            if (!haystack.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
            results.Add(new SearchResult(
                $"risk:{risk.Id}",
                "Risk",
                risk.Title,
                string.Join(" · ", new[] { risk.Status.ToString(), risk.Severity, risk.Project }.Where(s => !string.IsNullOrEmpty(s))),
                $"/risks/{risk.Id}",
                risk.Id));
        }

        foreach (TeamMember member in Cache.Team)
        {
            string haystack = string.Join(' ', new[] { member.Name, member.Role, member.CurrentFocus }.Where(s => !string.IsNullOrEmpty(s)));
            if (!haystack.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
            results.Add(new SearchResult(
                $"person:{member.Id}",
                "Person",
                member.Name,
                string.Join(" · ", new[] { member.Role, member.CurrentFocus }.Where(s => !string.IsNullOrEmpty(s))),
                $"/team/{member.Id}",
                member.Id));
        }

        foreach (Project project in Cache.Projects)
        {
            string haystack = string.Join(' ', new[]
            {
                project.Name, project.Summary, project.Description, project.Status?.ToString()
            }.Concat(project.Tags).Where(s => !string.IsNullOrEmpty(s)));
            if (!haystack.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
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
            int kindDiff = Array.IndexOf(KindOrder, a.Kind) - Array.IndexOf(KindOrder, b.Kind);
            return kindDiff != 0 ? kindDiff : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });

        _results = results.Take(MaxResults).ToList();
        if (_active >= _results.Count) _active = 0;
    }

    void SelectResult(SearchResult result)
    {
        if (result.Kind == "Task") Selection.SelectTask(result.EntityId);
        if (result.Kind == "Risk") Selection.SelectRisk(result.EntityId);
        if (result.Kind == "Person") Selection.SelectTeamMember(result.EntityId);
        if (result.Kind == "Project") Selection.SelectProject(result.EntityId);
        Nav.NavigateTo(result.To);
        _query = "";
        _open = false;
        _active = 0;
        _results = [];
    }

    void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            if (_open && HasQuery)
            {
                _open = false;
                _active = 0;
            }
            else if (HasQuery)
            {
                _query = "";
                _results = [];
            }
            return;
        }

        if (!_open || !HasQuery || _results.Count == 0) return;

        if (e.Key == "ArrowDown")
        {
            _active = (_active + 1) % _results.Count;
        }
        else if (e.Key == "ArrowUp")
        {
            _active = (_active - 1 + _results.Count) % _results.Count;
        }
        else if (e.Key == "Enter")
        {
            SelectResult(_results[_active]);
        }
    }

    void OnRootKeyDown(KeyboardEventArgs e) { }

    public void Dispose() => Cache.Changed -= OnCacheChanged;
}
