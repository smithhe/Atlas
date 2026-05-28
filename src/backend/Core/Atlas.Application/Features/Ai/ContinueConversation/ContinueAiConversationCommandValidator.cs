using FluentValidation;

namespace Atlas.Application.Features.Ai.ContinueConversation;

public sealed class ContinueAiConversationCommandValidator : AbstractValidator<ContinueAiConversationCommand>
{
    public ContinueAiConversationCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(8000);
    }
}
