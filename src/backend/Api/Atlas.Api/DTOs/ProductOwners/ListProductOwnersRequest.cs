namespace Atlas.Api.DTOs.ProductOwners;

public sealed record ListProductOwnersRequest(IReadOnlyList<Guid>? Ids);

