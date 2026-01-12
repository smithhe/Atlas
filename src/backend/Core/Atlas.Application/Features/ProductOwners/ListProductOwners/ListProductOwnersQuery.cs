using Atlas.Domain.Entities;

namespace Atlas.Application.Features.ProductOwners.ListProductOwners;

public sealed record ListProductOwnersQuery : IRequest<IReadOnlyList<ProductOwner>>;

