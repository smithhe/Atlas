using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Features.Ai.CreateConversation;
using FluentValidation.Results;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class CreateAiConversationCommandValidatorTests
{
    private readonly CreateAiConversationCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenPromptIsEmpty_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { Prompt = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateAiConversationCommand.Prompt));
    }

    [Fact]
    public void Validate_WhenViewIsInvalid_HasValidationError()
    {
        ValidationResult result = _validator.Validate(ValidCommand() with { View = (AiViewScope)999 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateAiConversationCommand.View));
    }

    [Fact]
    public void Validate_WhenCommandIsValid_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static CreateAiConversationCommand ValidCommand() => new(
        Prompt: "Summarize delivery risks",
        View: AiViewScope.Dashboard,
        ActionId: null,
        TaskId: null,
        ProjectId: null,
        RiskId: null,
        TeamMemberId: null);
}
