using Atlas.Api.DTOs.ProductOwners;
using Atlas.Application.Features.ProductOwners.ListProductOwners;

namespace Atlas.Api.Endpoints.ProductOwners;

public sealed class ListProductOwnersEndpoint : Endpoint<ListProductOwnersRequest, IReadOnlyList<ProductOwnerListItemDto>>
{
    private readonly IMediator _mediator;

    public ListProductOwnersEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/product-owners");
        AllowAnonymous();
        Summary(s => { s.Summary = "List product owners"; });
    }

    public override async Task HandleAsync(ListProductOwnersRequest req, CancellationToken ct)
    {
        var owners = await _mediator.Send(new ListProductOwnersQuery(), ct);

        if (req.Ids is { Count: > 0 })
        {
            var set = new HashSet<Guid>(req.Ids.Where(x => x != Guid.Empty));
            owners = owners.Where(o => set.Contains(o.Id)).ToList();
        }

        var dtos = owners.Select(o => new ProductOwnerListItemDto(o.Id, o.Name)).ToList();
        await Send.OkAsync(dtos, ct);
    }
}

