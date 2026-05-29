namespace Atlas.Application.Features.TeamMembers.AzureWorkItems.AddAzureWorkItemLocalNote;

public sealed class AddAzureWorkItemLocalNoteCommandValidator : AbstractValidator<AddAzureWorkItemLocalNoteCommand>
{
    public AddAzureWorkItemLocalNoteCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
        RuleFor(x => x.WorkItemId).GreaterThan(0);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(10_000);
    }
}
