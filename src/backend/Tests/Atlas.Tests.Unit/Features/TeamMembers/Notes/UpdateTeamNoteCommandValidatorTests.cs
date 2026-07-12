using Atlas.Application.Features.TeamMembers.Notes.UpdateTeamNote;
using Atlas.Domain.Enums;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.TeamMembers.Notes;

public sealed class UpdateTeamNoteCommandValidatorTests
{
    private readonly UpdateTeamNoteCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPrUrlIsInvalid_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { PrUrl = "relative/path" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTeamNoteCommand.PrUrl));
    }

    private static UpdateTeamNoteCommand ValidCommand() => new(
        TeamMemberId: Guid.NewGuid(),
        NoteId: Guid.NewGuid(),
        Type: NoteType.Standup,
        Title: "Standup",
        Text: "Shipped the API tests",
        PinnedOrder: null,
        AdoWorkItemId: "42",
        PrUrl: "https://dev.azure.com/org/project/_git/repo/pullrequest/1");
}
