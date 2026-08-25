using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class TeamNoteDetail : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] TeamNoteService TeamNoteService { get; set; } = default!;

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? NoteId { get; set; }

    static readonly NoteTag[] NoteTags = [NoteTag.Quick, NoteTag.Standup, NoteTag.Progress, NoteTag.Praise, NoteTag.Concern, NoteTag.Blocker];

    bool _editing;
    string _draftTitle = "", _draftText = "", _draftAdo = "", _draftPr = "";
    NoteTag _draftTag = NoteTag.Quick;

    TeamMember? Member
    {
        get
        {
            if (!Guid.TryParse(MemberId, out Guid id)) return null;
            return Cache.Team.FirstOrDefault(m => m.Id == id);
        }
    }

    TeamNote? Note
    {
        get
        {
            if (Member is null || !Guid.TryParse(NoteId, out Guid nid)) return null;
            return Member.Notes.FirstOrDefault(n => n.Id == nid);
        }
    }

    protected override void OnInitialized()
    {
        Cache.Changed += OnChanged;
        Ai.SetContext("Context: Team Note Detail", [new AiAction("summarize-note", "Summarize this note")]);
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

        SyncDraftsFromNote();
    }

    void SyncDraftsFromNote()
    {
        if (Note is null || _editing) return;
        _draftTitle = Note.Title ?? "";
        _draftTag = Note.Tag;
        _draftText = Note.Text;
        _draftAdo = Note.AdoWorkItemId ?? "";
        _draftPr = Note.PrUrl ?? "";
    }

    void BackToNotes() => Nav.NavigateTo($"/team/{MemberId}/notes");
    void BackToMember() => Nav.NavigateTo($"/team/{MemberId}");

    void BeginEdit()
    {
        if (Note is null) return;
        _draftTitle = Note.Title ?? "";
        _draftTag = Note.Tag;
        _draftText = Note.Text;
        _draftAdo = Note.AdoWorkItemId ?? "";
        _draftPr = Note.PrUrl ?? "";
        _editing = true;
        SyncDraftTarget();
    }

    void CancelEdit()
    {
        SyncDraftsFromNote();
        _editing = false;
        if (Note is not null)
        {
            _draftTitle = Note.Title ?? "";
            _draftTag = Note.Tag;
            _draftText = Note.Text;
            _draftAdo = Note.AdoWorkItemId ?? "";
            _draftPr = Note.PrUrl ?? "";
        }
        SyncDraftTarget();
    }

    void SyncDraftTarget()
    {
        if (!_editing)
        {
            Ai.RegisterDraftTarget(null);
            return;
        }

        Ai.RegisterDraftTarget(new AiDraftTarget
        {
            Label = "note body",
            Insert = text =>
            {
                _draftText = string.IsNullOrWhiteSpace(_draftText)
                    ? text
                    : $"{_draftText.TrimEnd()}\n\n{text}";
                InvokeAsync(StateHasChanged);
            },
        });
    }

    async Task SaveEdit()
    {
        if (Member is null || Note is null) return;
        string nextTitle = _draftTitle.Trim();
        string ado = _draftAdo.Trim();
        string pr = _draftPr.Trim();
        var updated = new TeamNote
        {
            Id = Note.Id,
            CreatedIso = Note.CreatedIso,
            LastModifiedIso = DateTimeOffset.UtcNow.ToString("o"),
            Tag = _draftTag,
            Title = string.IsNullOrEmpty(nextTitle) ? null : nextTitle,
            Text = _draftText,
            AdoWorkItemId = string.IsNullOrEmpty(ado) ? null : ado,
            PrUrl = string.IsNullOrEmpty(pr) ? null : pr
        };

        try
        {
            await TeamNoteService.UpdateAsync(Member.Id, updated);
            List<TeamNote> nextNotes = Member.Notes.Select(n => n.Id == Note.Id ? updated : n).ToList();
            Cache.UpdateTeamMember(new TeamMember
            {
                Id = Member.Id,
                Name = Member.Name,
                Role = Member.Role,
                StatusDot = Member.StatusDot,
                CurrentFocus = Member.CurrentFocus,
                Profile = Member.Profile,
                Signals = Member.Signals,
                Notes = nextNotes,
                PinnedNoteIds = Member.PinnedNoteIds,
                ActivitySnapshot = Member.ActivitySnapshot,
                AzureItems = Member.AzureItems
            });
            _editing = false;
            SyncDraftTarget();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to save note changes right now. Please try again.\n\n{ex.Message}");
        }
    }

    void OnChanged() => InvokeAsync(() =>
    {
        if (Guid.TryParse(MemberId, out Guid id) && Cache.TeamReady && Cache.Team.All(m => m.Id != id))
        {
            Nav.NavigateTo("/team", replace: true);
            return;
        }

        SyncDraftsFromNote();
        StateHasChanged();
    });

    public void Dispose()
    {
        Cache.Changed -= OnChanged;
        Ai.RegisterDraftTarget(null);
    }
}
