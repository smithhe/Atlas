namespace Atlas.Application.Features.AzureDevOps.Import;

public sealed record LinkAzureWorkItemsCommand(
    IReadOnlyList<Guid> AzureWorkItemIds,
    Guid ProjectId,
    Guid? TeamMemberId) : IRequest<int>;
