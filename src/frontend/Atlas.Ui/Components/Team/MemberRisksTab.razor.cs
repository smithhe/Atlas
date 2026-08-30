using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberRisksTab
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private TeamMemberRiskService TeamMemberRiskService { get; set; } = null!;

    [Parameter] public Guid MemberId { get; set; }

    private string _query = "";
    private string _statusFilter = "All";
    private bool _createOpen;
    private string _title = "", _severity = "Medium", _status = "Open", _trend = "Stable";
    private string _firstNoticed = DisplayLabels.TodayIsoDateLocal();
    private string _riskType = "", _impactArea = "", _description = "", _currentAction = "", _linkedRiskId = "";

    private List<TeamMemberRisk> MemberRisks
    {
        get
        {
            var q = _query.Trim().ToLowerInvariant();
            return Cache.TeamMemberRisks
                .Where(r => r.MemberId == MemberId)
                .Where(r => _statusFilter == "All" || r.Status == _statusFilter)
                .Where(r =>
                {
                    if (string.IsNullOrEmpty(q))
                    {
                        return true;
                    }

                    var hay = string.Join(' ', r.Title, r.RiskType, r.ImpactArea, r.Description, r.CurrentAction).ToLowerInvariant();
                    return hay.Contains(q);
                })
                .ToList();
        }
    }

    private void OpenCreate()
    {
        _title = "";
        _severity = "Medium";
        _status = "Open";
        _trend = "Stable";
        _firstNoticed = DisplayLabels.TodayIsoDateLocal();
        _riskType = _impactArea = _description = _currentAction = _linkedRiskId = "";
        _createOpen = true;
    }

    private void OnFirstNoticedChange(ChangeEventArgs e) => _firstNoticed = e.Value?.ToString() ?? "";

    private void OpenRisk(Guid riskId) => Nav.NavigateTo($"/team/{MemberId}/risks/{riskId}");

    private void CloseCreate() => _createOpen = false;

    private async Task SaveCreate()
    {
        var title = _title.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            Guid? linked = Guid.TryParse(_linkedRiskId, out Guid lid) ? lid : null;
            var draft = new TeamMemberRisk
            {
                MemberId = MemberId,
                Title = title,
                Severity = _severity,
                RiskType = _riskType.Trim(),
                Status = _status,
                Trend = _trend,
                FirstNoticedDateIso = _firstNoticed,
                ImpactArea = _impactArea.Trim(),
                Description = _description.Trim(),
                CurrentAction = _currentAction.Trim(),
                LinkedRiskId = linked
            };
            TeamMemberRisk next = await TeamMemberRiskService.AddAsync(MemberId, draft);
            CloseCreate();
            Nav.NavigateTo($"/team/{MemberId}/risks/{next.Id}");
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to create team member risk right now. Please try again.\n\n{ex.Message}");
        }
    }
}
