using Atlas.Application.Features.Growth.Goals.AddGrowthGoal;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Growth.Goals;

public sealed class AddGrowthGoalCommandValidatorTests
{
    private readonly AddGrowthGoalCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenGrowthIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { GrowthId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddGrowthGoalCommand.GrowthId));
    }

    [Fact]
    public void Validate_WhenTargetDateBeforeStartDate_HasValidationError()
    {
        AddGrowthGoalCommand command = ValidCommand() with
        {
            StartDate = new DateOnly(2026, 6, 15),
            TargetDate = new DateOnly(2026, 6, 1)
        };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("TargetDate"));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static AddGrowthGoalCommand ValidCommand() => new(
        GrowthId: Guid.NewGuid(),
        Title: "Improve facilitation",
        Description: "Lead more effective retros",
        Status: GrowthGoalStatus.OnTrack,
        StartDate: new DateOnly(2026, 1, 1),
        TargetDate: new DateOnly(2026, 12, 31),
        Category: "Leadership",
        Priority: Priority.Medium);
}
