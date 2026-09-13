using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class TeamMemberRiskDetail : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private TeamMemberRiskService TeamMemberRiskService { get; set; } = null!;

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? TeamMemberRiskId { get; set; }

    private bool _editing;
    private TeamMemberRisk? _draft;

    private TeamMember? Member =>
        Guid.TryParse(MemberId, out Guid id) ? Cache.Team.FirstOrDefault(m => m.Id == id) : null;

    private TeamMemberRisk? View
    {
        get
        {
            if (!Guid.TryParse(TeamMemberRiskId, out Guid rid))
            {
                return null;
            }

            TeamMemberRisk? r = Cache.TeamMemberRisks.FirstOrDefault(x => x.Id == rid);
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
        View?.LinkedRiskId is { } lid ? Cache.Risks.FirstOrDefault(r => r.Id == lid) : null;

    private string LinkedRiskValue =>
        (_editing && _draft is not null ? _draft.LinkedRiskId : View?.LinkedRiskId)?.ToString() ?? "";

    private string ReviewedLabel
    {
        get
        {
            var days = DisplayLabels.DaysSince((_editing && _draft is not null ? _draft : View)?.LastReviewedIso);
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
        Cache.Changed += OnChangedAsync;
        Ai.SetContext("Context: Team Member Risk Detail",
        [
            new AiAction("summarize-risk", "Summarize this risk"),
            new AiAction("suggest-mitigation", "Suggest mitigation experiments"),
        ]);
        await Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(MemberId) && !Guid.TryParse(MemberId, out _))
        {
            Nav.NavigateTo("/team", replace: true);
            return;
        }

        if (Guid.TryParse(MemberId, out Guid id))
        {
            Selection.SelectTeamMember(id);
            if (Cache.TeamReady && Member is null)
            {
                Nav.NavigateTo("/team", replace: true);
            }
        }
    }

    private void BackToRisks() => Nav.NavigateTo($"/team/{MemberId}/risks");
    private void BackToMember() => Nav.NavigateTo($"/team/{MemberId}");

    private void OnDraftFirstNoticed(ChangeEventArgs e)
    {
        if (_draft is null)
        {
            return;
        }

        _draft.FirstNoticedDateIso = e.Value?.ToString() ?? "";
    }

    private void BeginEdit()
    {
        if (View is null)
        {
            return;
        }

        _draft = EntityClone.TeamMemberRisk(View);
        _editing = true;
    }

    private void CancelEdit()
    {
        _editing = false;
        _draft = null;
    }

    private void SaveEdit()
    {
        if (_draft is null)
        {
            return;
        }

        PersistRisk(_draft);
        _editing = false;
        _draft = null;
    }

    private void OnLinkedRiskChange(ChangeEventArgs e)
    {
        Guid? next = Guid.TryParse(e.Value?.ToString(), out Guid id) ? id : null;
        if (_editing && _draft is not null)
        {
            _draft.LinkedRiskId = next;
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
        if (_editing && _draft is not null)
        {
            _draft.LastReviewedIso = iso;
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

        Selection.SelectRisk(LinkedGlobal.Id);
        Nav.NavigateTo("/risks");
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
            await TeamMemberRiskService.UpdateAsync(memberId, next);
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync($"Unable to save team member risk right now. Please try again.\n\n{ex.Message}");
        }
    }

    private async void OnChangedAsync()
    {
        try
        {
            await InvokeAsync(() =>
            {
                if (Guid.TryParse(MemberId, out Guid id) && Cache.TeamReady && Cache.Team.All(m => m.Id != id))
                {
                    Nav.NavigateTo("/team", replace: true);
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

    public void Dispose() => Cache.Changed -= OnChangedAsync;
}
