using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests.Integration.Persistence;

public sealed class RepositoryIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly AtlasIntegrationApplicationFactory _factory;

    public RepositoryIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProjectRepository_AddAndGetRoundTripsEntity()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IProjectRepository projects = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"Repo-{Guid.NewGuid():N}",
            Summary = "Summary",
            Description = null,
            Status = ProjectStatus.Active,
            Health = HealthSignal.Green,
            TargetDate = null,
            Priority = Priority.Medium,
            ProductOwnerId = null,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        await projects.AddAsync(project, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        Project? loaded = await projects.GetByIdAsync(project.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(project.Name, loaded.Name);
    }

    [Fact]
    public async Task DbContext_TracksTeamMemberAggregate()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();

        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = $"Member-{Guid.NewGuid():N}",
            Role = "Engineer",
            StatusDot = StatusDot.Green,
            CurrentFocus = "Testing"
        };

        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();

        TeamMember? loaded = await db.TeamMembers.FindAsync(member.Id);
        Assert.NotNull(loaded);
        Assert.Equal(member.Name, loaded.Name);
    }
}
