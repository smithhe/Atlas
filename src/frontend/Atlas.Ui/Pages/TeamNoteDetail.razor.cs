using Microsoft.AspNetCore.Components;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class TeamNoteDetail : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private TeamNoteService TeamNoteService { get; set; } = null!;

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? NoteId { get; set; }

    private static readonly NoteTag[] NoteTags = [NoteTag.Quick, NoteTag.Standup, NoteTag.Progress, NoteTag.Praise, NoteTag.Concern, NoteTag.Blocker];

    private bool _editing;
    private string _draftTitle = "", _draftText = "", _draftAdo = "", _draftPr = "";
    private NoteTag _draftTag = NoteTag.Quick;

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

    private TeamNote? Note
    {
        get
        {
            if (Member is null || !Guid.TryParse(NoteId, out Guid nid))
            {
                return null;
            }

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
            {
                Nav.NavigateTo("/team", replace: true);
            }
        }

        SyncDraftsFromNote();
    }

    private void SyncDraftsFromNote()
    {
        if (Note is null || _editing)
        {
            return;
        }

        _draftTitle = Note.Title ?? "";
        _draftTag = Note.Tag;
        _draftText = Note.Text;
        _draftAdo = Note.AdoWorkItemId ?? "";
        _draftPr = Note.PrUrl ?? "";
    }

    private void BackToNotes() => Nav.NavigateTo($"/team/{MemberId}/notes");
    private void BackToMember() => Nav.NavigateTo($"/team/{MemberId}");

    private void BeginEdit()
    {
        if (Note is null)
        {
            return;
        }

        _draftTitle = Note.Title ?? "";
        _draftTag = Note.Tag;
        _draftText = Note.Text;
        _draftAdo = Note.AdoWorkItemId ?? "";
        _draftPr = Note.PrUrl ?? "";
        _editing = true;
        SyncDraftTarget();
    }

    private void CancelEdit()
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

    private void SyncDraftTarget()
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

    private async Task SaveEdit()
    {
        if (Member is null || Note is null)
        {
            return;
        }

        var nextTitle = _draftTitle.Trim();
        var ado = _draftAdo.Trim();
        var pr = _draftPr.Trim();
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
            _editing = false;
            SyncDraftTarget();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to save note changes right now. Please try again.\n\n{ex.Message}");
        }
    }

    private void OnChanged() => InvokeAsync(() =>
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
