namespace Atlas.Application.Features.TeamMembers.AzureWorkItems.AddAzureWorkItemLocalNote;

public sealed record AddAzureWorkItemLocalNoteCommand(
    Guid TeamMemberId,
    int WorkItemId,
    string Text) : IRequest<Guid>;
