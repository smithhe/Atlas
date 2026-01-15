using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.Connection;

public sealed record GetAzureConnectionQuery : IRequest<AzureConnection?>;
