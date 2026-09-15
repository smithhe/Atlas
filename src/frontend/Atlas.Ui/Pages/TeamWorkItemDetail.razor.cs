using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class TeamWorkItemDetail : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private SelectionState _selection { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IAiStateService _ai { get; set; } = null!;
        [Inject] private IAzureWorkItemService _azureWorkItemService { get; set; } = null!;

        [Parameter] public string? MemberId { get; set; }
        [Parameter] public string? WorkItemId { get; set; }

        private string NewNoteText { get; set; } = "";

        private void BackToWorkItems() => this._nav.NavigateTo($"/team/{MemberId}/work-items");
        private void BackToMember() => this._nav.NavigateTo($"/team/{MemberId}");

        private TeamMember? Member
        {
            get
            {
                if (!Guid.TryParse(MemberId, out Guid id))
                {
                    return null;
                }

                return this._cache.Team.FirstOrDefault(m => m.Id == id);
            }
        }

        private AzureItem? Item =>
            Member is null || string.IsNullOrEmpty(WorkItemId)
                ? null
                : Member.AzureItems.FirstOrDefault(a => a.Id == WorkItemId);

        protected override async Task OnInitializedAsync()
        {
            this._cache.Changed += OnChangedAsync;
            this._ai.SetContext("Context: Team Work Item Detail", [new AiAction("summarize-item", "Summarize this work item")]);
            await this._cache.EnsureHydratedAsync();
        }

        protected override void OnParametersSet()
        {
            if (!string.IsNullOrEmpty(MemberId) && !Guid.TryParse(MemberId, out _))
            {
                this._nav.NavigateTo("/team", replace: true);
                return;
            }

            if (Guid.TryParse(MemberId, out Guid id))
            {
                this._selection.SelectTeamMember(id);
                if (this._cache.TeamReady && Member is null)
                {
                    this._nav.NavigateTo("/team", replace: true);
                }
            }
        }

        private async Task OnProjectChange(ChangeEventArgs e)
        {
            if (Member is null || Item is null)
            {
                return;
            }

            var raw = e.Value?.ToString();
            Guid? nextProject = null;
            if (!string.IsNullOrEmpty(raw) && Guid.TryParse(raw, out Guid parsed))
            {
                nextProject = parsed;
            }
            try
            {
                await this._azureWorkItemService.SetProjectAsync(Member.Id, Item.Id, nextProject);
            }
            catch (Exception)
            {
                await this._dialogs.AlertAsync("Unable to save work item project right now. Please try again.");
            }
        }

        private async Task AddLocalNote()
        {
            if (Member is null || Item is null)
            {
                return;
            }

            var text = this.NewNoteText.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!int.TryParse(Item.Id, out var workItemIdInt))
            {
                await this._dialogs.AlertAsync("Unable to save work item note: invalid work item id.");
                return;
            }

            try
            {
                WorkItemNote note = await this._azureWorkItemService.AddLocalNoteAsync(Member.Id, workItemIdInt, text);
                this.NewNoteText = "";
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync($"Unable to save work item note right now. Please try again.\n\n{ex.Message}");
            }
        }

        private async void OnChangedAsync()
        {
            try
            {
                await InvokeAsync(() =>
                {
                    if (Guid.TryParse(MemberId, out Guid id) && this._cache.TeamReady && this._cache.Team.All(m => m.Id != id))
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

        public void Dispose() => this._cache.Changed -= OnChangedAsync;
    }
}
