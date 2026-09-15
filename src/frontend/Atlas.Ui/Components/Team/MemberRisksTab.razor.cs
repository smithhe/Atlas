using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Team
{
public partial class MemberRisksTab
{
    [Inject] private IAppCacheService _cache { get; set; } = null!;
    [Inject] private NavigationManager _nav { get; set; } = null!;
    [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
    [Inject] private ITeamMemberRiskService _teamMemberRiskService { get; set; } = null!;

    [Parameter] public Guid MemberId { get; set; }

    private string Query { get; set; } = "";
    private string StatusFilter { get; set; } = "All";
    private bool CreateOpen { get; set; }
        private string Title { get; set; } = "";
    private string Severity { get; set; } = "Medium";
    private string Status { get; set; } = "Open";
    private string Trend { get; set; } = "Stable";
    private string FirstNoticed { get; set; } = DisplayLabels.TodayIsoDateLocal();
        private string RiskType { get; set; } = "";
    private string ImpactArea { get; set; } = "";
    private string Description { get; set; } = "";
    private string CurrentAction { get; set; } = "";
    private string LinkedRiskId { get; set; } = "";

    private List<TeamMemberRisk> MemberRisks
    {
        get
        {
            var q = this.Query.Trim().ToLowerInvariant();
            return this._cache.TeamMemberRisks
                .Where(r => r.MemberId == MemberId)
                .Where(r => this.StatusFilter == "All" || r.Status == this.StatusFilter)
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
        this.Title = "";
        this.Severity = "Medium";
        this.Status = "Open";
        this.Trend = "Stable";
        this.FirstNoticed = DisplayLabels.TodayIsoDateLocal();
        this.RiskType = this.ImpactArea = this.Description = this.CurrentAction = this.LinkedRiskId = "";
        this.CreateOpen = true;
    }

    private void OnFirstNoticedChange(ChangeEventArgs e) => this.FirstNoticed = e.Value?.ToString() ?? "";

    private void OpenRisk(Guid riskId) => this._nav.NavigateTo($"/team/{MemberId}/risks/{riskId}");

    private void CloseCreate() => this.CreateOpen = false;

    private async Task SaveCreate()
    {
        var title = this.Title.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            Guid? linked = Guid.TryParse(this.LinkedRiskId, out Guid lid) ? lid : null;
            var draft = new TeamMemberRisk
            {
                MemberId = MemberId,
                Title = title,
                Severity = this.Severity,
                RiskType = this.RiskType.Trim(),
                Status = this.Status,
                Trend = this.Trend,
                FirstNoticedDateIso = this.FirstNoticed,
                ImpactArea = this.ImpactArea.Trim(),
                Description = this.Description.Trim(),
                CurrentAction = this.CurrentAction.Trim(),
                LinkedRiskId = linked
            };
            TeamMemberRisk next = await this._teamMemberRiskService.AddAsync(MemberId, draft);
            CloseCreate();
            this._nav.NavigateTo($"/team/{MemberId}/risks/{next.Id}");
        }
        catch (Exception ex)
        {
            await this._dialogs.AlertAsync($"Unable to create team member risk right now. Please try again.\n\n{ex.Message}");
        }
    }
}
}
