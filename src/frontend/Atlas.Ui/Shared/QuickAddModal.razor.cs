using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Shared;

public partial class QuickAddModal
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] TaskService TaskService { get; set; } = default!;
    [Inject] RiskService RiskService { get; set; } = default!;
    [Inject] TeamNoteService TeamNoteService { get; set; } = default!;

    static readonly string[] NoteTags = ["Quick", "Standup", "Progress", "Praise", "Concern", "Blocker"];

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    ElementReference _panel;
    string _kind = "task";
    bool _saving;
    string _taskTitle = "";
    string _riskTitle = "";
    string _memberId = "";
    string _noteTag = "Quick";
    string _noteTitle = "";
    string _noteText = "";
    string _noteAdo = "";
    string _notePr = "";

    bool CanCreate =>
        _kind is "task" or "risk"
        || (_kind == "note" && !string.IsNullOrWhiteSpace(_memberId) && !string.IsNullOrWhiteSpace(_noteText));

    void ResetForm()
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

    async Task HandleClose()
    {
        if (_saving) return;
        ResetForm();
        await OnClose.InvokeAsync();
    }

    Task CloseFromOverlay() => HandleClose();

    async Task OnOverlayKey(KeyboardEventArgs e)
    {
        if (e.Key == "Escape") await HandleClose();
    }

    void OnNoteTagChange(ChangeEventArgs e) => _noteTag = e.Value?.ToString() ?? "Quick";

    async Task OnTitleKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && CanCreate && !_saving)
        {
            await HandleCreate();
        }
    }

    async Task HandleCreate()
    {
        if (_saving || !CanCreate) return;
        _saving = true;
        try
        {
            if (_kind == "task")
            {
                string title = string.IsNullOrWhiteSpace(_taskTitle) ? "New task" : _taskTitle.Trim();
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
                string title = string.IsNullOrWhiteSpace(_riskTitle) ? "New risk" : _riskTitle.Trim();
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

            string text = _noteText.Trim();
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
            string titleOpt = _noteTitle.Trim();
            string ado = _noteAdo.Trim();
            string pr = _notePr.Trim();
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
