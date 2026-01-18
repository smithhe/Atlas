using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.Connection;

public sealed class UpdateAzureConnectionCommandHandler : IRequestHandler<UpdateAzureConnectionCommand, bool>
{
    private readonly IAzureConnectionRepository _connections;
    private readonly IUnitOfWork _uow;

    public UpdateAzureConnectionCommandHandler(IAzureConnectionRepository connections, IUnitOfWork uow)
    {
        _connections = connections;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateAzureConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var existing = await _connections.GetSingletonAsync(cancellationToken);
        if (existing is null)
        {
            existing = new AzureConnection { Id = Guid.NewGuid() };
            await _connections.AddAsync(existing, cancellationToken);
        }

        existing.Organization = request.Organization.Trim();
        existing.Project = request.Project.Trim();
        existing.ProjectId = request.ProjectId.Trim();
        existing.AreaPath = request.AreaPath.Trim();
        existing.TeamName = string.IsNullOrWhiteSpace(request.TeamName) ? null : request.TeamName.Trim();
        existing.TeamId = request.TeamId.Trim();
        existing.IsEnabled = request.IsEnabled;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}
