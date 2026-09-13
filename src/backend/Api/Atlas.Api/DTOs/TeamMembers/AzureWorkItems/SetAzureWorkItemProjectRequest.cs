namespace Atlas.Api.DTOs.TeamMembers.AzureWorkItems;

public sealed record SetAzureWorkItemProjectRequest(Guid TeamMemberId, int WorkItemId, Guid? ProjectId);
