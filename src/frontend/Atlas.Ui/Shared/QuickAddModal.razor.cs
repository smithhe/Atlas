using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Shared
{
    public partial class QuickAddModal
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private SelectionState _selection { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private ITaskService _taskService { get; set; } = null!;
        [Inject] private IRiskService _riskService { get; set; } = null!;
        [Inject] private ITeamNoteService _teamNoteService { get; set; } = null!;

        private static readonly string[] NoteTags = ["Quick", "Standup", "Progress", "Praise", "Concern", "Blocker"];

        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }

        private ElementReference Panel { get; set; }
        private string Kind { get; set; } = "task";
        private bool Saving { get; set; }
        private string TaskTitle { get; set; } = "";
        private string RiskTitle { get; set; } = "";
        private string MemberId { get; set; } = "";
        private string NoteTag { get; set; } = "Quick";
        private string NoteTitle { get; set; } = "";
        private string NoteText { get; set; } = "";
        private string NoteAdo { get; set; } = "";
        private string NotePr { get; set; } = "";

        private bool CanCreate =>
            this.Kind is "task" or "risk"
            || (this.Kind == "note" && !string.IsNullOrWhiteSpace(this.MemberId) && !string.IsNullOrWhiteSpace(this.NoteText));

        private void ResetForm()
        {
            this.Kind = "task";
            this.TaskTitle = "";
            this.RiskTitle = "";
            this.MemberId = "";
            this.NoteTag = "Quick";
            this.NoteTitle = "";
            this.NoteText = "";
            this.NoteAdo = "";
            this.NotePr = "";
            this.Saving = false;
        }

        private async Task HandleClose()
        {
            if (this.Saving)
            {
                return;
            }

            ResetForm();
            await OnClose.InvokeAsync();
        }

        private Task CloseFromOverlay() => HandleClose();

        private async Task OnOverlayKey(KeyboardEventArgs e)
        {
            if (e.Key == "Escape")
            {
                await HandleClose();
            }
        }

        private void OnNoteTagChange(ChangeEventArgs e) => this.NoteTag = e.Value?.ToString() ?? "Quick";

        private async Task OnTitleKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && CanCreate && !this.Saving)
            {
                await HandleCreate();
            }
        }

        private async Task HandleCreate()
        {
            if (this.Saving || !CanCreate)
            {
                return;
            }

            this.Saving = true;
            try
            {
                if (this.Kind == "task")
                {
                    var title = string.IsNullOrWhiteSpace(this.TaskTitle) ? "New task" : this.TaskTitle.Trim();
                    var draft = new AtlasTask
                    {
                        Title = title,
                        Priority = Priority.Medium,
                        Status = Models.TaskStatus.NotStarted,
                        EstimatedDurationText = "1h",
                        EstimateConfidence = Confidence.Medium,
                        Notes = "",
                        DependencyTaskIds = Array.Empty<Guid>(),
                        LastTouchedIso = DateTimeOffset.UtcNow.ToString("o")
                    };
                    AtlasTask created = await this._taskService.CreateAsync(draft);
                    Guid id = created.Id;
                    ResetForm();
                    await OnClose.InvokeAsync();
                    this._nav.NavigateTo($"/tasks/{id}");
                    return;
                }

                if (this.Kind == "risk")
                {
                    var title = string.IsNullOrWhiteSpace(this.RiskTitle) ? "New risk" : this.RiskTitle.Trim();
                    var draft = new Risk
                    {
                        Title = title,
                        Status = RiskStatus.Open,
                        Severity = "Medium",
                        Description = "",
                        Evidence = "",
                        LinkedTaskIds = Array.Empty<Guid>(),
                        LinkedTeamMemberIds = Array.Empty<Guid>(),
                        History = Array.Empty<RiskHistoryEntry>(),
                        LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o")
                    };
                    Risk created = await this._riskService.CreateAsync(draft);
                    Guid id = created.Id;
                    ResetForm();
                    await OnClose.InvokeAsync();
                    this._nav.NavigateTo($"/risks/{id}");
                    return;
                }

                if (!Guid.TryParse(this.MemberId, out Guid memberId))
                {
                    await this._dialogs.AlertAsync("Select a team member and enter note text before creating.");
                    return;
                }

                var text = this.NoteText.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    await this._dialogs.AlertAsync("Select a team member and enter note text before creating.");
                    return;
                }

                TeamMember? member = this._cache.Team.FirstOrDefault(m => m.Id == memberId);
                if (member is null)
                {
                    await this._dialogs.AlertAsync("That team member is no longer available. Refresh and try again.");
                    return;
                }

                Enum.TryParse(this.NoteTag, out NoteTag tag);
                var titleOpt = this.NoteTitle.Trim();
                var ado = this.NoteAdo.Trim();
                var pr = this.NotePr.Trim();
                TeamNote saved = await this._teamNoteService.AddAsync(
                    memberId,
                    tag,
                    text,
                    string.IsNullOrEmpty(titleOpt) ? null : titleOpt,
                    string.IsNullOrEmpty(ado) ? null : ado,
                    string.IsNullOrEmpty(pr) ? null : pr);

                this._selection.SelectTeamMember(memberId);
                ResetForm();
                await OnClose.InvokeAsync();
                this._nav.NavigateTo($"/team/{memberId}/notes");
            }
            catch (Exception)
            {
                await this._dialogs.AlertAsync($"Unable to create {(this.Kind == "note" ? "note" : this.Kind)} right now. Please try again.");
            }
            finally
            {
                this.Saving = false;
            }
        }
    }
}
