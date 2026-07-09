using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Tasks.CreateTask;
using Atlas.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using DomainTaskItem = Atlas.Domain.Entities.TaskItem;

namespace Atlas.Tests.Integration;

public sealed class ApplicationHostIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly AtlasIntegrationApplicationFactory _factory;

    public ApplicationHostIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Host_ResolvesPersistenceServices()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        AtlasDbContext dbContext = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();

        Assert.NotNull(unitOfWork);
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void Host_ResolvesRepositoriesAndMediatR()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        Assert.NotNull(services.GetRequiredService<ITaskRepository>());
        Assert.NotNull(services.GetRequiredService<IRiskRepository>());
        Assert.NotNull(services.GetRequiredService<ITeamMemberRepository>());
        Assert.NotNull(services.GetRequiredService<IProjectRepository>());
        Assert.NotNull(services.GetRequiredService<IMediator>());
        Assert.NotNull(services.GetRequiredService<IAiConversationService>());
    }

    [Fact]
    public async Task MediatR_CreateTaskCommand_PersistsThroughUnitOfWork()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        ITaskRepository tasks = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        Guid id = await mediator.Send(new CreateTaskCommand(
            Title: $"Integration-{Guid.NewGuid():N}",
            Priority: Domain.Enums.Priority.Medium,
            Status: Domain.Enums.TaskStatus.NotStarted,
            AssigneeId: null,
            ProjectId: null,
            RiskId: null,
            DueDate: null,
            EstimatedDurationText: "1h",
            EstimateConfidence: Domain.Enums.Confidence.Medium,
            ActualDurationText: null,
            Notes: string.Empty));

        DomainTaskItem? created = await tasks.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(created);
    }
}
