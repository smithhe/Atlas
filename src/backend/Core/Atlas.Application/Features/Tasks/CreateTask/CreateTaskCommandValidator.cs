namespace Atlas.Application.Features.Tasks.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Notes)
            .NotNull()
            .MaximumLength(50000);

        RuleFor(x => x.EstimatedDurationText)
            .NotNull()
            .MaximumLength(200);

        RuleFor(x => x.ActualDurationText)
            .MaximumLength(200);

        When(x => x.BlockedByTaskIds is not null, () =>
        {
            RuleForEach(x => x.BlockedByTaskIds!)
                .NotEmpty();
        });
    }
}

