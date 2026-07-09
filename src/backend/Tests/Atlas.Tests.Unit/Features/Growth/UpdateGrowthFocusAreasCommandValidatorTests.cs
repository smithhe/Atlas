using Atlas.Application.Features.Growth.UpdateFocusAreas;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Growth;

public sealed class UpdateGrowthFocusAreasCommandValidatorTests
{
    private readonly UpdateGrowthFocusAreasCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenGrowthIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(new UpdateGrowthFocusAreasCommand(Guid.Empty, "Focus"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateGrowthFocusAreasCommand.GrowthId));
    }

    [Fact]
    public void Validate_WhenFocusAreasMarkdownIsNull_HasValidationError()
    {
        ValidationResult result = _validator.Validate(new UpdateGrowthFocusAreasCommand(Guid.NewGuid(), null!));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateGrowthFocusAreasCommand.FocusAreasMarkdown));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(new UpdateGrowthFocusAreasCommand(Guid.NewGuid(), "## Coaching"));

        Assert.True(result.IsValid);
    }
}
