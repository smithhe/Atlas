using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberNotesTab : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private TeamNoteService TeamNoteService { get; set; } = null!;

    [Parameter, EditorRequired] public TeamMember Member { get; set; } = null!;

    private static readonly NoteTag[] NoteTags = [NoteTag.Quick, NoteTag.Standup, NoteTag.Progress, NoteTag.Praise, NoteTag.Concern, NoteTag.Blocker];

    private string _query = "";
    private string _tagFilter = "All";
    private string _sortBy = "Newest";
    private string _quickFilter = "All";
    private Guid? _expandedNoteId;
    private Guid? _selectedNoteId;
    private bool _isNewOpen;
    private bool _isEditOpen;
    private string _editTab = "Write";
    private NoteTag _newTag = NoteTag.Quick;
    private string _newTitle = "", _newText = "", _newAdo = "", _newPr = "";
    private string _draftText = "", _draftAdo = "", _draftPr = "";

    private TeamNote? SelectedNote =>
        _selectedNoteId is { } id ? Member.Notes.FirstOrDefault(n => n.Id == id) : null;

    private List<TeamNote> FilteredSorted
    {
        get
        {
            var q = _query.Trim().ToLowerInvariant();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var weekMs = TimeSpan.FromDays(7);

            bool Matches(TeamNote n)
            {
                if (_tagFilter != "All" && !string.Equals(n.Tag.ToString(), _tagFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (_quickFilter == "ThisWeek")
                {
                    if (!DateTimeOffset.TryParse(n.CreatedIso, out DateTimeOffset created) || now - created.ToUniversalTime() > weekMs)
                    {
                        return false;
                    }
                }
                else if (_quickFilter == "ActionItems" && !IsActionItemNote(n))
                {
                    return false;
                }
                else if (_quickFilter == "OneOnOne" && !IsOneOnOneNote(n))
                {
                    return false;
                }
                else if (_quickFilter == "Risks" && !IsRiskNote(n))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(q))
                {
                    return true;
                }

                var hay = string.Join(' ', n.Tag, n.Title ?? "", DisplayLabels.GetDerivedTitle(n), n.Text, n.AdoWorkItemId ?? "", n.PrUrl ?? "").ToLowerInvariant();
                return hay.Contains(q);
            }

            var list = Member.Notes.Where(Matches).ToList();
            list.Sort((a, b) =>
            {
                var at = DateTimeOffset.TryParse(a.CreatedIso, out DateTimeOffset ad) ? ad.ToUnixTimeMilliseconds() : 0;
                var bt = DateTimeOffset.TryParse(b.CreatedIso, out DateTimeOffset bd) ? bd.ToUnixTimeMilliseconds() : 0;
                return _sortBy == "Newest" ? bt.CompareTo(at) : at.CompareTo(bt);
            });
            return list;
        }
    }

    private static bool IsActionItemNote(TeamNote note)
    {
        var t = note.Text.ToLowerInvariant();
        return t.Contains("- [ ]") || t.Contains("action item") || t.Contains("next:") || t.Contains("todo");
    }

    private static bool IsOneOnOneNote(TeamNote note)
    {
        var t = note.Text.ToLowerInvariant();
        return t.Contains("1:1") || t.Contains("one-on-one") || t.Contains("one on one");
    }

    private static bool IsRiskNote(TeamNote note)
    {
        if (note.Tag is NoteTag.Concern or NoteTag.Blocker)
        {
            return true;
        }

        var t = note.Text.ToLowerInvariant();
        return t.Contains("risk") || t.Contains("mitigation") || t.Contains("watchout") || t.Contains("watch-outs");
    }

    private void ToggleExpand(Guid id) => _expandedNoteId = _expandedNoteId == id ? null : id;

    private void ToggleExpandClick(MouseEventArgs _, Guid id) => ToggleExpand(id);

    private void OnRowKey(KeyboardEventArgs e, Guid id)
    {
        if (e.Key is "Enter" or " ")
        {
            ToggleExpand(id);
        }
    }

    private void OpenNoteModal(MouseEventArgs _, Guid id)
    {
        _selectedNoteId = id;
        _isEditOpen = false;
        _draftText = "";
        _editTab = "Write";
        SyncDraftTarget();
    }

    private void CloseNoteModal()
    {
        _selectedNoteId = null;
        _isEditOpen = false;
        _draftText = _draftAdo = _draftPr = "";
        _editTab = "Write";
        SyncDraftTarget();
    }

    private void BeginNoteEdit()
    {
        if (SelectedNote is null)
        {
            return;
        }

        _isEditOpen = true;
        _draftText = SelectedNote.Text;
        _draftAdo = SelectedNote.AdoWorkItemId ?? "";
        _draftPr = SelectedNote.PrUrl ?? "";
        _editTab = "Write";
        SyncDraftTarget();
    }

    private void CancelNoteEdit()
    {
        _isEditOpen = false;
        _draftText = _draftAdo = _draftPr = "";
        _editTab = "Write";
        SyncDraftTarget();
    }

    private void SyncDraftTarget()
    {
        if (_isEditOpen)
        {
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
            return;
        }

        if (_isNewOpen)
        {
            Ai.RegisterDraftTarget(new AiDraftTarget
            {
                Label = "note body",
                Insert = text =>
                {
                    _newText = string.IsNullOrWhiteSpace(_newText)
                        ? text
                        : $"{_newText.TrimEnd()}\n\n{text}";
                    InvokeAsync(StateHasChanged);
                },
            });
            return;
        }

        Ai.RegisterDraftTarget(null);
    }

    private async Task SaveNoteEdit()
    {
        if (SelectedNote is null)
        {
            return;
        }

        TeamNote note = SelectedNote;
        TeamNote updated = CloneNote(note);
        updated.Text = _draftText;
        updated.AdoWorkItemId = string.IsNullOrWhiteSpace(_draftAdo) ? null : _draftAdo.Trim();
        updated.PrUrl = string.IsNullOrWhiteSpace(_draftPr) ? null : _draftPr.Trim();
        updated.LastModifiedIso = DateTimeOffset.UtcNow.ToString("o");

        try
        {
            await TeamNoteService.UpdateAsync(Member.Id, updated);
            _isEditOpen = false;
            SyncDraftTarget();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to save note changes right now. Please try again.\n\n{ex.Message}");
        }
    }

    private void OpenFull(Guid noteId) => Nav.NavigateTo($"/team/{Member.Id}/notes/{noteId}");

    private void OpenFullClick(MouseEventArgs _, Guid noteId) => OpenFull(noteId);

    private void OpenNew()
    {
        _isNewOpen = true;
        _newTag = NoteTag.Quick;
        _newTitle = _newText = _newAdo = _newPr = "";
        SyncDraftTarget();
    }

    private void CloseNew()
    {
        _isNewOpen = false;
        _newTitle = _newText = _newAdo = _newPr = "";
        SyncDraftTarget();
    }

    private async Task CreateNote()
    {
        if (string.IsNullOrWhiteSpace(_newText))
        {
            return;
        }

        var title = _newTitle.Trim();
        var ado = _newAdo.Trim();
        var pr = _newPr.Trim();
        try
        {
            TeamNote saved = await TeamNoteService.AddAsync(
                Member.Id,
                _newTag,
                _newText.Trim(),
                string.IsNullOrEmpty(title) ? null : title,
                string.IsNullOrEmpty(ado) ? null : ado,
                string.IsNullOrEmpty(pr) ? null : pr);
            CloseNew();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to create note right now. Please try again.\n\n{ex.Message}");
        }
    }

    private static TeamNote CloneNote(TeamNote n) => new()
    {
        Id = n.Id,
        CreatedIso = n.CreatedIso,
        LastModifiedIso = n.LastModifiedIso,
        Tag = n.Tag,
        Title = n.Title,
        Text = n.Text,
        AdoWorkItemId = n.AdoWorkItemId,
        PrUrl = n.PrUrl
    };

    private static TeamMember CloneMemberWithNotes(TeamMember m, IReadOnlyList<TeamNote> notes) =>
        EntityClone.TeamMember(m, notes: notes);

    public void Dispose() => Ai.RegisterDraftTarget(null);
}
