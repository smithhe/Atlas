using Atlas.Application.Features.TeamMembers.CreateTeamMember;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.TeamMembers;

public sealed class CreateTeamMemberCommandValidatorTests
{
    private readonly CreateTeamMemberCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Name = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTeamMemberCommand.Name));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static CreateTeamMemberCommand ValidCommand() => new(
        Name: "Alex",
        Role: "Engineer",
        StatusDot: StatusDot.Green);
}
