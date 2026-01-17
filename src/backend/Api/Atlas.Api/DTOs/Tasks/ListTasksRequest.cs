namespace Atlas.Api.DTOs.Tasks;

public sealed class ListTasksRequest
{
    [QueryParam]
    public IReadOnlyList<Guid>? Ids { get; set; }
}

