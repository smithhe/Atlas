namespace Atlas.Application.Features.TeamMembers.AzureWorkItems.SetAzureWorkItemProject;

public sealed class SetAzureWorkItemProjectCommandValidator : AbstractValidator<SetAzureWorkItemProjectCommand>
{
    public SetAzureWorkItemProjectCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
        RuleFor(x => x.WorkItemId).GreaterThan(0);
        RuleFor(x => x.ProjectId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("ProjectId must not be empty when provided.");
    }
}
