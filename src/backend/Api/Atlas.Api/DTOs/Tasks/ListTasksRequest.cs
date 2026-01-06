namespace Atlas.Api.DTOs.Tasks;

public sealed record ListTasksRequest(IReadOnlyList<Guid>? Ids = null);

