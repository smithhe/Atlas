using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class TeamWorkItemDetail : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private AzureWorkItemService AzureWorkItemService { get; set; } = null!;

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? WorkItemId { get; set; }

    private string _newNoteText = "";

    private void BackToWorkItems() => Nav.NavigateTo($"/team/{MemberId}/work-items");
    private void BackToMember() => Nav.NavigateTo($"/team/{MemberId}");

    private TeamMember? Member
    {
        get
        {
            if (!Guid.TryParse(MemberId, out Guid id))
            {
                return null;
            }

            return Cache.Team.FirstOrDefault(m => m.Id == id);
        }
    }

    private AzureItem? Item =>
        Member is null || string.IsNullOrEmpty(WorkItemId)
            ? null
            : Member.AzureItems.FirstOrDefault(a => a.Id == WorkItemId);

    protected override void OnInitialized()
    {
        Cache.Changed += OnChanged;
        Ai.SetContext("Context: Team Work Item Detail", [new AiAction("summarize-item", "Summarize this work item")]);
        _ = Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(MemberId) && !Guid.TryParse(MemberId, out _))
        {
            Nav.NavigateTo("/team", replace: true);
            return;
        }

        if (Guid.TryParse(MemberId, out Guid id))
        {
            Selection.SelectTeamMember(id);
            if (Cache.TeamReady && Member is null)
            {
                Nav.NavigateTo("/team", replace: true);
            }
        }
    }

    private void OnProjectChange(ChangeEventArgs e)
    {
        if (Member is null || Item is null)
        {
            return;
        }

        var nextProject = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString();
        AzureWorkItemService.SetProject(Member.Id, Item.Id, nextProject);
    }

    private async Task AddLocalNote()
    {
        if (Member is null || Item is null)
        {
            return;
        }

        var text = _newNoteText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (!int.TryParse(Item.Id, out var workItemIdInt))
        {
            await Dialogs.AlertAsync("Unable to save work item note: invalid work item id.");
            return;
        }

        try
        {
            WorkItemNote note = await AzureWorkItemService.AddLocalNoteAsync(Member.Id, workItemIdInt, text);
            _newNoteText = "";
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to save work item note right now. Please try again.\n\n{ex.Message}");
        }
    }

    private void OnChanged() => InvokeAsync(() =>
    {
        if (Guid.TryParse(MemberId, out Guid id) && Cache.TeamReady && Cache.Team.All(m => m.Id != id))
        {
            Nav.NavigateTo("/team", replace: true);
            return;
        }

        StateHasChanged();
    });

    public void Dispose() => Cache.Changed -= OnChanged;
}
