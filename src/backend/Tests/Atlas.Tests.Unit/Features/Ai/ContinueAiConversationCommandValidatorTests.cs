using Atlas.Application.Features.Ai.ContinueConversation;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class ContinueAiConversationCommandValidatorTests
{
    private readonly ContinueAiConversationCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenConversationIdIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { ConversationId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ContinueAiConversationCommand.ConversationId));
    }

    [Fact]
    public void Validate_WhenPromptIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Prompt = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ContinueAiConversationCommand.Prompt));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static ContinueAiConversationCommand ValidCommand() => new(
        ConversationId: Guid.NewGuid(),
        Prompt: "Tell me more");
}
