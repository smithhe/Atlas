using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Team
{
    public partial class MemberGrowthTab : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IGrowthService _growthService { get; set; } = null!;

        [Parameter, EditorRequired] public TeamMember Member { get; set; } = null!;

        private static readonly string[] SkillLevels = ["Awareness", "Beginner", "Developing", "Intermediate", "Advanced", "Expert"];

        private bool GoalOpen { get; set; }
        private bool SkillOpen { get; set; }
        private bool ThemeOpen { get; set; }
        private bool FocusOpen { get; set; }
        private bool ThemeIsNew { get; set; }
        private bool Retrying { get; set; }
        private bool Disposed { get; set; }
        private int? SkillIndex { get; set; }
        private Guid? ThemeId { get; set; }
        private string GoalTitle { get; set; } = "";
        private string GoalDesc { get; set; } = "";
        private string GoalCategory { get; set; } = "";
        private string GoalPriority { get; set; } = "";
        private string GoalStart { get; set; } = "";
        private string GoalTarget { get; set; } = "";
        private GrowthGoalStatus GoalStatus { get; set; } = GrowthGoalStatus.OnTrack;
        private string SkillLabel { get; set; } = "";
        private string SkillFrom { get; set; } = "";
        private string SkillTo { get; set; } = "";
        private string ThemeTitle { get; set; } = "";
        private string ThemeDesc { get; set; } = "";
        private string ThemeObserved { get; set; } = "";
        private string FocusDraft { get; set; } = "";

        private Growth? Growth => this._cache.GetGrowth(Member.Id);
        private IReadOnlyList<GrowthGoal> ActiveGoals => Growth?.Goals ?? Array.Empty<GrowthGoal>();
        private IReadOnlyList<string> Skills => Growth?.SkillsInProgress ?? Array.Empty<string>();
        private IReadOnlyList<GrowthFeedbackTheme> Themes => Growth?.FeedbackThemes ?? Array.Empty<GrowthFeedbackTheme>();
        private string FocusAreasMarkdown => Growth?.FocusAreasMarkdown ?? "";

        private GrowthLoadStatus LoadStatus { get; set; }
        private string? LoadError { get; set; }
        private Guid GrowthMemberId { get; set; }
        private bool MemberInitializing { get; set; }

        private string FormattedSkillText => GrowthUiHelpers.FormatSkillText(this.SkillLabel, this.SkillFrom, this.SkillTo);

        private string GoalValidationMessage => GrowthUiHelpers.ValidateGoalAdd(this.GoalTitle, this.GoalDesc, this.GoalCategory, this.GoalStart, this.GoalTarget) ?? "";

        private bool CanSaveGoal => string.IsNullOrEmpty(GoalValidationMessage);

        private bool CanSaveSkill =>
            !string.IsNullOrWhiteSpace(this.SkillLabel)
            && FormattedSkillText.Length <= GrowthUiHelpers.MaxSkillLength
            && !GrowthUiHelpers.HasSkillDuplicate(Skills, FormattedSkillText, this.SkillIndex);

        private bool CanSaveTheme => GrowthUiHelpers.ValidateThemeFields(this.ThemeTitle, this.ThemeDesc, this.ThemeObserved) is null;

        private string FocusValidationMessage => GrowthUiHelpers.ValidateFocusMarkdown(this.FocusDraft) ?? "";

        private bool CanSaveFocus => string.IsNullOrEmpty(FocusValidationMessage);

        private string SkillValidationMessage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.SkillLabel))
                {
                    if (!string.IsNullOrWhiteSpace(this.SkillFrom) || !string.IsNullOrWhiteSpace(this.SkillTo))
                    {
                        return "Skill label is required.";
                    }

                    return "";
                }

                if (FormattedSkillText.Length > GrowthUiHelpers.MaxSkillLength)
                {
                    return $"Skill text cannot exceed {GrowthUiHelpers.MaxSkillLength} characters.";
                }

                if (GrowthUiHelpers.HasSkillDuplicate(Skills, FormattedSkillText, this.SkillIndex))
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
                var error = GrowthUiHelpers.ValidateThemeFields(this.ThemeTitle, this.ThemeDesc, this.ThemeObserved);
                if (error is null)
                {
                    return "";
                }

                if (!string.IsNullOrWhiteSpace(this.ThemeTitle) || !string.IsNullOrWhiteSpace(this.ThemeDesc) || !string.IsNullOrWhiteSpace(this.ThemeObserved) || !this.ThemeIsNew)
                {
                    return error;
                }

                return "";
            }
        }

        protected override void OnInitialized() => this._cache.Changed += OnChangedAsync;

        protected override async Task OnParametersSetAsync()
        {
            if (Member.Id != this.GrowthMemberId)
            {
                ResetMemberState();
                this.GrowthMemberId = Member.Id;
                this.MemberInitializing = true;
                await this._cache.EnsureGrowthLoadedAsync(Member.Id);
            }

            this.LoadStatus = this._cache.GetGrowthLoadStatus(Member.Id);
            this.LoadError = this._cache.GetGrowthLoadError(Member.Id);
            if (this.LoadStatus is GrowthLoadStatus.Succeeded or GrowthLoadStatus.Failed)
            {
                this.MemberInitializing = false;
            }
        }

        private void ResetMemberState()
        {
            this.GoalOpen = this.SkillOpen = this.ThemeOpen = this.FocusOpen = false;
            this.ThemeIsNew = false;
            this.Retrying = false;
            this.MemberInitializing = true;
            this.SkillIndex = null;
            this.ThemeId = null;
            this.GoalTitle = this.GoalDesc = this.GoalCategory = this.GoalPriority = this.GoalStart = this.GoalTarget = "";
            this.GoalStatus = GrowthGoalStatus.OnTrack;
            this.SkillLabel = this.SkillFrom = this.SkillTo = "";
            this.ThemeTitle = this.ThemeDesc = this.ThemeObserved = "";
            this.FocusDraft = "";
        }

        private async void OnChangedAsync()
        {
            if (this.Disposed)
            {
                return;
            }

            try
            {
                await InvokeAsync(() =>
                {
                    this.LoadStatus = this._cache.GetGrowthLoadStatus(Member.Id);
                    this.LoadError = this._cache.GetGrowthLoadError(Member.Id);
                    if (this.LoadStatus is GrowthLoadStatus.Succeeded or GrowthLoadStatus.Failed)
                    {
                        this.MemberInitializing = false;
                    }

                    StateHasChanged();
                });
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        }

        private async Task RetryLoad()
        {
            this.Retrying = true;
            await InvokeAsync(StateHasChanged);
            try
            {
                await this._cache.RetryGrowthLoadAsync(Member.Id);
            }
            finally
            {
                this.Retrying = false;
                this.LoadStatus = this._cache.GetGrowthLoadStatus(Member.Id);
                this.LoadError = this._cache.GetGrowthLoadError(Member.Id);
                if (!this.Disposed)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private async Task<Guid?> RequireGrowthIdAsync(string operation)
        {
            Guid growthId = await this._cache.EnsureGrowthIdAsync(Member.Id);
            if (growthId != Guid.Empty)
            {
                return growthId;
            }

            await this._dialogs.AlertAsync(GrowthUiHelpers.GrowthUnavailableMessage(operation));
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
                return EntityClone.Growth(new Growth
                {
                    Id = id,
                    MemberId = Member.Id
                });
            }

            return EntityClone.Growth(g, id: id != Guid.Empty ? id : g.Id, memberId: Member.Id);
        }

        private void OpenAddGoal()
        {
            this.GoalTitle = this.GoalDesc = this.GoalCategory = this.GoalPriority = this.GoalTarget = "";
            this.GoalStatus = GrowthGoalStatus.OnTrack;
            this.GoalStart = DisplayLabels.TodayIsoDateLocal();
            this.GoalOpen = true;
        }

        private void OnGoalStartChange(ChangeEventArgs e) => this.GoalStart = e.Value?.ToString() ?? "";

        private void OnGoalTargetChange(ChangeEventArgs e) => this.GoalTarget = e.Value?.ToString() ?? "";

        private void OpenGoal(Guid goalId) => this._nav.NavigateTo($"/team/{Member.Id}/growth/goals/{goalId}");

        private void CloseGoalModal() => this.GoalOpen = false;

        private async Task SaveGoal()
        {
            if (!CanSaveGoal)
            {
                return;
            }

            var title = this.GoalTitle.Trim();
            try
            {
                Guid? growthId = await RequireGrowthIdAsync("add a growth goal");
                if (growthId is not { } gid)
                {
                    return;
                }

                Priority? priority = Enum.TryParse(this.GoalPriority, out Priority p) ? p : null;
                GrowthGoal created = await this._growthService.AddGoalAsync(Member.Id, gid, new GrowthGoal
                {
                    Title = title,
                    Description = this.GoalDesc.Trim(),
                    Status = this.GoalStatus,
                    Category = string.IsNullOrWhiteSpace(this.GoalCategory) ? null : this.GoalCategory.Trim(),
                    Priority = priority,
                    StartDateIso = string.IsNullOrWhiteSpace(this.GoalStart) ? null : this.GoalStart,
                    TargetDateIso = string.IsNullOrWhiteSpace(this.GoalTarget) ? null : this.GoalTarget,
                    LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o"),
                    Actions = Array.Empty<GrowthGoalAction>(),
                    CheckIns = Array.Empty<GrowthGoalCheckIn>(),
                    SuccessCriteria = Array.Empty<string>()
                });
                if (!GrowthUiHelpers.IsValidCreatedId(created.Id))
                {
                    await this._cache.RetryGrowthLoadAsync(Member.Id);
                    await this._dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("goal"));
                    return;
                }

                CloseGoalModal();
                this._nav.NavigateTo($"/team/{Member.Id}/growth/goals/{created.Id}");
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add growth goal.", ex));
            }
        }

        private void OpenAddSkill()
        {
            this.SkillIndex = null;
            this.SkillLabel = this.SkillFrom = this.SkillTo = "";
            this.SkillOpen = true;
        }

        private void OpenEditSkill(int idx)
        {
            var text = Skills.ElementAtOrDefault(idx) ?? "";
            (string Label, string From, string To) parsed = GrowthUiHelpers.ParseSkillText(text);
            this.SkillIndex = idx;
            this.SkillLabel = parsed.Label;
            this.SkillFrom = parsed.From;
            this.SkillTo = parsed.To;
            this.SkillOpen = true;
        }

        private void CloseSkillModal()
        {
            this.SkillOpen = false;
            this.SkillIndex = null;
            this.SkillLabel = this.SkillFrom = this.SkillTo = "";
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
                if (this.SkillIndex is null)
                {
                    skills.Add(nextText);
                }
                else
                {
                    skills[this.SkillIndex.Value] = nextText;
                }

                var normalized = skills.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                Guid? growthId = await RequireGrowthIdAsync("save skills");
                if (growthId is not { } gid)
                {
                    return;
                }

                await this._growthService.SetSkillsInProgressAsync(Member.Id, gid, normalized);
                CloseSkillModal();
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save skills.", ex));
            }
        }

        private async Task DeleteSkill()
        {
            if (this.SkillIndex is null)
            {
                return;
            }

            try
            {
                Growth g = BaseGrowth();
                var skills = g.SkillsInProgress.Where((_, i) => i != this.SkillIndex.Value).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                Guid? growthId = await RequireGrowthIdAsync("remove skill");
                if (growthId is not { } gid)
                {
                    return;
                }

                await this._growthService.SetSkillsInProgressAsync(Member.Id, gid, skills);
                CloseSkillModal();
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to remove skill.", ex));
            }
        }

        private void OpenAddTheme()
        {
            this.ThemeIsNew = true;
            this.ThemeId = null;
            this.ThemeTitle = this.ThemeDesc = this.ThemeObserved = "";
            this.ThemeOpen = true;
        }

        private void OpenEditTheme(Guid themeId)
        {
            GrowthFeedbackTheme? t = Themes.FirstOrDefault(x => x.Id == themeId);
            if (t is null)
            {
                return;
            }

            this.ThemeIsNew = false;
            this.ThemeId = t.Id;
            this.ThemeTitle = t.Title;
            this.ThemeDesc = t.Description;
            this.ThemeObserved = t.ObservedSinceLabel ?? "";
            this.ThemeOpen = true;
        }

        private void CloseThemeModal()
        {
            this.ThemeOpen = false;
            this.ThemeId = null;
            this.ThemeIsNew = false;
        }

        private async Task SaveTheme()
        {
            if (!CanSaveTheme)
            {
                return;
            }

            var title = this.ThemeTitle.Trim();
            var description = this.ThemeDesc.Trim();
            var observed = string.IsNullOrWhiteSpace(this.ThemeObserved) ? null : this.ThemeObserved.Trim();

            try
            {
                Guid? growthId = await RequireGrowthIdAsync("save feedback theme");
                if (growthId is not { } gid)
                {
                    return;
                }

                if (this.ThemeIsNew)
                {
                    GrowthFeedbackTheme created = await this._growthService.AddFeedbackThemeAsync(Member.Id, gid, new GrowthFeedbackTheme
                    {
                        Title = title,
                        Description = description,
                        ObservedSinceLabel = observed
                    });
                    if (!GrowthUiHelpers.IsValidCreatedId(created.Id))
                    {
                        await this._cache.RetryGrowthLoadAsync(Member.Id);
                        await this._dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("feedback theme"));
                        return;
                    }
                }
                else if (this.ThemeId is { } tid)
                {
                    await this._growthService.UpdateFeedbackThemeAsync(Member.Id, gid, new GrowthFeedbackTheme
                    {
                        Id = tid,
                        Title = title,
                        Description = description,
                        ObservedSinceLabel = observed
                    });
                }
                else
                {
                    await this._dialogs.AlertAsync("This theme is no longer available. Refreshing growth data.");
                    await this._cache.RetryGrowthLoadAsync(Member.Id);
                    return;
                }

                CloseThemeModal();
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save feedback theme.", ex));
            }
        }

        private async Task DeleteTheme()
        {
            if (this.ThemeId is not { } themeId)
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

                await this._growthService.DeleteFeedbackThemeAsync(Member.Id, gid, themeId);
                CloseThemeModal();
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to delete feedback theme.", ex));
            }
        }

        private void OpenEditFocusAreas()
        {
            this.FocusDraft = FocusAreasMarkdown;
            this.FocusOpen = true;
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

                await this._growthService.UpdateFocusAreasAsync(Member.Id, gid, this.FocusDraft);
                this.FocusOpen = false;
            }
            catch (Exception ex)
            {
                await this._dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save focus areas.", ex));
            }
        }

        public void Dispose()
        {
            this.Disposed = true;
            this._cache.Changed -= OnChangedAsync;
        }
    }
}
