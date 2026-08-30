using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Team : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private TeamMemberService TeamMemberService { get; set; } = null!;

    public enum MemberTab { Overview, Notes, WorkItems, Risks, Growth }

    [Parameter] public string? MemberId { get; set; }

    private MemberTab _localTab = MemberTab.Overview;

    private bool IsFocusMode => !string.IsNullOrEmpty(MemberId);

    private Guid? MemberIdParsed => Guid.TryParse(MemberId, out Guid id) ? id : null;

    private MemberTab ActiveTab => IsFocusMode ? GetRouteTab() : _localTab;

    private TeamMember? Member
    {
        get
        {
            if (MemberIdParsed is not { } id)
            {
                return null;
            }

            return Cache.Team.FirstOrDefault(m => m.Id == id);
        }
    }

    private TeamMember? Selected
    {
        get
        {
            if (IsFocusMode)
            {
                return Member;
            }

            if (Selection.SelectedTeamMemberId is { } sid)
            {
                return Cache.Team.FirstOrDefault(m => m.Id == sid);
            }

            return null;
        }
    }

    protected override void OnInitialized()
    {
        Cache.Changed += OnChanged;
        Nav.LocationChanged += OnLocationChanged;
        Ai.SetContext("Context: Team",
        [
            new AiAction("summarize-patterns", "Summarize patterns (frequent blockers)"),
            new AiAction("growth-areas", "Highlight growth areas"),
            new AiAction("cite-notes", "Cite specific notes"),
        ]);
        _ = Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        // Non-GUID focus ids must bounce to list (not stuck on “Redirecting…”).
        if (!string.IsNullOrEmpty(MemberId) && MemberIdParsed is null)
        {
            Nav.NavigateTo("/team", replace: true);
            return;
        }

        if (MemberIdParsed is { } id)
        {
            Selection.SelectTeamMember(id);
            if (Cache.TeamReady && Member is null)
            {
                Nav.NavigateTo("/team", replace: true);
            }
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(StateHasChanged);

    private MemberTab GetRouteTab()
    {
        var path = new Uri(Nav.Uri).AbsolutePath;
        if (path.Contains("/notes", StringComparison.OrdinalIgnoreCase))
        {
            return MemberTab.Notes;
        }

        if (path.Contains("/work-items", StringComparison.OrdinalIgnoreCase))
        {
            return MemberTab.WorkItems;
        }

        if (path.Contains("/risks", StringComparison.OrdinalIgnoreCase))
        {
            return MemberTab.Risks;
        }

        if (path.Contains("/growth", StringComparison.OrdinalIgnoreCase))
        {
            return MemberTab.Growth;
        }

        return MemberTab.Overview;
    }

    private static string TabLabel(MemberTab tab) => tab switch
    {
        MemberTab.Notes => "Notes",
        MemberTab.WorkItems => "Work Items",
        MemberTab.Risks => "Risks",
        MemberTab.Growth => "Growth",
        _ => "Overview"
    };

    private string MemberTabPath(Guid memberId, MemberTab tab) => tab switch
    {
        MemberTab.Notes => $"/team/{memberId}/notes",
        MemberTab.WorkItems => $"/team/{memberId}/work-items",
        MemberTab.Risks => $"/team/{memberId}/risks",
        MemberTab.Growth => $"/team/{memberId}/growth",
        _ => $"/team/{memberId}"
    };

    private void SelectFromList(Guid id) => Selection.SelectTeamMember(id);

    private void GoFocus(Guid id) => Nav.NavigateTo(MemberTabPath(id, _localTab));

    private void EnterFocus()
    {
        if (Selected is null)
        {
            return;
        }

        Nav.NavigateTo(MemberTabPath(Selected.Id, ActiveTab));
    }

    private void ExitFocus()
    {
        _localTab = GetRouteTab();
        Nav.NavigateTo("/team");
    }

    private void GoNotes()
    {
        if (Selected is null)
        {
            return;
        }

        if (IsFocusMode)
        {
            Nav.NavigateTo($"/team/{Selected.Id}/notes");
        }
        else
        {
            _localTab = MemberTab.Notes;
        }
    }

    private void GoWorkItem(string workItemId)
    {
        if (Selected is null)
        {
            return;
        }

        Nav.NavigateTo($"/team/{Selected.Id}/work-items/{workItemId}");
    }

    private async Task HandleMemberUpdate(TeamMember next)
    {
        TeamMember? previous = Cache.Team.FirstOrDefault(m => m.Id == next.Id);
        if (previous is null)
        {
            return;
        }

        try
        {
            await TeamMemberService.UpdateAsync(previous, next);
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to save team member changes right now. Please try again.\n\n{ex.Message}");
        }
    }

    private void OnChanged() => InvokeAsync(() =>
    {
        if (MemberIdParsed is { } id && Cache.TeamReady && Cache.Team.All(m => m.Id != id))
        {
            Nav.NavigateTo("/team", replace: true);
            return;
        }

        StateHasChanged();
    });

    public void Dispose()
    {
        Cache.Changed -= OnChanged;
        Nav.LocationChanged -= OnLocationChanged;
    }
}
