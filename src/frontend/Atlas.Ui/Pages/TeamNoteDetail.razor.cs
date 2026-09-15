using Microsoft.AspNetCore.Components;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class TeamNoteDetail : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private SelectionState _selection { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IAiStateService _ai { get; set; } = null!;
        [Inject] private ITeamNoteService _teamNoteService { get; set; } = null!;

        [Parameter] public string? MemberId { get; set; }
        [Parameter] public string? NoteId { get; set; }

        private static readonly NoteTag[] NoteTags = [NoteTag.Quick, NoteTag.Standup, NoteTag.Progress, NoteTag.Praise, NoteTag.Concern, NoteTag.Blocker];

        private bool Editing { get; set; }
            private string DraftTitle { get; set; } = "";
        private string DraftText { get; set; } = "";
        private string DraftAdo { get; set; } = "";
        private string DraftPr { get; set; } = "";
        private NoteTag DraftTag { get; set; } = NoteTag.Quick;

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

        protected override async Task OnInitializedAsync()
        {
            this._cache.Changed += OnChangedAsync;
            this._ai.SetContext("Context: Team Note Detail", [new AiAction("summarize-note", "Summarize this note")]);
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

            SyncDraftsFromNote();
        }

        private void SyncDraftsFromNote()
        {
            if (Note is null || this.Editing)
            {
                return;
            }

            this.DraftTitle = Note.Title ?? "";
            this.DraftTag = Note.Tag;
            this.DraftText = Note.Text;
            this.DraftAdo = Note.AdoWorkItemId ?? "";
            this.DraftPr = Note.PrUrl ?? "";
        }

        private void BackToNotes() => this._nav.NavigateTo($"/team/{MemberId}/notes");
        private void BackToMember() => this._nav.NavigateTo($"/team/{MemberId}");

        private void BeginEdit()
        {
            if (Note is null)
            {
                return;
            }

            this.DraftTitle = Note.Title ?? "";
            this.DraftTag = Note.Tag;
            this.DraftText = Note.Text;
            this.DraftAdo = Note.AdoWorkItemId ?? "";
            this.DraftPr = Note.PrUrl ?? "";
            this.Editing = true;
            SyncDraftTarget();
        }

        private void CancelEdit()
        {
            SyncDraftsFromNote();
            this.Editing = false;
            if (Note is not null)
            {
                this.DraftTitle = Note.Title ?? "";
                this.DraftTag = Note.Tag;
                this.DraftText = Note.Text;
                this.DraftAdo = Note.AdoWorkItemId ?? "";
                this.DraftPr = Note.PrUrl ?? "";
            }
            SyncDraftTarget();
        }

        private void SyncDraftTarget()
        {
            if (!this.Editing)
            {
                this._ai.RegisterDraftTarget(null);
                return;
            }

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
        }

        private async Task SaveEdit()
        {
            if (Member is null || Note is null)
            {
                return;
            }

            var nextTitle = this.DraftTitle.Trim();
            var ado = this.DraftAdo.Trim();
            var pr = this.DraftPr.Trim();
            var updated = new TeamNote
            {
                Id = Note.Id,
                CreatedIso = Note.CreatedIso,
                LastModifiedIso = DateTimeOffset.UtcNow.ToString("o"),
                Tag = this.DraftTag,
                Title = string.IsNullOrEmpty(nextTitle) ? null : nextTitle,
                Text = this.DraftText,
                AdoWorkItemId = string.IsNullOrEmpty(ado) ? null : ado,
                PrUrl = string.IsNullOrEmpty(pr) ? null : pr
            };

            try
            {
                await this._teamNoteService.UpdateAsync(Member.Id, updated);
                this.Editing = false;
                SyncDraftTarget();
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync($"Unable to save note changes right now. Please try again.\n\n{ex.Message}");
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

                    SyncDraftsFromNote();
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
            this._ai.RegisterDraftTarget(null);
        }
    }
}
