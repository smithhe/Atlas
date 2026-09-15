using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class Team : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private SelectionState _selection { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IAiStateService _ai { get; set; } = null!;
        [Inject] private ITeamMemberService _teamMemberService { get; set; } = null!;

        public enum MemberTab { Overview, Notes, WorkItems, Risks, Growth }

        [Parameter] public string? MemberId { get; set; }

        private MemberTab LocalTab { get; set; } = MemberTab.Overview;

        private bool IsFocusMode => !string.IsNullOrEmpty(MemberId);

        private Guid? MemberIdParsed => Guid.TryParse(MemberId, out Guid id) ? id : null;

        private MemberTab ActiveTab => IsFocusMode ? GetRouteTab() : this.LocalTab;

        private TeamMember? Member
        {
            get
            {
                if (MemberIdParsed is not { } id)
                {
                    return null;
                }

                return this._cache.Team.FirstOrDefault(m => m.Id == id);
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

                if (this._selection.SelectedTeamMemberId is { } sid)
                {
                    return this._cache.Team.FirstOrDefault(m => m.Id == sid);
                }

                return null;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            this._cache.Changed += OnChangedAsync;
            this._nav.LocationChanged += OnLocationChangedAsync;
            this._ai.SetContext("Context: Team",
            [
                new AiAction("summarize-patterns", "Summarize patterns (frequent blockers)"),
                new AiAction("growth-areas", "Highlight growth areas"),
                new AiAction("cite-notes", "Cite specific notes"),
            ]);
            await this._cache.EnsureHydratedAsync();
        }

        protected override void OnParametersSet()
        {
            // Non-GUID focus ids must bounce to list (not stuck on “Redirecting…”).
            if (!string.IsNullOrEmpty(MemberId) && MemberIdParsed is null)
            {
                this._nav.NavigateTo("/team", replace: true);
                return;
            }

            if (MemberIdParsed is { } id)
            {
                this._selection.SelectTeamMember(id);
                if (this._cache.TeamReady && Member is null)
                {
                    this._nav.NavigateTo("/team", replace: true);
                }
            }
        }

        private async void OnLocationChangedAsync(object? sender, LocationChangedEventArgs e)
        {
            try
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        }

        private MemberTab GetRouteTab()
        {
            var path = new Uri(this._nav.Uri).AbsolutePath;
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

        private void SelectFromList(Guid id) => this._selection.SelectTeamMember(id);

        private void GoFocus(Guid id) => this._nav.NavigateTo(MemberTabPath(id, this.LocalTab));

        private void EnterFocus()
        {
            if (Selected is null)
            {
                return;
            }

            this._nav.NavigateTo(MemberTabPath(Selected.Id, ActiveTab));
        }

        private void ExitFocus()
        {
            this.LocalTab = GetRouteTab();
            this._nav.NavigateTo("/team");
        }

        private void GoNotes()
        {
            if (Selected is null)
            {
                return;
            }

            if (IsFocusMode)
            {
                this._nav.NavigateTo($"/team/{Selected.Id}/notes");
            }
            else
            {
                this.LocalTab = MemberTab.Notes;
            }
        }

        private void GoWorkItem(string workItemId)
        {
            if (Selected is null)
            {
                return;
            }

            this._nav.NavigateTo($"/team/{Selected.Id}/work-items/{workItemId}");
        }

        private async Task HandleMemberUpdate(TeamMember next)
        {
            TeamMember? previous = this._cache.Team.FirstOrDefault(m => m.Id == next.Id);
            if (previous is null)
            {
                return;
            }

            try
            {
                await this._teamMemberService.UpdateAsync(previous, next);
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync($"Unable to save team member changes right now. Please try again.\n\n{ex.Message}");
            }
        }

        private async void OnChangedAsync()
        {
            try
            {
                await InvokeAsync(() =>
                {
                    if (MemberIdParsed is { } id && this._cache.TeamReady && this._cache.Team.All(m => m.Id != id))
                    {
                        this._nav.NavigateTo("/team", replace: true);
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

        public void Dispose()
        {
            this._cache.Changed -= OnChangedAsync;
            this._nav.LocationChanged -= OnLocationChangedAsync;
        }
    }
}
