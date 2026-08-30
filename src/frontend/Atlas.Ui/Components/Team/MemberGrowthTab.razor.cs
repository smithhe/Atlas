using Microsoft.AspNetCore.Components;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberGrowthTab : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private GrowthService GrowthService { get; set; } = null!;

    [Parameter, EditorRequired] public TeamMember Member { get; set; } = null!;

    private static readonly string[] SkillLevels = ["Awareness", "Beginner", "Developing", "Intermediate", "Advanced", "Expert"];

    private bool _goalOpen, _skillOpen, _themeOpen, _focusOpen, _themeIsNew, _retrying, _disposed;
    private int? _skillIndex;
    private Guid? _themeId;
    private string _goalTitle = "", _goalDesc = "", _goalCategory = "", _goalPriority = "", _goalStart = "", _goalTarget = "";
    private GrowthGoalStatus _goalStatus = GrowthGoalStatus.OnTrack;
    private string _skillLabel = "", _skillFrom = "", _skillTo = "";
    private string _themeTitle = "", _themeDesc = "", _themeObserved = "", _focusDraft = "";

    private Growth? Growth => Cache.GetGrowth(Member.Id);
    private IReadOnlyList<GrowthGoal> ActiveGoals => Growth?.Goals ?? Array.Empty<GrowthGoal>();
    private IReadOnlyList<string> Skills => Growth?.SkillsInProgress ?? Array.Empty<string>();
    private IReadOnlyList<GrowthFeedbackTheme> Themes => Growth?.FeedbackThemes ?? Array.Empty<GrowthFeedbackTheme>();
    private string FocusAreasMarkdown => Growth?.FocusAreasMarkdown ?? "";

    private GrowthLoadStatus _loadStatus;
    private string? _loadError;
    private Guid _growthMemberId;
    private bool _memberInitializing;

    private string FormattedSkillText => GrowthUiHelpers.FormatSkillText(_skillLabel, _skillFrom, _skillTo);

    private string GoalValidationMessage => GrowthUiHelpers.ValidateGoalAdd(_goalTitle, _goalDesc, _goalCategory, _goalStart, _goalTarget) ?? "";

    private bool CanSaveGoal => string.IsNullOrEmpty(GoalValidationMessage);

    private bool CanSaveSkill =>
        !string.IsNullOrWhiteSpace(_skillLabel)
        && FormattedSkillText.Length <= GrowthUiHelpers.MaxSkillLength
        && !GrowthUiHelpers.HasSkillDuplicate(Skills, FormattedSkillText, _skillIndex);

    private bool CanSaveTheme => GrowthUiHelpers.ValidateThemeFields(_themeTitle, _themeDesc, _themeObserved) is null;

    private string FocusValidationMessage => GrowthUiHelpers.ValidateFocusMarkdown(_focusDraft) ?? "";

    private bool CanSaveFocus => string.IsNullOrEmpty(FocusValidationMessage);

    private string SkillValidationMessage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_skillLabel))
            {
                if (!string.IsNullOrWhiteSpace(_skillFrom) || !string.IsNullOrWhiteSpace(_skillTo))
                {
                    return "Skill label is required.";
                }

                return "";
            }

            if (FormattedSkillText.Length > GrowthUiHelpers.MaxSkillLength)
            {
                return $"Skill text cannot exceed {GrowthUiHelpers.MaxSkillLength} characters.";
            }

            if (GrowthUiHelpers.HasSkillDuplicate(Skills, FormattedSkillText, _skillIndex))
            {
                return "This skill already exists.";
            }

            return "";
        }
    }

    private string ThemeValidationMessage
    {
        get
        {
            var error = GrowthUiHelpers.ValidateThemeFields(_themeTitle, _themeDesc, _themeObserved);
            if (error is null)
            {
                return "";
            }

            if (!string.IsNullOrWhiteSpace(_themeTitle) || !string.IsNullOrWhiteSpace(_themeDesc) || !string.IsNullOrWhiteSpace(_themeObserved) || !_themeIsNew)
            {
                return error;
            }

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
        {
            _memberInitializing = false;
        }
    }

    private void ResetMemberState()
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

    private void OnChanged()
    {
        if (_disposed)
        {
            return;
        }

        _loadStatus = Cache.GetGrowthLoadStatus(Member.Id);
        _loadError = Cache.GetGrowthLoadError(Member.Id);
        if (_loadStatus is GrowthLoadStatus.Succeeded or GrowthLoadStatus.Failed)
        {
            _memberInitializing = false;
        }

        _ = InvokeAsync(StateHasChanged);
    }

    private async Task RetryLoad()
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
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task<Guid?> RequireGrowthIdAsync(string operation)
    {
        Guid growthId = await Cache.EnsureGrowthIdAsync(Member.Id);
        if (growthId != Guid.Empty)
        {
            return growthId;
        }

        await Dialogs.AlertAsync(GrowthUiHelpers.GrowthUnavailableMessage(operation));
        return null;
    }

    private Growth BaseGrowth(Guid? knownId = null)
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

    private void CommitGrowth(Growth next) => Cache.UpdateGrowth(next);

    private void OpenAddGoal()
    {
        _goalTitle = _goalDesc = _goalCategory = _goalPriority = _goalTarget = "";
        _goalStatus = GrowthGoalStatus.OnTrack;
        _goalStart = DisplayLabels.TodayIsoDateLocal();
        _goalOpen = true;
    }

    private void OnGoalStartChange(ChangeEventArgs e) => _goalStart = e.Value?.ToString() ?? "";

    private void OnGoalTargetChange(ChangeEventArgs e) => _goalTarget = e.Value?.ToString() ?? "";

    private void OpenGoal(Guid goalId) => Nav.NavigateTo($"/team/{Member.Id}/growth/goals/{goalId}");

    private void CloseGoalModal() => _goalOpen = false;

    private async Task SaveGoal()
    {
        if (!CanSaveGoal)
        {
            return;
        }

        var title = _goalTitle.Trim();
        try
        {
            Guid? growthId = await RequireGrowthIdAsync("add a growth goal");
            if (growthId is not { } gid)
            {
                return;
            }

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

    private void OpenAddSkill()
    {
        _skillIndex = null;
        _skillLabel = _skillFrom = _skillTo = "";
        _skillOpen = true;
    }

    private void OpenEditSkill(int idx)
    {
        var text = Skills.ElementAtOrDefault(idx) ?? "";
        (string Label, string From, string To) parsed = GrowthUiHelpers.ParseSkillText(text);
        _skillIndex = idx;
        _skillLabel = parsed.Label;
        _skillFrom = parsed.From;
        _skillTo = parsed.To;
        _skillOpen = true;
    }

    private void CloseSkillModal()
    {
        _skillOpen = false;
        _skillIndex = null;
        _skillLabel = _skillFrom = _skillTo = "";
    }

    private async Task SaveSkill()
    {
        if (!CanSaveSkill)
        {
            return;
        }

        var nextText = FormattedSkillText;
        try
        {
            Growth g = BaseGrowth();
            var skills = g.SkillsInProgress.ToList();
            if (_skillIndex is null)
            {
                skills.Add(nextText);
            }
            else
            {
                skills[_skillIndex.Value] = nextText;
            }

            var normalized = skills.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Guid? growthId = await RequireGrowthIdAsync("save skills");
            if (growthId is not { } gid)
            {
                return;
            }

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

    private async Task DeleteSkill()
    {
        if (_skillIndex is null)
        {
            return;
        }

        try
        {
            Growth g = BaseGrowth();
            var skills = g.SkillsInProgress.Where((_, i) => i != _skillIndex.Value).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Guid? growthId = await RequireGrowthIdAsync("remove skill");
            if (growthId is not { } gid)
            {
                return;
            }

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

    private void OpenAddTheme()
    {
        _themeIsNew = true;
        _themeId = null;
        _themeTitle = _themeDesc = _themeObserved = "";
        _themeOpen = true;
    }

    private void OpenEditTheme(Guid themeId)
    {
        GrowthFeedbackTheme? t = Themes.FirstOrDefault(x => x.Id == themeId);
        if (t is null)
        {
            return;
        }

        _themeIsNew = false;
        _themeId = t.Id;
        _themeTitle = t.Title;
        _themeDesc = t.Description;
        _themeObserved = t.ObservedSinceLabel ?? "";
        _themeOpen = true;
    }

    private void CloseThemeModal()
    {
        _themeOpen = false;
        _themeId = null;
        _themeIsNew = false;
    }

    private async Task SaveTheme()
    {
        if (!CanSaveTheme)
        {
            return;
        }

        var title = _themeTitle.Trim();
        var description = _themeDesc.Trim();
        var observed = string.IsNullOrWhiteSpace(_themeObserved) ? null : _themeObserved.Trim();

        try
        {
            Guid? growthId = await RequireGrowthIdAsync("save feedback theme");
            if (growthId is not { } gid)
            {
                return;
            }

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

    private async Task DeleteTheme()
    {
        if (_themeId is not { } themeId)
        {
            return;
        }

        try
        {
            Guid? growthId = await RequireGrowthIdAsync("delete feedback theme");
            if (growthId is not { } gid)
            {
                return;
            }

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

    private void OpenEditFocusAreas()
    {
        _focusDraft = FocusAreasMarkdown;
        _focusOpen = true;
    }

    private async Task SaveFocusAreas()
    {
        if (!CanSaveFocus)
        {
            return;
        }

        try
        {
            Guid? growthId = await RequireGrowthIdAsync("save focus areas");
            if (growthId is not { } gid)
            {
                return;
            }

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
