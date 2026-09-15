using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
public partial class TeamMemberRiskDetail : IDisposable
{
    [Inject] private IAppCacheService _cache { get; set; } = null!;
    [Inject] private SelectionState _selection { get; set; } = null!;
    [Inject] private NavigationManager _nav { get; set; } = null!;
    [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
    [Inject] private IAiStateService _ai { get; set; } = null!;
    [Inject] private ITeamMemberRiskService _teamMemberRiskService { get; set; } = null!;

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? TeamMemberRiskId { get; set; }

    private bool Editing { get; set; }
    private TeamMemberRisk? Draft { get; set; }

    private TeamMember? Member =>
        Guid.TryParse(MemberId, out Guid id) ? this._cache.Team.FirstOrDefault(m => m.Id == id) : null;

    private TeamMemberRisk? View
    {
        get
        {
            if (!Guid.TryParse(TeamMemberRiskId, out Guid rid))
            {
                return null;
            }

            TeamMemberRisk? r = this._cache.TeamMemberRisks.FirstOrDefault(x => x.Id == rid);
            if (r is null)
            {
                return null;
            }

            if (Guid.TryParse(MemberId, out Guid mid) && r.MemberId != mid)
            {
                return null;
            }

            return r;
        }
    }

    private Risk? LinkedGlobal =>
        View?.LinkedRiskId is { } lid ? this._cache.Risks.FirstOrDefault(r => r.Id == lid) : null;

    private string LinkedRiskValue =>
        (this.Editing && this.Draft is not null ? this.Draft.LinkedRiskId : View?.LinkedRiskId)?.ToString() ?? "";

    private string ReviewedLabel
    {
        get
        {
            var days = DisplayLabels.DaysSince((this.Editing && this.Draft is not null ? this.Draft : View)?.LastReviewedIso);
            if (days is null)
            {
                return "—";
            }

            if (days == 0)
            {
                return "today";
            }

            return days == 1 ? "1 day ago" : $"{days} days ago";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        this._cache.Changed += OnChangedAsync;
        this._ai.SetContext("Context: Team Member Risk Detail",
        [
            new AiAction("summarize-risk", "Summarize this risk"),
            new AiAction("suggest-mitigation", "Suggest mitigation experiments"),
        ]);
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
    }

    private void BackToRisks() => this._nav.NavigateTo($"/team/{MemberId}/risks");
    private void BackToMember() => this._nav.NavigateTo($"/team/{MemberId}");

    private void OnDraftFirstNoticed(ChangeEventArgs e)
    {
        if (this.Draft is null)
        {
            return;
        }

        this.Draft.FirstNoticedDateIso = e.Value?.ToString() ?? "";
    }

    private void BeginEdit()
    {
        if (View is null)
        {
            return;
        }

        this.Draft = EntityClone.TeamMemberRisk(View);
        this.Editing = true;
    }

    private void CancelEdit()
    {
        this.Editing = false;
        this.Draft = null;
    }

    private void SaveEdit()
    {
        if (this.Draft is null)
        {
            return;
        }

        PersistRisk(this.Draft);
        this.Editing = false;
        this.Draft = null;
    }

    private void OnLinkedRiskChange(ChangeEventArgs e)
    {
        Guid? next = Guid.TryParse(e.Value?.ToString(), out Guid id) ? id : null;
        if (this.Editing && this.Draft is not null)
        {
            this.Draft.LinkedRiskId = next;
            return;
        }

        if (View is null)
        {
            return;
        }

        TeamMemberRisk updated = EntityClone.TeamMemberRisk(View, linkedRiskId: next, setLinkedRiskId: true);
        PersistRisk(updated);
    }

    private void MarkReviewed()
    {
        var iso = DateTimeOffset.UtcNow.ToString("o");
        if (this.Editing && this.Draft is not null)
        {
            this.Draft.LastReviewedIso = iso;
            return;
        }

        if (View is null)
        {
            return;
        }

        TeamMemberRisk updated = EntityClone.TeamMemberRisk(View, lastReviewedIso: iso, setLastReviewedIso: true);
        PersistRisk(updated);
    }

    private void OpenLinkedGlobal()
    {
        if (LinkedGlobal is null)
        {
            return;
        }

        this._selection.SelectRisk(LinkedGlobal.Id);
        this._nav.NavigateTo("/risks");
    }

    private void PersistRisk(TeamMemberRisk next)
    {
        if (!Guid.TryParse(MemberId, out Guid memberId))
        {
            return;
        }

        PersistRiskFireAndForget(memberId, next);
    }

    private async void PersistRiskFireAndForget(Guid memberId, TeamMemberRisk next)
    {
        try
        {
            await PersistAsync(memberId, next);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private async Task PersistAsync(Guid memberId, TeamMemberRisk next)
    {
        try
        {
            await this._teamMemberRiskService.UpdateAsync(memberId, next);
        }
        catch (Exception ex)
        {
            await this._dialogs.AlertAsync($"Unable to save team member risk right now. Please try again.\n\n{ex.Message}");
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

                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    public void Dispose() => this._cache.Changed -= OnChangedAsync;
}
}
