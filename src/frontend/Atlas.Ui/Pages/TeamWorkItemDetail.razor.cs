using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class TeamWorkItemDetail : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] AzureWorkItemService AzureWorkItemService { get; set; } = default!;

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? WorkItemId { get; set; }

    string _newNoteText = "";

    void BackToWorkItems() => Nav.NavigateTo($"/team/{MemberId}/work-items");
    void BackToMember() => Nav.NavigateTo($"/team/{MemberId}");

    TeamMember? Member
    {
        get
        {
            if (!Guid.TryParse(MemberId, out Guid id)) return null;
            return Cache.Team.FirstOrDefault(m => m.Id == id);
        }
    }

    AzureItem? Item =>
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
                Nav.NavigateTo("/team", replace: true);
        }
    }

    void OnProjectChange(ChangeEventArgs e)
    {
        if (Member is null || Item is null) return;
        string? nextProject = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString();
        UpdateWorkItem(new AzureItem
        {
            Id = Item.Id,
            Title = Item.Title,
            Status = Item.Status,
            AssignedTo = Item.AssignedTo,
            TicketUrl = Item.TicketUrl,
            ProjectId = nextProject,
            ChangedDateUtc = Item.ChangedDateUtc,
            TimeTaken = Item.TimeTaken,
            StartDateIso = Item.StartDateIso,
            CommitsUrl = Item.CommitsUrl,
            PrUrls = Item.PrUrls,
            LocalNotes = Item.LocalNotes
        });
    }

    void UpdateWorkItem(AzureItem next)
    {
        if (Member is null) return;
        Cache.UpdateTeamMember(new TeamMember
        {
            Id = Member.Id,
            Name = Member.Name,
            Role = Member.Role,
            StatusDot = Member.StatusDot,
            CurrentFocus = Member.CurrentFocus,
            Profile = Member.Profile,
            Signals = Member.Signals,
            Notes = Member.Notes,
            PinnedNoteIds = Member.PinnedNoteIds,
            ActivitySnapshot = Member.ActivitySnapshot,
            AzureItems = Member.AzureItems.Select(a => a.Id == next.Id ? next : a).ToList()
        });
    }

    async Task AddLocalNote()
    {
        if (Member is null || Item is null) return;
        string text = _newNoteText.Trim();
        if (string.IsNullOrEmpty(text)) return;
        if (!int.TryParse(Item.Id, out int workItemIdInt))
        {
            await Dialogs.AlertAsync("Unable to save work item note: invalid work item id.");
            return;
        }

        try
        {
            AtlasApiDTOsTeamMembersAzureWorkItemsAddAzureWorkItemLocalNoteResponse saved = await AzureWorkItemService.AddLocalNoteAsync(
                Member.Id, workItemIdInt, text);
            var note = new WorkItemNote
            {
                Id = saved.Id ?? Guid.NewGuid(),
                CreatedIso = saved.CreatedAt?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
                Text = text
            };
            List<WorkItemNote> nextNotes = new[] { note }.Concat(Item.LocalNotes).ToList();
            UpdateWorkItem(new AzureItem
            {
                Id = Item.Id,
                Title = Item.Title,
                Status = Item.Status,
                AssignedTo = Item.AssignedTo,
                TicketUrl = Item.TicketUrl,
                ProjectId = Item.ProjectId,
                ChangedDateUtc = Item.ChangedDateUtc,
                TimeTaken = Item.TimeTaken,
                StartDateIso = Item.StartDateIso,
                CommitsUrl = Item.CommitsUrl,
                PrUrls = Item.PrUrls,
                LocalNotes = nextNotes
            });
            _newNoteText = "";
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to save work item note right now. Please try again.\n\n{ex.Message}");
        }
    }

    void OnChanged() => InvokeAsync(() =>
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
