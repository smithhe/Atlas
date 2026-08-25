using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberRisksTab
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] TeamMemberRiskService TeamMemberRiskService { get; set; } = default!;

    [Parameter] public Guid MemberId { get; set; }

    string _query = "";
    string _statusFilter = "All";
    bool _createOpen;
    string _title = "", _severity = "Medium", _status = "Open", _trend = "Stable";
    string _firstNoticed = DisplayLabels.TodayIsoDateLocal();
    string _riskType = "", _impactArea = "", _description = "", _currentAction = "", _linkedRiskId = "";

    List<TeamMemberRisk> MemberRisks
    {
        get
        {
            string q = _query.Trim().ToLowerInvariant();
            return Cache.TeamMemberRisks
                .Where(r => r.MemberId == MemberId)
                .Where(r => _statusFilter == "All" || r.Status == _statusFilter)
                .Where(r =>
                {
                    if (string.IsNullOrEmpty(q)) return true;
                    string hay = string.Join(' ', r.Title, r.RiskType, r.ImpactArea, r.Description, r.CurrentAction).ToLowerInvariant();
                    return hay.Contains(q);
                })
                .ToList();
        }
    }

    void OpenCreate()
    {
        _title = "";
        _severity = "Medium";
        _status = "Open";
        _trend = "Stable";
        _firstNoticed = DisplayLabels.TodayIsoDateLocal();
        _riskType = _impactArea = _description = _currentAction = _linkedRiskId = "";
        _createOpen = true;
    }

    void OnFirstNoticedChange(ChangeEventArgs e) => _firstNoticed = e.Value?.ToString() ?? "";

    void OpenRisk(Guid riskId) => Nav.NavigateTo($"/team/{MemberId}/risks/{riskId}");

    void CloseCreate() => _createOpen = false;

    async Task SaveCreate()
    {
        string title = _title.Trim();
        if (string.IsNullOrEmpty(title)) return;
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
