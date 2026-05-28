using FluentValidation;

namespace Atlas.Application.Features.Ai.CreateConversation;

public sealed class CreateAiConversationCommandValidator : AbstractValidator<CreateAiConversationCommand>
{
    public CreateAiConversationCommandValidator()
    {
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.View).IsInEnum();
    }
}
