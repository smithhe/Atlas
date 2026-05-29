namespace Atlas.Api.DTOs.TeamMembers.AzureWorkItems;

public sealed record AddAzureWorkItemLocalNoteRequest(Guid TeamMemberId, int WorkItemId, string Text);
