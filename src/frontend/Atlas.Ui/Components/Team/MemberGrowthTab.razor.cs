using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberGrowthTab : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] GrowthService GrowthService { get; set; } = default!;

    [Parameter, EditorRequired] public TeamMember Member { get; set; } = default!;

    static readonly string[] SkillLevels = ["Awareness", "Beginner", "Developing", "Intermediate", "Advanced", "Expert"];

    bool _goalOpen, _skillOpen, _themeOpen, _focusOpen, _themeIsNew, _retrying, _disposed;
    int? _skillIndex;
    Guid? _themeId;
    string _goalTitle = "", _goalDesc = "", _goalCategory = "", _goalPriority = "", _goalStart = "", _goalTarget = "";
    GrowthGoalStatus _goalStatus = GrowthGoalStatus.OnTrack;
    string _skillLabel = "", _skillFrom = "", _skillTo = "";
    string _themeTitle = "", _themeDesc = "", _themeObserved = "", _focusDraft = "";

    Growth? Growth => Cache.GetGrowth(Member.Id);
    IReadOnlyList<GrowthGoal> ActiveGoals => Growth?.Goals ?? Array.Empty<GrowthGoal>();
    IReadOnlyList<string> Skills => Growth?.SkillsInProgress ?? Array.Empty<string>();
    IReadOnlyList<GrowthFeedbackTheme> Themes => Growth?.FeedbackThemes ?? Array.Empty<GrowthFeedbackTheme>();
    string FocusAreasMarkdown => Growth?.FocusAreasMarkdown ?? "";

    GrowthLoadStatus _loadStatus;
    string? _loadError;
    Guid _growthMemberId;
    bool _memberInitializing;

    string FormattedSkillText => GrowthUiHelpers.FormatSkillText(_skillLabel, _skillFrom, _skillTo);

    string GoalValidationMessage => GrowthUiHelpers.ValidateGoalAdd(_goalTitle, _goalDesc, _goalCategory, _goalStart, _goalTarget) ?? "";

    bool CanSaveGoal => string.IsNullOrEmpty(GoalValidationMessage);

    bool CanSaveSkill =>
        !string.IsNullOrWhiteSpace(_skillLabel)
        && FormattedSkillText.Length <= GrowthUiHelpers.MaxSkillLength
        && !GrowthUiHelpers.HasSkillDuplicate(Skills, FormattedSkillText, _skillIndex);

    bool CanSaveTheme => GrowthUiHelpers.ValidateThemeFields(_themeTitle, _themeDesc, _themeObserved) is null;

    string FocusValidationMessage => GrowthUiHelpers.ValidateFocusMarkdown(_focusDraft) ?? "";

    bool CanSaveFocus => string.IsNullOrEmpty(FocusValidationMessage);

    string SkillValidationMessage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_skillLabel))
            {
                if (!string.IsNullOrWhiteSpace(_skillFrom) || !string.IsNullOrWhiteSpace(_skillTo))
                    return "Skill label is required.";
                return "";
            }

            if (FormattedSkillText.Length > GrowthUiHelpers.MaxSkillLength)
                return $"Skill text cannot exceed {GrowthUiHelpers.MaxSkillLength} characters.";
            if (GrowthUiHelpers.HasSkillDuplicate(Skills, FormattedSkillText, _skillIndex))
                return "This skill already exists.";
            return "";
        }
    }

    string ThemeValidationMessage
    {
        get
        {
            string? error = GrowthUiHelpers.ValidateThemeFields(_themeTitle, _themeDesc, _themeObserved);
            if (error is null)
                return "";
            if (!string.IsNullOrWhiteSpace(_themeTitle) || !string.IsNullOrWhiteSpace(_themeDesc) || !string.IsNullOrWhiteSpace(_themeObserved) || !_themeIsNew)
                return error;
            return "";
        }
    }

    protected override void OnInitialized() => Cache.Changed += OnChanged;

    protected override void OnParametersSet()
    {
        if (Member.Id != _growthMemberId)
        {
            ResetMemberState();
            _growthMemberId = Member.Id;
            _memberInitializing = true;
            _ = Cache.EnsureGrowthLoadedAsync(Member.Id);
        }

        _loadStatus = Cache.GetGrowthLoadStatus(Member.Id);
        _loadError = Cache.GetGrowthLoadError(Member.Id);
        if (_loadStatus is GrowthLoadStatus.Succeeded or GrowthLoadStatus.Failed)
            _memberInitializing = false;
    }

    void ResetMemberState()
    {
        _goalOpen = _skillOpen = _themeOpen = _focusOpen = false;
        _themeIsNew = false;
        _retrying = false;
        _memberInitializing = true;
        _skillIndex = null;
        _themeId = null;
        _goalTitle = _goalDesc = _goalCategory = _goalPriority = _goalStart = _goalTarget = "";
        _goalStatus = GrowthGoalStatus.OnTrack;
        _skillLabel = _skillFrom = _skillTo = "";
        _themeTitle = _themeDesc = _themeObserved = "";
        _focusDraft = "";
    }

    void OnChanged()
    {
        if (_disposed)
            return;

        _loadStatus = Cache.GetGrowthLoadStatus(Member.Id);
        _loadError = Cache.GetGrowthLoadError(Member.Id);
        if (_loadStatus is GrowthLoadStatus.Succeeded or GrowthLoadStatus.Failed)
            _memberInitializing = false;
        _ = InvokeAsync(StateHasChanged);
    }

    async Task RetryLoad()
    {
        _retrying = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            await Cache.RetryGrowthLoadAsync(Member.Id);
        }
        finally
        {
            _retrying = false;
            _loadStatus = Cache.GetGrowthLoadStatus(Member.Id);
            _loadError = Cache.GetGrowthLoadError(Member.Id);
            if (!_disposed)
                await InvokeAsync(StateHasChanged);
        }
    }

    async Task<Guid?> RequireGrowthIdAsync(string operation)
    {
        Guid growthId = await Cache.EnsureGrowthIdAsync(Member.Id);
        if (growthId != Guid.Empty)
            return growthId;

        await Dialogs.AlertAsync(GrowthUiHelpers.GrowthUnavailableMessage(operation));
        return null;
    }

    Growth BaseGrowth(Guid? knownId = null)
    {
        Growth? g = Growth;
        Guid id = knownId is { } kid && kid != Guid.Empty
            ? kid
            : g?.Id ?? Guid.Empty;
        if (g is null)
        {
            return new Growth
            {
                Id = id,
                MemberId = Member.Id,
                Goals = Array.Empty<GrowthGoal>(),
                SkillsInProgress = Array.Empty<string>(),
                FeedbackThemes = Array.Empty<GrowthFeedbackTheme>(),
                FocusAreasMarkdown = ""
            };
        }

        return new Growth
        {
            Id = id != Guid.Empty ? id : g.Id,
            MemberId = Member.Id,
            Goals = g.Goals ?? Array.Empty<GrowthGoal>(),
            SkillsInProgress = g.SkillsInProgress ?? Array.Empty<string>(),
            FeedbackThemes = g.FeedbackThemes ?? Array.Empty<GrowthFeedbackTheme>(),
            FocusAreasMarkdown = g.FocusAreasMarkdown ?? ""
        };
    }

    void CommitGrowth(Growth next) => Cache.UpdateGrowth(next);

    void OpenAddGoal()
    {
        _goalTitle = _goalDesc = _goalCategory = _goalPriority = _goalTarget = "";
        _goalStatus = GrowthGoalStatus.OnTrack;
        _goalStart = DisplayLabels.TodayIsoDateLocal();
        _goalOpen = true;
    }

    void OnGoalStartChange(ChangeEventArgs e) => _goalStart = e.Value?.ToString() ?? "";

    void OnGoalTargetChange(ChangeEventArgs e) => _goalTarget = e.Value?.ToString() ?? "";

    void OpenGoal(Guid goalId) => Nav.NavigateTo($"/team/{Member.Id}/growth/goals/{goalId}");

    void CloseGoalModal() => _goalOpen = false;

    async Task SaveGoal()
    {
        if (!CanSaveGoal)
            return;

        string title = _goalTitle.Trim();
        try
        {
            Guid? growthId = await RequireGrowthIdAsync("add a growth goal");
            if (growthId is not { } gid)
                return;

            Priority? priority = Enum.TryParse(_goalPriority, out Priority p) ? p : null;
            AtlasApiDTOsGrowthGoalsAddGrowthGoalResponse res = await GrowthService.AddGoalAsync(gid, new AtlasApiDTOsGrowthGoalsAddGrowthGoalRequest
            {
                Title = title,
                Description = _goalDesc.Trim(),
                Status = ApiMappers.ToApiGrowthGoalStatus(_goalStatus),
                Category = string.IsNullOrWhiteSpace(_goalCategory) ? null : _goalCategory.Trim(),
                Priority = priority is null ? null : ApiMappers.ToApiPriority(priority.Value),
                StartDate = DateTimeOffset.TryParse(_goalStart, out DateTimeOffset sd) ? sd : null,
                TargetDate = DateTimeOffset.TryParse(_goalTarget, out DateTimeOffset td) ? td : null
            });
            if (!GrowthUiHelpers.IsValidCreatedId(res.Id))
            {
                await Cache.RetryGrowthLoadAsync(Member.Id);
                await Dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("goal"));
                return;
            }

            Guid goalId = res.Id!.Value;
            var nextGoal = new GrowthGoal
            {
                Id = goalId,
                Title = title,
                Description = _goalDesc.Trim(),
                Status = _goalStatus,
                Category = string.IsNullOrWhiteSpace(_goalCategory) ? null : _goalCategory.Trim(),
                Priority = priority,
                StartDateIso = string.IsNullOrWhiteSpace(_goalStart) ? null : _goalStart,
                TargetDateIso = string.IsNullOrWhiteSpace(_goalTarget) ? null : _goalTarget,
                LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o"),
                Actions = Array.Empty<GrowthGoalAction>(),
                CheckIns = Array.Empty<GrowthGoalCheckIn>(),
                SuccessCriteria = Array.Empty<string>()
            };
            Growth g = BaseGrowth(gid);
            CommitGrowth(new Growth
            {
                Id = gid,
                MemberId = Member.Id,
                Goals = new[] { nextGoal }.Concat(g.Goals).ToList(),
                SkillsInProgress = g.SkillsInProgress,
                FeedbackThemes = g.FeedbackThemes,
                FocusAreasMarkdown = g.FocusAreasMarkdown
            });
            CloseGoalModal();
            Nav.NavigateTo($"/team/{Member.Id}/growth/goals/{nextGoal.Id}");
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add growth goal.", ex));
        }
    }

    void OpenAddSkill()
    {
        _skillIndex = null;
        _skillLabel = _skillFrom = _skillTo = "";
        _skillOpen = true;
    }

    void OpenEditSkill(int idx)
    {
        string text = Skills.ElementAtOrDefault(idx) ?? "";
        (string Label, string From, string To) parsed = GrowthUiHelpers.ParseSkillText(text);
        _skillIndex = idx;
        _skillLabel = parsed.Label;
        _skillFrom = parsed.From;
        _skillTo = parsed.To;
        _skillOpen = true;
    }

    void CloseSkillModal()
    {
        _skillOpen = false;
        _skillIndex = null;
        _skillLabel = _skillFrom = _skillTo = "";
    }

    async Task SaveSkill()
    {
        if (!CanSaveSkill)
            return;

        string nextText = FormattedSkillText;
        try
        {
            Growth g = BaseGrowth();
            List<string> skills = g.SkillsInProgress.ToList();
            if (_skillIndex is null) skills.Add(nextText);
            else skills[_skillIndex.Value] = nextText;
            List<string> normalized = skills.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Guid? growthId = await RequireGrowthIdAsync("save skills");
            if (growthId is not { } gid)
                return;

            await GrowthService.SetSkillsInProgressAsync(gid, normalized);
            CommitGrowth(new Growth
            {
                Id = gid,
                MemberId = Member.Id,
                Goals = g.Goals,
                SkillsInProgress = normalized,
                FeedbackThemes = g.FeedbackThemes,
                FocusAreasMarkdown = g.FocusAreasMarkdown
            });
            CloseSkillModal();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save skills.", ex));
        }
    }

    async Task DeleteSkill()
    {
        if (_skillIndex is null) return;
        try
        {
            Growth g = BaseGrowth();
            List<string> skills = g.SkillsInProgress.Where((_, i) => i != _skillIndex.Value).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Guid? growthId = await RequireGrowthIdAsync("remove skill");
            if (growthId is not { } gid)
                return;

            await GrowthService.SetSkillsInProgressAsync(gid, skills);
            CommitGrowth(new Growth
            {
                Id = gid,
                MemberId = Member.Id,
                Goals = g.Goals,
                SkillsInProgress = skills,
                FeedbackThemes = g.FeedbackThemes,
                FocusAreasMarkdown = g.FocusAreasMarkdown
            });
            CloseSkillModal();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to remove skill.", ex));
        }
    }

    void OpenAddTheme()
    {
        _themeIsNew = true;
        _themeId = null;
        _themeTitle = _themeDesc = _themeObserved = "";
        _themeOpen = true;
    }

    void OpenEditTheme(Guid themeId)
    {
        GrowthFeedbackTheme? t = Themes.FirstOrDefault(x => x.Id == themeId);
        if (t is null) return;
        _themeIsNew = false;
        _themeId = t.Id;
        _themeTitle = t.Title;
        _themeDesc = t.Description;
        _themeObserved = t.ObservedSinceLabel ?? "";
        _themeOpen = true;
    }

    void CloseThemeModal()
    {
        _themeOpen = false;
        _themeId = null;
        _themeIsNew = false;
    }

    async Task SaveTheme()
    {
        if (!CanSaveTheme)
            return;

        string title = _themeTitle.Trim();
        string description = _themeDesc.Trim();
        string? observed = string.IsNullOrWhiteSpace(_themeObserved) ? null : _themeObserved.Trim();

        try
        {
            Guid? growthId = await RequireGrowthIdAsync("save feedback theme");
            if (growthId is not { } gid)
                return;

            Growth g = BaseGrowth(gid);
            if (_themeIsNew)
            {
                AtlasApiDTOsGrowthFeedbackThemesAddFeedbackThemeResponse res = await GrowthService.AddFeedbackThemeAsync(gid, new AtlasApiDTOsGrowthFeedbackThemesAddFeedbackThemeRequest
                {
                    Title = title,
                    Description = description,
                    ObservedSinceLabel = observed
                });
                if (!GrowthUiHelpers.IsValidCreatedId(res.Id))
                {
                    await Cache.RetryGrowthLoadAsync(Member.Id);
                    await Dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("feedback theme"));
                    return;
                }

                Guid id = res.Id!.Value;
                CommitGrowth(new Growth
                {
                    Id = gid,
                    MemberId = Member.Id,
                    Goals = g.Goals,
                    SkillsInProgress = g.SkillsInProgress,
                    FeedbackThemes = g.FeedbackThemes.Append(new GrowthFeedbackTheme
                    {
                        Id = id,
                        Title = title,
                        Description = description,
                        ObservedSinceLabel = observed
                    }).ToList(),
                    FocusAreasMarkdown = g.FocusAreasMarkdown
                });
            }
            else if (_themeId is { } tid)
            {
                await GrowthService.UpdateFeedbackThemeAsync(gid, tid, new AtlasApiDTOsGrowthFeedbackThemesUpdateFeedbackThemeRequest
                {
                    Title = title,
                    Description = description,
                    ObservedSinceLabel = observed
                });
                CommitGrowth(new Growth
                {
                    Id = gid,
                    MemberId = Member.Id,
                    Goals = g.Goals,
                    SkillsInProgress = g.SkillsInProgress,
                    FeedbackThemes = g.FeedbackThemes.Select(t => t.Id == tid
                        ? new GrowthFeedbackTheme { Id = tid, Title = title, Description = description, ObservedSinceLabel = observed }
                        : t).ToList(),
                    FocusAreasMarkdown = g.FocusAreasMarkdown
                });
            }
            else
            {
                await Dialogs.AlertAsync("This theme is no longer available. Refreshing growth data.");
                await Cache.RetryGrowthLoadAsync(Member.Id);
                return;
            }

            CloseThemeModal();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save feedback theme.", ex));
        }
    }

    async Task DeleteTheme()
    {
        if (_themeId is not { } themeId) return;
        try
        {
            Guid? growthId = await RequireGrowthIdAsync("delete feedback theme");
            if (growthId is not { } gid)
                return;

            Growth g = BaseGrowth(gid);
            await GrowthService.DeleteFeedbackThemeAsync(gid, themeId);
            CommitGrowth(new Growth
            {
                Id = gid,
                MemberId = Member.Id,
                Goals = g.Goals,
                SkillsInProgress = g.SkillsInProgress,
                FeedbackThemes = g.FeedbackThemes.Where(t => t.Id != themeId).ToList(),
                FocusAreasMarkdown = g.FocusAreasMarkdown
            });
            CloseThemeModal();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to delete feedback theme.", ex));
        }
    }

    void OpenEditFocusAreas()
    {
        _focusDraft = FocusAreasMarkdown;
        _focusOpen = true;
    }

    async Task SaveFocusAreas()
    {
        if (!CanSaveFocus)
            return;

        try
        {
            Guid? growthId = await RequireGrowthIdAsync("save focus areas");
            if (growthId is not { } gid)
                return;

            Growth g = BaseGrowth(gid);
            await GrowthService.UpdateFocusAreasAsync(gid, _focusDraft);
            CommitGrowth(new Growth
            {
                Id = gid,
                MemberId = Member.Id,
                Goals = g.Goals,
                SkillsInProgress = g.SkillsInProgress,
                FeedbackThemes = g.FeedbackThemes,
                FocusAreasMarkdown = _focusDraft
            });
            _focusOpen = false;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save focus areas.", ex));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Cache.Changed -= OnChanged;
    }
}
