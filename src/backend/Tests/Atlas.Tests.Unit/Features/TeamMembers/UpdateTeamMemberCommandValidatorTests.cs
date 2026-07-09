using Atlas.Application.Features.TeamMembers.UpdateTeamMember;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.TeamMembers;

public sealed class UpdateTeamMemberCommandValidatorTests
{
    private readonly UpdateTeamMemberCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTeamMemberCommand.Id));
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Name = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTeamMemberCommand.Name));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static UpdateTeamMemberCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Name: "Alex",
        Role: "Lead",
        StatusDot: StatusDot.Yellow,
        CurrentFocus: "Platform work");
}
