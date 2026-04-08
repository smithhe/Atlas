using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Abstractions.Time;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;

namespace Atlas.Application.Features.AzureDevOps.Team;

public sealed class ImportAzureTeamMembersCommandHandler : IRequestHandler<ImportAzureTeamMembersCommand, ImportAzureTeamMembersResult>
{
    private readonly IAzureUserRepository _azureUsers;
    private readonly IAzureUserMappingRepository _mappings;
    private readonly IAzureProductOwnerMappingRepository _productOwnerMappings;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public ImportAzureTeamMembersCommandHandler(
        IAzureUserRepository azureUsers,
        IAzureUserMappingRepository mappings,
        IAzureProductOwnerMappingRepository productOwnerMappings,
        ITeamMemberRepository teamMembers,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _azureUsers = azureUsers;
        _mappings = mappings;
        _productOwnerMappings = productOwnerMappings;
        _teamMembers = teamMembers;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ImportAzureTeamMembersResult> Handle(ImportAzureTeamMembersCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Users
            .Where(x => !string.IsNullOrWhiteSpace(x.UniqueName))
            .Select(x => new AzureTeamMemberSelection(
                x.DisplayName.Trim(),
                NormalizeUniqueName(x.UniqueName),
                string.IsNullOrWhiteSpace(x.Descriptor) ? null : x.Descriptor.Trim()))
            .GroupBy(x => x.UniqueName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (normalized.Count == 0)
        {
            return new ImportAzureTeamMembersResult(0, 0, 0, 0);
        }

        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        var uniqueNames = normalized.Select(x => x.UniqueName).ToList();
        IReadOnlyList<AzureUser> existingUsers = await _azureUsers.GetByUniqueNamesAsync(uniqueNames, cancellationToken);
        IReadOnlyList<AzureUserMapping> existingMappings = await _mappings.GetByUniqueNamesAsync(uniqueNames, cancellationToken);
        IReadOnlyList<AzureProductOwnerMapping> existingProductOwnerMappings = await _productOwnerMappings.GetByUniqueNamesAsync(uniqueNames, cancellationToken);

        var userByUnique = existingUsers.ToDictionary(x => x.UniqueName, StringComparer.OrdinalIgnoreCase);
        var mappingByUnique = existingMappings.ToDictionary(x => x.AzureUniqueName, StringComparer.OrdinalIgnoreCase);
        var productOwnerMappingByUnique = existingProductOwnerMappings.ToDictionary(x => x.AzureUniqueName, StringComparer.OrdinalIgnoreCase);

        var usersAdded = 0;
        var usersUpdated = 0;
        var teamMembersCreated = 0;
        var mappingsCreated = 0;

        foreach (AzureTeamMemberSelection selection in normalized)
        {
            // Product Owners and Team Members are mutually exclusive import targets.
            if (productOwnerMappingByUnique.ContainsKey(selection.UniqueName))
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

            if (!mappingByUnique.ContainsKey(selection.UniqueName))
            {
                var teamMember = new TeamMember
                {
                    Id = Guid.NewGuid(),
                    Name = string.IsNullOrWhiteSpace(selection.DisplayName) ? selection.UniqueName : selection.DisplayName,
                    Role = string.Empty,
                    StatusDot = StatusDot.Green,
                    CurrentFocus = string.Empty
                };

                await _teamMembers.AddAsync(teamMember, cancellationToken);
                teamMembersCreated++;

                var mapping = new AzureUserMapping
                {
                    Id = Guid.NewGuid(),
                    AzureUniqueName = selection.UniqueName,
                    TeamMemberId = teamMember.Id,
                    LinkedAtUtc = _clock.UtcNow
                };
                await _mappings.AddAsync(mapping, cancellationToken);
                mappingsCreated++;
                mappingByUnique[mapping.AzureUniqueName] = mapping;
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ImportAzureTeamMembersResult(usersAdded, usersUpdated, teamMembersCreated, mappingsCreated);
    }

    private static string NormalizeUniqueName(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
