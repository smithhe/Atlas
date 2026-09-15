using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Team
{
    public partial class MemberNotesTab : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IAiStateService _ai { get; set; } = null!;
        [Inject] private ITeamNoteService _teamNoteService { get; set; } = null!;

        [Parameter, EditorRequired] public TeamMember Member { get; set; } = null!;

        private static readonly NoteTag[] NoteTags = [NoteTag.Quick, NoteTag.Standup, NoteTag.Progress, NoteTag.Praise, NoteTag.Concern, NoteTag.Blocker];

        private string Query { get; set; } = "";
        private string TagFilter { get; set; } = "All";
        private string SortBy { get; set; } = "Newest";
        private string QuickFilter { get; set; } = "All";
        private Guid? ExpandedNoteId { get; set; }
        private Guid? SelectedNoteId { get; set; }
        private bool IsNewOpen { get; set; }
        private bool IsEditOpen { get; set; }
        private string EditTab { get; set; } = "Write";
        private NoteTag NewTag { get; set; } = NoteTag.Quick;
            private string NewTitle { get; set; } = "";
        private string NewText { get; set; } = "";
        private string NewAdo { get; set; } = "";
        private string NewPr { get; set; } = "";
            private string DraftText { get; set; } = "";
        private string DraftAdo { get; set; } = "";
        private string DraftPr { get; set; } = "";

        private TeamNote? SelectedNote =>
            this.SelectedNoteId is { } id ? Member.Notes.FirstOrDefault(n => n.Id == id) : null;

        private List<TeamNote> FilteredSorted
        {
            get
            {
                var q = this.Query.Trim().ToLowerInvariant();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                var weekMs = TimeSpan.FromDays(7);

                bool Matches(TeamNote n)
                {
                    if (this.TagFilter != "All" && !string.Equals(n.Tag.ToString(), this.TagFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (this.QuickFilter == "ThisWeek")
                    {
                        if (!DateTimeOffset.TryParse(n.CreatedIso, out DateTimeOffset created) || now - created.ToUniversalTime() > weekMs)
                        {
                            return false;
                        }
                    }
                    else if (this.QuickFilter == "ActionItems" && !IsActionItemNote(n))
                    {
                        return false;
                    }
                    else if (this.QuickFilter == "OneOnOne" && !IsOneOnOneNote(n))
                    {
                        return false;
                    }
                    else if (this.QuickFilter == "Risks" && !IsRiskNote(n))
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
                    return this.SortBy == "Newest" ? bt.CompareTo(at) : at.CompareTo(bt);
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

        private void ToggleExpand(Guid id) => this.ExpandedNoteId = this.ExpandedNoteId == id ? null : id;

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
            this.SelectedNoteId = id;
            this.IsEditOpen = false;
            this.DraftText = "";
            this.EditTab = "Write";
            SyncDraftTarget();
        }

        private void CloseNoteModal()
        {
            this.SelectedNoteId = null;
            this.IsEditOpen = false;
            this.DraftText = this.DraftAdo = this.DraftPr = "";
            this.EditTab = "Write";
            SyncDraftTarget();
        }

        private void BeginNoteEdit()
        {
            if (SelectedNote is null)
            {
                return;
            }

            this.IsEditOpen = true;
            this.DraftText = SelectedNote.Text;
            this.DraftAdo = SelectedNote.AdoWorkItemId ?? "";
            this.DraftPr = SelectedNote.PrUrl ?? "";
            this.EditTab = "Write";
            SyncDraftTarget();
        }

        private void CancelNoteEdit()
        {
            this.IsEditOpen = false;
            this.DraftText = this.DraftAdo = this.DraftPr = "";
            this.EditTab = "Write";
            SyncDraftTarget();
        }

        private void SyncDraftTarget()
        {
            if (this.IsEditOpen)
            {
                this._ai.RegisterDraftTarget(new AiDraftTarget
                {
                    Label = "note body",
                    Insert = text =>
                    {
                        this.DraftText = string.IsNullOrWhiteSpace(this.DraftText)
                            ? text
                            : $"{this.DraftText.TrimEnd()}\n\n{text}";
                        return InvokeAsync(StateHasChanged);
                    },
                });
                return;
            }

            if (this.IsNewOpen)
            {
                this._ai.RegisterDraftTarget(new AiDraftTarget
                {
                    Label = "note body",
                    Insert = text =>
                    {
                        this.NewText = string.IsNullOrWhiteSpace(this.NewText)
                            ? text
                            : $"{this.NewText.TrimEnd()}\n\n{text}";
                        return InvokeAsync(StateHasChanged);
                    },
                });
                return;
            }

            this._ai.RegisterDraftTarget(null);
        }

        private async Task SaveNoteEdit()
        {
            if (SelectedNote is null)
            {
                return;
            }

            TeamNote note = SelectedNote;
            TeamNote updated = CloneNote(note);
            updated.Text = this.DraftText;
            updated.AdoWorkItemId = string.IsNullOrWhiteSpace(this.DraftAdo) ? null : this.DraftAdo.Trim();
            updated.PrUrl = string.IsNullOrWhiteSpace(this.DraftPr) ? null : this.DraftPr.Trim();
            updated.LastModifiedIso = DateTimeOffset.UtcNow.ToString("o");

            try
            {
                await this._teamNoteService.UpdateAsync(Member.Id, updated);
                this.IsEditOpen = false;
                SyncDraftTarget();
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync($"Unable to save note changes right now. Please try again.\n\n{ex.Message}");
            }
        }

        private void OpenFull(Guid noteId) => this._nav.NavigateTo($"/team/{Member.Id}/notes/{noteId}");

        private void OpenFullClick(MouseEventArgs _, Guid noteId) => OpenFull(noteId);

        private void OpenNew()
        {
            this.IsNewOpen = true;
            this.NewTag = NoteTag.Quick;
            this.NewTitle = this.NewText = this.NewAdo = this.NewPr = "";
            SyncDraftTarget();
        }

        private void CloseNew()
        {
            this.IsNewOpen = false;
            this.NewTitle = this.NewText = this.NewAdo = this.NewPr = "";
            SyncDraftTarget();
        }

        private async Task CreateNote()
        {
            if (string.IsNullOrWhiteSpace(this.NewText))
            {
                return;
            }

            var title = this.NewTitle.Trim();
            var ado = this.NewAdo.Trim();
            var pr = this.NewPr.Trim();
            try
            {
                TeamNote saved = await this._teamNoteService.AddAsync(
                    Member.Id,
                    this.NewTag,
                    this.NewText.Trim(),
                    string.IsNullOrEmpty(title) ? null : title,
                    string.IsNullOrEmpty(ado) ? null : ado,
                    string.IsNullOrEmpty(pr) ? null : pr);
                CloseNew();
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync($"Unable to create note right now. Please try again.\n\n{ex.Message}");
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

        public void Dispose() => this._ai.RegisterDraftTarget(null);
    }
}
