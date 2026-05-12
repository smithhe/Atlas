using Atlas.Application.Abstractions.Ai;

namespace Atlas.Application.Features.Ai.CreateSession;

public sealed class CreateAiSessionCommandValidator : AbstractValidator<CreateAiSessionCommand>
{
    public CreateAiSessionCommandValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .MaximumLength(4_000);

        RuleFor(x => x.ActionId)
            .MaximumLength(200);

        RuleFor(x => x.View)
            .Must(v => v == AiViewScope.Dashboard || v == AiViewScope.Tasks)
            .WithMessage("Only Dashboard and Tasks views are supported in phase 1.");
    }
}

