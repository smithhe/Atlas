using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Abstractions.Time;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.ProductOwners;

public sealed class ImportAzureProductOwnersCommandHandler
    : IRequestHandler<ImportAzureProductOwnersCommand, ImportAzureProductOwnersResult>
{
    private readonly IAzureUserRepository _azureUsers;
    private readonly IAzureUserMappingRepository _teamMappings;
    private readonly IAzureProductOwnerMappingRepository _productOwnerMappings;
    private readonly IProductOwnerRepository _productOwners;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public ImportAzureProductOwnersCommandHandler(
        IAzureUserRepository azureUsers,
        IAzureUserMappingRepository teamMappings,
        IAzureProductOwnerMappingRepository productOwnerMappings,
        IProductOwnerRepository productOwners,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _azureUsers = azureUsers;
        _teamMappings = teamMappings;
        _productOwnerMappings = productOwnerMappings;
        _productOwners = productOwners;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ImportAzureProductOwnersResult> Handle(
        ImportAzureProductOwnersCommand request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Users
            .Where(x => !string.IsNullOrWhiteSpace(x.UniqueName))
            .Select(x => new AzureProductOwnerSelection(
                x.DisplayName.Trim(),
                NormalizeUniqueName(x.UniqueName),
                string.IsNullOrWhiteSpace(x.Descriptor) ? null : x.Descriptor.Trim()))
            .GroupBy(x => x.UniqueName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (normalized.Count == 0)
        {
            return new ImportAzureProductOwnersResult(0, 0, 0, 0);
        }

        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        var uniqueNames = normalized.Select(x => x.UniqueName).ToList();
        IReadOnlyList<AzureUser> existingUsers = await _azureUsers.GetByUniqueNamesAsync(uniqueNames, cancellationToken);
        IReadOnlyList<AzureUserMapping> existingTeamMappings = await _teamMappings.GetByUniqueNamesAsync(uniqueNames, cancellationToken);
        IReadOnlyList<AzureProductOwnerMapping> existingProductOwnerMappings = await _productOwnerMappings.GetByUniqueNamesAsync(uniqueNames, cancellationToken);
        IReadOnlyList<ProductOwner> existingProductOwners = await _productOwners.ListAsync(cancellationToken);

        var userByUnique = existingUsers.ToDictionary(x => x.UniqueName, StringComparer.OrdinalIgnoreCase);
        var teamMappingByUnique = existingTeamMappings.ToDictionary(x => x.AzureUniqueName, StringComparer.OrdinalIgnoreCase);
        var productOwnerMappingByUnique = existingProductOwnerMappings.ToDictionary(x => x.AzureUniqueName, StringComparer.OrdinalIgnoreCase);
        var productOwnerByName = existingProductOwners.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        var usersAdded = 0;
        var usersUpdated = 0;
        var productOwnersCreated = 0;
        var mappingsCreated = 0;

        foreach (AzureProductOwnerSelection selection in normalized)
        {
            // Product Owners and Team Members are mutually exclusive import targets.
            if (teamMappingByUnique.ContainsKey(selection.UniqueName))
            {
                continue;
            }

            if (!userByUnique.TryGetValue(selection.UniqueName, out AzureUser? user))
            {
                user = new AzureUser
                {
                    Id = Guid.NewGuid(),
                    DisplayName = selection.DisplayName,
                    UniqueName = selection.UniqueName,
                    Descriptor = selection.Descriptor,
                    IsActive = true
                };
                await _azureUsers.AddAsync(user, cancellationToken);
                userByUnique[user.UniqueName] = user;
                usersAdded++;
            }
            else
            {
                user.DisplayName = selection.DisplayName;
                user.Descriptor = selection.Descriptor;
                user.IsActive = true;
                usersUpdated++;
            }

            if (productOwnerMappingByUnique.ContainsKey(selection.UniqueName))
            {
                continue;
            }

            var preferredName = string.IsNullOrWhiteSpace(selection.DisplayName)
                ? selection.UniqueName
                : selection.DisplayName;

            if (!productOwnerByName.TryGetValue(preferredName, out ProductOwner? productOwner))
            {
                productOwner = new ProductOwner
                {
                    Id = Guid.NewGuid(),
                    Name = preferredName
                };
                await _productOwners.AddAsync(productOwner, cancellationToken);
                productOwnerByName[productOwner.Name] = productOwner;
                productOwnersCreated++;
            }
            else
            {
                // Name-based ProductOwner dedupe is intentional. Keep creating a mapping
                // for each distinct Azure identity so future imports can still detect
                // that this Azure user has already been processed.
                // TODO: Surface a UI warning when duplicate Product Owner names are skipped.
            }

            var mapping = new AzureProductOwnerMapping
            {
                Id = Guid.NewGuid(),
                AzureUniqueName = selection.UniqueName,
                ProductOwnerId = productOwner.Id,
                LinkedAtUtc = _clock.UtcNow
            };
            await _productOwnerMappings.AddAsync(mapping, cancellationToken);
            productOwnerMappingByUnique[mapping.AzureUniqueName] = mapping;
            mappingsCreated++;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ImportAzureProductOwnersResult(
            usersAdded,
            usersUpdated,
            productOwnersCreated,
            mappingsCreated);
    }

    private static string NormalizeUniqueName(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
