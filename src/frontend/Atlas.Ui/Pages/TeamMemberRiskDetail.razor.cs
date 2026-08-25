using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class TeamMemberRiskDetail : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] TeamMemberRiskService TeamMemberRiskService { get; set; } = default!;

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? TeamMemberRiskId { get; set; }

    bool _editing;
    TeamMemberRisk? _draft;

    TeamMember? Member =>
        Guid.TryParse(MemberId, out Guid id) ? Cache.Team.FirstOrDefault(m => m.Id == id) : null;

    TeamMemberRisk? View
    {
        get
        {
            if (!Guid.TryParse(TeamMemberRiskId, out Guid rid)) return null;
            TeamMemberRisk? r = Cache.TeamMemberRisks.FirstOrDefault(x => x.Id == rid);
            if (r is null) return null;
            if (Guid.TryParse(MemberId, out Guid mid) && r.MemberId != mid) return null;
            return r;
        }
    }

    Risk? LinkedGlobal =>
        View?.LinkedRiskId is { } lid ? Cache.Risks.FirstOrDefault(r => r.Id == lid) : null;

    string LinkedRiskValue =>
        (_editing && _draft is not null ? _draft.LinkedRiskId : View?.LinkedRiskId)?.ToString() ?? "";

    string ReviewedLabel
    {
        get
        {
            int? days = DisplayLabels.DaysSince((_editing && _draft is not null ? _draft : View)?.LastReviewedIso);
            if (days is null) return "—";
            if (days == 0) return "today";
            return days == 1 ? "1 day ago" : $"{days} days ago";
        }
    }

    protected override void OnInitialized()
    {
        Cache.Changed += OnChanged;
        Ai.SetContext("Context: Team Member Risk Detail",
        [
            new AiAction("summarize-risk", "Summarize this risk"),
            new AiAction("suggest-mitigation", "Suggest mitigation experiments"),
        ]);
        _ = Cache.EnsureHydratedAsync();
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
                Nav.NavigateTo("/team", replace: true);
        }
    }

    void BackToRisks() => Nav.NavigateTo($"/team/{MemberId}/risks");
    void BackToMember() => Nav.NavigateTo($"/team/{MemberId}");

    void OnDraftFirstNoticed(ChangeEventArgs e)
    {
        if (_draft is null) return;
        _draft.FirstNoticedDateIso = e.Value?.ToString() ?? "";
    }

    void BeginEdit()
    {
        if (View is null) return;
        _draft = Clone(View);
        _editing = true;
    }

    void CancelEdit()
    {
        _editing = false;
        _draft = null;
    }

    void SaveEdit()
    {
        if (_draft is null) return;
        PersistRisk(_draft);
        _editing = false;
        _draft = null;
    }

    void OnLinkedRiskChange(ChangeEventArgs e)
    {
        Guid? next = Guid.TryParse(e.Value?.ToString(), out Guid id) ? id : null;
        if (_editing && _draft is not null)
        {
            _draft.LinkedRiskId = next;
            return;
        }

        if (View is null) return;
        TeamMemberRisk updated = Clone(View);
        updated.LinkedRiskId = next;
        PersistRisk(updated);
    }

    void MarkReviewed()
    {
        var iso = DateTimeOffset.UtcNow.ToString("o");
        if (_editing && _draft is not null)
        {
            _draft.LastReviewedIso = iso;
            return;
        }

        if (View is null) return;
        TeamMemberRisk updated = Clone(View);
        updated.LastReviewedIso = iso;
        PersistRisk(updated);
    }

    void OpenLinkedGlobal()
    {
        if (LinkedGlobal is null) return;
        Selection.SelectRisk(LinkedGlobal.Id);
        Nav.NavigateTo("/risks");
    }

    void PersistRisk(TeamMemberRisk next)
    {
        if (!Guid.TryParse(MemberId, out Guid memberId)) return;
        Cache.UpdateTeamMemberRisk(next);
        _ = PersistAsync(memberId, next);
    }

    async Task PersistAsync(Guid memberId, TeamMemberRisk next)
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

    static TeamMemberRisk Clone(TeamMemberRisk r) => new()
    {
        Id = r.Id,
        MemberId = r.MemberId,
        Title = r.Title,
        Severity = r.Severity,
        RiskType = r.RiskType,
        Status = r.Status,
        Trend = r.Trend,
        FirstNoticedDateIso = r.FirstNoticedDateIso,
        ImpactArea = r.ImpactArea,
        Description = r.Description,
        CurrentAction = r.CurrentAction,
        LastReviewedIso = r.LastReviewedIso,
        LinkedRiskId = r.LinkedRiskId
    };

    void OnChanged() => InvokeAsync(() =>
    {
        if (Guid.TryParse(MemberId, out Guid id) && Cache.TeamReady && Cache.Team.All(m => m.Id != id))
        {
            Nav.NavigateTo("/team", replace: true);
            return;
        }

        StateHasChanged();
    });

    public void Dispose() => Cache.Changed -= OnChanged;
}
