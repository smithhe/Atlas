namespace Atlas.Api.DTOs.ProductOwners;

public sealed class ListProductOwnersRequest
{
    [QueryParam]
    public IReadOnlyList<Guid>? Ids { get; set; }
}

