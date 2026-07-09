using Atlas.Application.Features.Growth.EnsureGrowthForTeamMember;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Growth;

public sealed class EnsureGrowthForTeamMemberCommandValidatorTests
{
    private readonly EnsureGrowthForTeamMemberCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTeamMemberIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(new EnsureGrowthForTeamMemberCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(EnsureGrowthForTeamMemberCommand.TeamMemberId));
    }

    [Fact]
    public void Validate_WhenTeamMemberIdIsSet_Passes()
    {
        ValidationResult result = _validator.Validate(new EnsureGrowthForTeamMemberCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }
}
