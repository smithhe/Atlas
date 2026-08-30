using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Shared;

public partial class QuickAddModal
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private TaskService TaskService { get; set; } = null!;
    [Inject] private RiskService RiskService { get; set; } = null!;
    [Inject] private TeamNoteService TeamNoteService { get; set; } = null!;

    private static readonly string[] NoteTags = ["Quick", "Standup", "Progress", "Praise", "Concern", "Blocker"];

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private ElementReference _panel;
    private string _kind = "task";
    private bool _saving;
    private string _taskTitle = "";
    private string _riskTitle = "";
    private string _memberId = "";
    private string _noteTag = "Quick";
    private string _noteTitle = "";
    private string _noteText = "";
    private string _noteAdo = "";
    private string _notePr = "";

    private bool CanCreate =>
        _kind is "task" or "risk"
        || (_kind == "note" && !string.IsNullOrWhiteSpace(_memberId) && !string.IsNullOrWhiteSpace(_noteText));

    private void ResetForm()
    {
        _kind = "task";
        _taskTitle = "";
        _riskTitle = "";
        _memberId = "";
        _noteTag = "Quick";
        _noteTitle = "";
        _noteText = "";
        _noteAdo = "";
        _notePr = "";
        _saving = false;
    }

    private async Task HandleClose()
    {
        if (_saving)
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

    private void OnNoteTagChange(ChangeEventArgs e) => _noteTag = e.Value?.ToString() ?? "Quick";

    private async Task OnTitleKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && CanCreate && !_saving)
        {
            await HandleCreate();
        }
    }

    private async Task HandleCreate()
    {
        if (_saving || !CanCreate)
        {
            return;
        }

        _saving = true;
        try
        {
            if (_kind == "task")
            {
                var title = string.IsNullOrWhiteSpace(_taskTitle) ? "New task" : _taskTitle.Trim();
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
                AtlasTask created = await TaskService.CreateAsync(draft);
            Guid id = created.Id;
                await Cache.RefetchTasksAsync();
                ResetForm();
                await OnClose.InvokeAsync();
                Nav.NavigateTo($"/tasks/{id}");
                return;
            }

            if (_kind == "risk")
            {
                var title = string.IsNullOrWhiteSpace(_riskTitle) ? "New risk" : _riskTitle.Trim();
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
                Risk created = await RiskService.CreateAsync(draft);
            Guid id = created.Id;
                await Cache.RefetchRisksAsync();
                ResetForm();
                await OnClose.InvokeAsync();
                Nav.NavigateTo($"/risks/{id}");
                return;
            }

            if (!Guid.TryParse(_memberId, out Guid memberId))
            {
                await Dialogs.AlertAsync("Select a team member and enter note text before creating.");
                return;
            }

            var text = _noteText.Trim();
            if (string.IsNullOrEmpty(text))
            {
                await Dialogs.AlertAsync("Select a team member and enter note text before creating.");
                return;
            }

            TeamMember? member = Cache.Team.FirstOrDefault(m => m.Id == memberId);
            if (member is null)
            {
                await Dialogs.AlertAsync("That team member is no longer available. Refresh and try again.");
                return;
            }

            Enum.TryParse(_noteTag, out NoteTag tag);
            var titleOpt = _noteTitle.Trim();
            var ado = _noteAdo.Trim();
            var pr = _notePr.Trim();
            await TeamNoteService.AddAsync(
            memberId,
            tag,
            text,
            string.IsNullOrEmpty(titleOpt) ? null : titleOpt,
            string.IsNullOrEmpty(ado) ? null : ado,
            string.IsNullOrEmpty(pr) ? null : pr);

            await Cache.RefetchTeamAsync();
            Selection.SelectTeamMember(memberId);
            ResetForm();
            await OnClose.InvokeAsync();
            Nav.NavigateTo($"/team/{memberId}/notes");
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync($"Unable to create {(_kind == "note" ? "note" : _kind)} right now. Please try again.");
        }
        finally
        {
            _saving = false;
        }
    }
}
