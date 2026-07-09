using System.Linq;
using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Abstractions.Time;
using Atlas.Domain.Entities;

namespace Atlas.Tests.Unit.Fakes;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTimeOffset? utcNow = null)
    {
        UtcNow = utcNow ?? new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    }

    public DateTimeOffset UtcNow { get; set; }
}

internal sealed class FakeAzureConnectionRepository : IAzureConnectionRepository
{
    public AzureConnection? Singleton { get; set; }

    public Task<AzureConnection?> GetSingletonAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Singleton);

    public Task AddAsync(AzureConnection connection, CancellationToken cancellationToken = default)
    {
        Singleton = connection;
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

internal sealed class FakeAzureSyncStateRepository : IAzureSyncStateRepository
{
    private readonly Dictionary<Guid, AzureSyncState> _byConnection = new();

    public Task<AzureSyncState?> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byConnection.GetValueOrDefault(connectionId));

    public Task AddAsync(AzureSyncState state, CancellationToken cancellationToken = default)
    {
        _byConnection[state.AzureConnectionId] = state;
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public void Seed(AzureSyncState state) => _byConnection[state.AzureConnectionId] = state;
}

internal sealed class FakeAzureWorkItemRepository : IAzureWorkItemRepository
{
    private readonly IList<AzureWorkItemLink> _links;

    /// <summary>
    /// Pass the same list instance as <see cref="FakeAzureWorkItemLinkRepository.Links"/> so
    /// <see cref="ListUnlinkedAsync"/> excludes linked work items (mirrors production).
    /// </summary>
    public FakeAzureWorkItemRepository(IList<AzureWorkItemLink>? links = null)
    {
        _links = links ?? new List<AzureWorkItemLink>();
    }

    public List<AzureWorkItem> Items { get; } = [];

    public Task<IReadOnlyList<AzureWorkItem>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        return Task.FromResult<IReadOnlyList<AzureWorkItem>>(Items.Where(x => idSet.Contains(x.Id)).ToList());
    }

    public Task<IReadOnlyList<AzureWorkItem>> GetByWorkItemIdsAsync(
        Guid connectionId,
        IReadOnlyList<int> workItemIds,
        CancellationToken cancellationToken = default)
    {
        var idSet = workItemIds.ToHashSet();
        return Task.FromResult<IReadOnlyList<AzureWorkItem>>(
            Items.Where(x => x.AzureConnectionId == connectionId && idSet.Contains(x.WorkItemId)).ToList());
    }

    public Task<IReadOnlyList<AzureWorkItem>> ListUnlinkedAsync(
        Guid connectionId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var linkedIds = _links.Select(l => l.AzureWorkItemId).ToHashSet();
        return Task.FromResult<IReadOnlyList<AzureWorkItem>>(
            Items.Where(x => x.AzureConnectionId == connectionId)
                .Where(x => !linkedIds.Contains(x.Id))
                .OrderByDescending(x => x.ChangedDateUtc)
                .Take(take)
                .ToList());
    }

    public Task AddAsync(AzureWorkItem workItem, CancellationToken cancellationToken = default)
    {
        Items.Add(workItem);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

internal sealed class FakeAzureWorkItemLinkRepository : IAzureWorkItemLinkRepository
{
    public FakeAzureWorkItemLinkRepository(List<AzureWorkItemLink>? links = null)
    {
        Links = links ?? [];
    }

    public List<AzureWorkItemLink> Links { get; }

    public Task<IReadOnlyList<AzureWorkItemLink>> GetByWorkItemIdsAsync(
        IReadOnlyList<Guid> workItemIds,
        CancellationToken cancellationToken = default)
    {
        var idSet = workItemIds.ToHashSet();
        return Task.FromResult<IReadOnlyList<AzureWorkItemLink>>(
            Links.Where(x => idSet.Contains(x.AzureWorkItemId)).ToList());
    }

    public Task AddAsync(AzureWorkItemLink link, CancellationToken cancellationToken = default)
    {
        Links.Add(link);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

internal sealed class FakeAzureUserRepository : IAzureUserRepository
{
    public List<AzureUser> Users { get; } = [];

    public Task<IReadOnlyList<AzureUser>> GetByUniqueNamesAsync(
        IReadOnlyList<string> uniqueNames,
        CancellationToken cancellationToken = default)
    {
        var set = uniqueNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyList<AzureUser>>(
            Users.Where(x => set.Contains(x.UniqueName)).ToList());
    }

    public Task<IReadOnlyList<AzureUser>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AzureUser>>(Users.Where(x => x.IsActive).ToList());

    public Task AddAsync(AzureUser user, CancellationToken cancellationToken = default)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

internal sealed class FakeAzureUserMappingRepository : IAzureUserMappingRepository
{
    public List<AzureUserMapping> Mappings { get; } = [];

    public Task<IReadOnlyList<AzureUserMapping>> GetByUniqueNamesAsync(
        IReadOnlyList<string> uniqueNames,
        CancellationToken cancellationToken = default)
    {
        var set = uniqueNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyList<AzureUserMapping>>(
            Mappings.Where(x => set.Contains(x.AzureUniqueName)).ToList());
    }

    public Task<IReadOnlyList<string>> ListUniqueNamesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Mappings.Select(x => x.AzureUniqueName).ToList());

    public Task AddAsync(AzureUserMapping mapping, CancellationToken cancellationToken = default)
    {
        Mappings.Add(mapping);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

internal sealed class FakeAzureProductOwnerMappingRepository : IAzureProductOwnerMappingRepository
{
    public List<AzureProductOwnerMapping> Mappings { get; } = [];

    public Task<IReadOnlyList<AzureProductOwnerMapping>> GetByUniqueNamesAsync(
        IReadOnlyList<string> uniqueNames,
        CancellationToken cancellationToken = default)
    {
        var set = uniqueNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyList<AzureProductOwnerMapping>>(
            Mappings.Where(x => set.Contains(x.AzureUniqueName)).ToList());
    }

    public Task<IReadOnlyList<string>> ListUniqueNamesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Mappings.Select(x => x.AzureUniqueName).ToList());

    public Task AddAsync(AzureProductOwnerMapping mapping, CancellationToken cancellationToken = default)
    {
        Mappings.Add(mapping);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

internal sealed class FakeTeamMemberRepository : ITeamMemberRepository
{
    public List<TeamMember> Members { get; } = [];

    public Task<TeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Members.FirstOrDefault(x => x.Id == id));

    public Task<TeamMember?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<TeamMember>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TeamMember>>(Members.ToList());

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Members.Any(x => x.Id == id));

    public Task AddAsync(TeamMember member, CancellationToken cancellationToken = default)
    {
        Members.Add(member);
        return Task.CompletedTask;
    }

    public Task AddNoteAsync(TeamNote note, CancellationToken cancellationToken = default)
    {
        TeamMember? member = Members.FirstOrDefault(x => x.Id == note.TeamMemberId);
        member?.Notes.Add(note);
        return Task.CompletedTask;
    }

    public Task AddRiskAsync(TeamMemberRisk risk, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task AddAzureWorkItemLocalNoteAsync(AzureWorkItemLocalNote note, CancellationToken cancellationToken = default)
    {
        TeamMember? member = Members.FirstOrDefault(x => x.Id == note.TeamMemberId);
        member?.AzureWorkItemLocalNotes.Add(note);
        return Task.CompletedTask;
    }

    public void Remove(TeamMember member) => Members.Remove(member);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public void Seed(TeamMember member) => Members.Add(member);
}

internal sealed class FakeProductOwnerRepository : IProductOwnerRepository
{
    public List<ProductOwner> Owners { get; } = [];

    public Task<ProductOwner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Owners.FirstOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<ProductOwner>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductOwner>>(Owners.ToList());

    public Task AddAsync(ProductOwner owner, CancellationToken cancellationToken = default)
    {
        Owners.Add(owner);
        return Task.CompletedTask;
    }

    public void Remove(ProductOwner owner) => Owners.Remove(owner);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public void Seed(ProductOwner owner) => Owners.Add(owner);
}

internal sealed class FakeProjectRepository : IProjectRepository
{
    public List<Project> Projects { get; } = [];

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Projects.FirstOrDefault(x => x.Id == id));

    public Task<Project?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Project>>(Projects.ToList());

    public Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        Projects.Add(project);
        return Task.CompletedTask;
    }

    public void Remove(Project project) => Projects.Remove(project);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public void Seed(Project project) => Projects.Add(project);
}

internal sealed class StubAzureDevOpsClient : IAzureDevOpsClient
{
    public List<string> CapturedWiql { get; } = [];
    public Exception? QueryException { get; set; }
    public IReadOnlyList<int> WorkItemIds { get; set; } = [];
    public IReadOnlyList<AzureWorkItemDetails> WorkItems { get; set; } = [];

    public Task<IReadOnlyList<AzureProjectSummary>> ListProjectsAsync(
        string baseUrl,
        string organization,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AzureProjectSummary>>([]);

    public Task<IReadOnlyList<AzureTeamSummary>> ListTeamsAsync(
        string baseUrl,
        string organization,
        string projectId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AzureTeamSummary>>([]);

    public Task<IReadOnlyList<AzureUserSummary>> ListUsersAsync(
        string baseUrl,
        string organization,
        string projectId,
        string teamId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AzureUserSummary>>([]);

    public Task<AzureTeamAreaPaths> GetTeamAreaPathsAsync(
        string baseUrl,
        string organization,
        string projectId,
        string teamName,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AzureTeamAreaPaths(null, []));

    public Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(
        string baseUrl,
        string organization,
        string project,
        string wiql,
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        if (QueryException is not null)
        {
            throw QueryException;
        }

        CapturedWiql.Add(wiql);
        IReadOnlyList<int> ids = top is int limit ? WorkItemIds.Take(limit).ToList() : WorkItemIds;
        return Task.FromResult(ids);
    }

    public Task<IReadOnlyList<AzureWorkItemDetails>> GetWorkItemsAsync(
        string baseUrl,
        string organization,
        string project,
        IReadOnlyList<int> workItemIds,
        CancellationToken cancellationToken = default)
    {
        var lookup = WorkItems.ToDictionary(w => w.Id);
        return Task.FromResult<IReadOnlyList<AzureWorkItemDetails>>(
            workItemIds.Where(lookup.ContainsKey).Select(id => lookup[id]).ToList());
    }
}
