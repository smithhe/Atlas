using Atlas.Application.Features.TeamMembers.Notes.AddTeamNote;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.TeamMembers.Notes;

public sealed class AddTeamNoteCommandValidatorTests
{
    private readonly AddTeamNoteCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTeamMemberIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { TeamMemberId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddTeamNoteCommand.TeamMemberId));
    }

    [Fact]
    public void Validate_WhenTextIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Text = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddTeamNoteCommand.Text));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPrUrlIsInvalid_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { PrUrl = "not-a-url" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddTeamNoteCommand.PrUrl));
    }

    [Fact]
    public void Validate_WhenAdoAndPrArePresent_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with
        {
            AdoWorkItemId = "42",
            PrUrl = "https://dev.azure.com/org/project/_git/repo/pullrequest/1"
        });

        Assert.True(result.IsValid);
    }

    private static AddTeamNoteCommand ValidCommand() => new(
        TeamMemberId: Guid.NewGuid(),
        Type: NoteType.Standup,
        Title: "Standup",
        Text: "Shipped the API tests");
}
