using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberNotesTab : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] TeamNoteService TeamNoteService { get; set; } = default!;

    [Parameter, EditorRequired] public TeamMember Member { get; set; } = default!;

    static readonly NoteTag[] NoteTags = [NoteTag.Quick, NoteTag.Standup, NoteTag.Progress, NoteTag.Praise, NoteTag.Concern, NoteTag.Blocker];

    string _query = "";
    string _tagFilter = "All";
    string _sortBy = "Newest";
    string _quickFilter = "All";
    Guid? _expandedNoteId;
    Guid? _selectedNoteId;
    bool _isNewOpen;
    bool _isEditOpen;
    string _editTab = "Write";
    NoteTag _newTag = NoteTag.Quick;
    string _newTitle = "", _newText = "", _newAdo = "", _newPr = "";
    string _draftText = "", _draftAdo = "", _draftPr = "";

    TeamNote? SelectedNote =>
        _selectedNoteId is { } id ? Member.Notes.FirstOrDefault(n => n.Id == id) : null;

    List<TeamNote> FilteredSorted
    {
        get
        {
            string q = _query.Trim().ToLowerInvariant();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan weekMs = TimeSpan.FromDays(7);

            bool Matches(TeamNote n)
            {
                if (_tagFilter != "All" && !string.Equals(n.Tag.ToString(), _tagFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (_quickFilter == "ThisWeek")
                {
                    if (!DateTimeOffset.TryParse(n.CreatedIso, out DateTimeOffset created) || now - created.ToUniversalTime() > weekMs)
                        return false;
                }
                else if (_quickFilter == "ActionItems" && !IsActionItemNote(n)) return false;
                else if (_quickFilter == "OneOnOne" && !IsOneOnOneNote(n)) return false;
                else if (_quickFilter == "Risks" && !IsRiskNote(n)) return false;

                if (string.IsNullOrEmpty(q)) return true;
                string hay = string.Join(' ', n.Tag, n.Title ?? "", DisplayLabels.GetDerivedTitle(n), n.Text, n.AdoWorkItemId ?? "", n.PrUrl ?? "").ToLowerInvariant();
                return hay.Contains(q);
            }

            List<TeamNote> list = Member.Notes.Where(Matches).ToList();
            list.Sort((a, b) =>
            {
                long at = DateTimeOffset.TryParse(a.CreatedIso, out DateTimeOffset ad) ? ad.ToUnixTimeMilliseconds() : 0;
                long bt = DateTimeOffset.TryParse(b.CreatedIso, out DateTimeOffset bd) ? bd.ToUnixTimeMilliseconds() : 0;
                return _sortBy == "Newest" ? bt.CompareTo(at) : at.CompareTo(bt);
            });
            return list;
        }
    }

    static bool IsActionItemNote(TeamNote note)
    {
        string t = note.Text.ToLowerInvariant();
        return t.Contains("- [ ]") || t.Contains("action item") || t.Contains("next:") || t.Contains("todo");
    }

    static bool IsOneOnOneNote(TeamNote note)
    {
        string t = note.Text.ToLowerInvariant();
        return t.Contains("1:1") || t.Contains("one-on-one") || t.Contains("one on one");
    }

    static bool IsRiskNote(TeamNote note)
    {
        if (note.Tag is NoteTag.Concern or NoteTag.Blocker) return true;
        string t = note.Text.ToLowerInvariant();
        return t.Contains("risk") || t.Contains("mitigation") || t.Contains("watchout") || t.Contains("watch-outs");
    }

    void ToggleExpand(Guid id) => _expandedNoteId = _expandedNoteId == id ? null : id;

    void ToggleExpandClick(MouseEventArgs _, Guid id) => ToggleExpand(id);

    void OnRowKey(KeyboardEventArgs e, Guid id)
    {
        if (e.Key is "Enter" or " ") ToggleExpand(id);
    }

    void OpenNoteModal(MouseEventArgs _, Guid id)
    {
        _selectedNoteId = id;
        _isEditOpen = false;
        _draftText = "";
        _editTab = "Write";
        SyncDraftTarget();
    }

    void CloseNoteModal()
    {
        _selectedNoteId = null;
        _isEditOpen = false;
        _draftText = _draftAdo = _draftPr = "";
        _editTab = "Write";
        SyncDraftTarget();
    }

    void BeginNoteEdit()
    {
        if (SelectedNote is null) return;
        _isEditOpen = true;
        _draftText = SelectedNote.Text;
        _draftAdo = SelectedNote.AdoWorkItemId ?? "";
        _draftPr = SelectedNote.PrUrl ?? "";
        _editTab = "Write";
        SyncDraftTarget();
    }

    void CancelNoteEdit()
    {
        _isEditOpen = false;
        _draftText = _draftAdo = _draftPr = "";
        _editTab = "Write";
        SyncDraftTarget();
    }

    void SyncDraftTarget()
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

    async Task SaveNoteEdit()
    {
        if (SelectedNote is null) return;
        TeamNote note = SelectedNote;
        TeamNote updated = CloneNote(note);
        updated.Text = _draftText;
        updated.AdoWorkItemId = string.IsNullOrWhiteSpace(_draftAdo) ? null : _draftAdo.Trim();
        updated.PrUrl = string.IsNullOrWhiteSpace(_draftPr) ? null : _draftPr.Trim();
        updated.LastModifiedIso = DateTimeOffset.UtcNow.ToString("o");

        try
        {
            await TeamNoteService.UpdateAsync(Member.Id, updated);
            List<TeamNote> nextNotes = Member.Notes.Select(x => x.Id == note.Id ? updated : x).ToList();
            Cache.UpdateTeamMember(CloneMemberWithNotes(Member, nextNotes));
            _isEditOpen = false;
            SyncDraftTarget();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to save note changes right now. Please try again.\n\n{ex.Message}");
        }
    }

    void OpenFull(Guid noteId) => Nav.NavigateTo($"/team/{Member.Id}/notes/{noteId}");

    void OpenFullClick(MouseEventArgs _, Guid noteId) => OpenFull(noteId);

    void OpenNew()
    {
        _isNewOpen = true;
        _newTag = NoteTag.Quick;
        _newTitle = _newText = _newAdo = _newPr = "";
        SyncDraftTarget();
    }

    void CloseNew()
    {
        _isNewOpen = false;
        _newTitle = _newText = _newAdo = _newPr = "";
        SyncDraftTarget();
    }

    async Task CreateNote()
    {
        if (string.IsNullOrWhiteSpace(_newText)) return;
        string title = _newTitle.Trim();
        string ado = _newAdo.Trim();
        string pr = _newPr.Trim();
        try
        {
            AtlasApiDTOsTeamMembersNotesAddTeamNoteResponse res = await TeamNoteService.AddAsync(
            Member.Id,
            _newTag,
            _newText.Trim(),
            string.IsNullOrEmpty(title) ? null : title,
            string.IsNullOrEmpty(ado) ? null : ado,
            string.IsNullOrEmpty(pr) ? null : pr);
            Guid id = res.Id ?? Guid.NewGuid();
            var now = DateTimeOffset.UtcNow.ToString("o");
            var saved = new TeamNote
            {
                Id = id,
                CreatedIso = now,
                LastModifiedIso = now,
                Tag = _newTag,
                Title = string.IsNullOrEmpty(title) ? null : title,
                Text = _newText.Trim(),
                AdoWorkItemId = string.IsNullOrEmpty(ado) ? null : ado,
                PrUrl = string.IsNullOrEmpty(pr) ? null : pr
            };
            Cache.UpdateTeamMember(CloneMemberWithNotes(Member, new[] { saved }.Concat(Member.Notes).ToList()));
            CloseNew();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to create note right now. Please try again.\n\n{ex.Message}");
        }
    }

    static TeamNote CloneNote(TeamNote n) => new()
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

    static TeamMember CloneMemberWithNotes(TeamMember m, IReadOnlyList<TeamNote> notes) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Role = m.Role,
        StatusDot = m.StatusDot,
        CurrentFocus = m.CurrentFocus,
        Profile = m.Profile,
        Signals = m.Signals,
        Notes = notes,
        PinnedNoteIds = m.PinnedNoteIds,
        ActivitySnapshot = m.ActivitySnapshot,
        AzureItems = m.AzureItems
    };

    public void Dispose() => Ai.RegisterDraftTarget(null);
}
