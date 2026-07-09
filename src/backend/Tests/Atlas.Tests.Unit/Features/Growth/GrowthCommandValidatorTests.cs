using Atlas.Application.Features.Growth.FeedbackThemes.AddFeedbackTheme;
using Atlas.Application.Features.Growth.FeedbackThemes.DeleteFeedbackTheme;
using Atlas.Application.Features.Growth.FeedbackThemes.UpdateFeedbackTheme;
using Atlas.Application.Features.Growth.Goals.Actions.AddGrowthGoalAction;
using Atlas.Application.Features.Growth.Goals.Actions.DeleteGrowthGoalAction;
using Atlas.Application.Features.Growth.Goals.Actions.UpdateGrowthGoalAction;
using Atlas.Application.Features.Growth.Goals.CheckIns.AddGrowthGoalCheckIn;
using Atlas.Application.Features.Growth.Goals.CheckIns.DeleteGrowthGoalCheckIn;
using Atlas.Application.Features.Growth.Goals.CheckIns.UpdateGrowthGoalCheckIn;
using Atlas.Application.Features.Growth.Goals.DeleteGrowthGoal;
using Atlas.Application.Features.Growth.Goals.UpdateGrowthGoal;
using Atlas.Application.Features.Growth.Skills.SetGrowthSkillsInProgress;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Growth;

public sealed class GrowthCommandValidatorTests
{
    private static readonly Guid GrowthId = Guid.NewGuid();
    private static readonly Guid GoalId = Guid.NewGuid();
    private static readonly Guid ActionId = Guid.NewGuid();
    private static readonly Guid CheckInId = Guid.NewGuid();
    private static readonly Guid ThemeId = Guid.NewGuid();

    [Fact]
    public void AddFeedbackTheme_WhenTitleEmpty_Fails()
    {
        var validator = new AddFeedbackThemeCommandValidator();
        ValidationResult result = validator.Validate(new AddFeedbackThemeCommand(GrowthId, string.Empty, "desc", null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddFeedbackTheme_WhenValid_Passes()
    {
        var validator = new AddFeedbackThemeCommandValidator();
        ValidationResult result = validator.Validate(new AddFeedbackThemeCommand(GrowthId, "Theme", "desc", "Q1"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateFeedbackTheme_WhenGrowthIdEmpty_Fails()
    {
        var validator = new UpdateFeedbackThemeCommandValidator();
        ValidationResult result = validator.Validate(new UpdateFeedbackThemeCommand(Guid.Empty, ThemeId, "Title", "desc", null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeleteFeedbackTheme_WhenThemeIdEmpty_Fails()
    {
        var validator = new DeleteFeedbackThemeCommandValidator();
        ValidationResult result = validator.Validate(new DeleteFeedbackThemeCommand(GrowthId, Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeleteGrowthGoal_WhenGoalIdEmpty_Fails()
    {
        var validator = new DeleteGrowthGoalCommandValidator();
        ValidationResult result = validator.Validate(new DeleteGrowthGoalCommand(GrowthId, Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateGrowthGoal_WhenTitleEmpty_Fails()
    {
        var validator = new UpdateGrowthGoalCommandValidator();
        ValidationResult result = validator.Validate(new UpdateGrowthGoalCommand(
            GrowthId, GoalId, string.Empty, "desc", GrowthGoalStatus.OnTrack,
            null, null, null, null, null, null, null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddGrowthGoalAction_WhenTitleEmpty_Fails()
    {
        var validator = new AddGrowthGoalActionCommandValidator();
        ValidationResult result = validator.Validate(new AddGrowthGoalActionCommand(
            GrowthId, GoalId, string.Empty, GrowthGoalActionState.Planned, null, null, null, null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateGrowthGoalAction_WhenGrowthIdEmpty_Fails()
    {
        var validator = new UpdateGrowthGoalActionCommandValidator();
        ValidationResult result = validator.Validate(new UpdateGrowthGoalActionCommand(
            Guid.Empty, GoalId, ActionId, "Title", GrowthGoalActionState.Complete, null, null, null, null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeleteGrowthGoalAction_WhenActionIdEmpty_Fails()
    {
        var validator = new DeleteGrowthGoalActionCommandValidator();
        ValidationResult result = validator.Validate(new DeleteGrowthGoalActionCommand(GrowthId, GoalId, Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddGrowthGoalCheckIn_WhenNoteEmpty_Fails()
    {
        var validator = new AddGrowthGoalCheckInCommandValidator();
        ValidationResult result = validator.Validate(new AddGrowthGoalCheckInCommand(
            GrowthId, GoalId, DateOnly.FromDateTime(DateTime.UtcNow), GrowthGoalCheckInSignal.Positive, string.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateGrowthGoalCheckIn_WhenCheckInIdEmpty_Fails()
    {
        var validator = new UpdateGrowthGoalCheckInCommandValidator();
        ValidationResult result = validator.Validate(new UpdateGrowthGoalCheckInCommand(
            GrowthId, GoalId, Guid.Empty, DateOnly.FromDateTime(DateTime.UtcNow), GrowthGoalCheckInSignal.Concern, "note"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeleteGrowthGoalCheckIn_WhenGoalIdEmpty_Fails()
    {
        var validator = new DeleteGrowthGoalCheckInCommandValidator();
        ValidationResult result = validator.Validate(new DeleteGrowthGoalCheckInCommand(GrowthId, Guid.Empty, CheckInId));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetGrowthSkillsInProgress_WhenSkillEmpty_Fails()
    {
        var validator = new SetGrowthSkillsInProgressCommandValidator();
        ValidationResult result = validator.Validate(new SetGrowthSkillsInProgressCommand(GrowthId, [string.Empty]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetGrowthSkillsInProgress_WhenValid_Passes()
    {
        var validator = new SetGrowthSkillsInProgressCommandValidator();
        ValidationResult result = validator.Validate(new SetGrowthSkillsInProgressCommand(GrowthId, ["C#", "Testing"]));
        Assert.True(result.IsValid);
    }
}
