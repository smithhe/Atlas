namespace Atlas.Application.Features.TeamMembers.AzureWorkItems.SetAzureWorkItemProject;

public sealed record SetAzureWorkItemProjectCommand(
    Guid TeamMemberId,
    int WorkItemId,
    Guid? ProjectId) : IRequest<bool>;
