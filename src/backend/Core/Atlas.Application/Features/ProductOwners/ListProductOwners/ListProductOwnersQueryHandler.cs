using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.ProductOwners.ListProductOwners;

public sealed class ListProductOwnersQueryHandler : IRequestHandler<ListProductOwnersQuery, IReadOnlyList<ProductOwner>>
{
    private readonly IProductOwnerRepository _repo;

    public ListProductOwnersQueryHandler(IProductOwnerRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<ProductOwner>> Handle(ListProductOwnersQuery request, CancellationToken cancellationToken)
    {
        return _repo.ListAsync(cancellationToken);
    }
}

