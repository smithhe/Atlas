using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests.Integration.Persistence;

public sealed class ComplexRepositoryIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly AtlasIntegrationApplicationFactory _factory;

    public ComplexRepositoryIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TaskRepository_FiltersIncludesAndDirectBlockers()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITaskRepository tasks = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var blocker = NewTask("Blocker");
        var dependent = NewTask("Dependent");
        dependent.BlockedBy.Add(new TaskDependency
        {
            Id = Guid.NewGuid(),
            DependentTaskId = dependent.Id,
            BlockerTaskId = blocker.Id
        });
        var other = NewTask("Other");

        await tasks.AddAsync(blocker, CancellationToken.None);
        await tasks.AddAsync(dependent, CancellationToken.None);
        await tasks.AddAsync(other, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        IReadOnlyList<TaskItem> filtered = await tasks.ListAsync([dependent.Id, other.Id], CancellationToken.None);
        filtered.Select(t => t.Id).Should().BeEquivalentTo([dependent.Id, other.Id]);
        filtered.Single(t => t.Id == dependent.Id).BlockedBy.Should().ContainSingle()
            .Which.BlockerTaskId.Should().Be(blocker.Id);

        TaskItem? details = await tasks.GetByIdWithDetailsAsync(dependent.Id, CancellationToken.None);
        details.Should().NotBeNull();
        details!.BlockedBy.Should().ContainSingle();

        IReadOnlyList<Guid> blockerIds = await tasks.GetDirectBlockerIdsAsync(dependent.Id, CancellationToken.None);
        blockerIds.Should().Equal(blocker.Id);
    }

    [Fact]
    public async Task GrowthRepository_LoadsDeepIncludesNormalizesSkillsAndFindsByTeamMember()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        IGrowthRepository growth = scope.ServiceProvider.GetRequiredService<IGrowthRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = $"Growth-{Guid.NewGuid():N}",
            Role = "Engineer",
            StatusDot = StatusDot.Green,
            CurrentFocus = string.Empty
        };
        db.TeamMembers.Add(member);

        var plan = new Growth
        {
            Id = Guid.NewGuid(),
            TeamMemberId = member.Id,
            FocusAreasMarkdown = "## Focus",
            SkillsInProgress =
            [
                new GrowthSkillInProgress { GrowthId = Guid.Empty, SortOrder = 2, Value = "zeta" },
                new GrowthSkillInProgress { GrowthId = Guid.Empty, SortOrder = 1, Value = "alpha" }
            ]
        };
        plan.SkillsInProgress.ForEach(s => s.GrowthId = plan.Id);

        var goal = new GrowthGoal
        {
            Id = Guid.NewGuid(),
            GrowthId = plan.Id,
            Title = "Goal",
            Description = "Desc",
            Status = GrowthGoalStatus.OnTrack,
            Actions =
            [
                new GrowthGoalAction
                {
                    Id = Guid.NewGuid(),
                    GrowthGoalId = Guid.Empty,
                    Title = "Action",
                    State = GrowthGoalActionState.Planned
                }
            ],
            CheckIns =
            [
                new GrowthGoalCheckIn
                {
                    Id = Guid.NewGuid(),
                    GrowthGoalId = Guid.Empty,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    Signal = GrowthGoalCheckInSignal.Positive,
                    Note = "ok"
                }
            ]
        };
        goal.Actions[0].GrowthGoalId = goal.Id;
        goal.CheckIns[0].GrowthGoalId = goal.Id;
        plan.Goals.Add(goal);
        plan.FeedbackThemes.Add(new GrowthFeedbackTheme
        {
            Id = Guid.NewGuid(),
            GrowthId = plan.Id,
            Title = "Theme",
            Description = "Desc"
        });

        await growth.AddAsync(plan, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        Growth? byId = await growth.GetByIdWithDetailsAsync(plan.Id, CancellationToken.None);
        byId.Should().NotBeNull();
        byId!.Goals.Should().ContainSingle();
        byId.Goals[0].Actions.Should().ContainSingle();
        byId.Goals[0].CheckIns.Should().ContainSingle();
        byId.FeedbackThemes.Should().ContainSingle();
        byId.SkillsInProgress.Select(s => s.Value).Should().Equal("alpha", "zeta");

        Growth? byMember = await growth.GetByTeamMemberIdWithDetailsAsync(member.Id, CancellationToken.None);
        byMember.Should().NotBeNull();
        byMember!.Id.Should().Be(plan.Id);
        byMember.SkillsInProgress.Select(s => s.SortOrder).Should().Equal(1, 2);
    }

    [Fact]
    public async Task TeamMemberRepository_GetByIdWithDetails_LoadsAggregate()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        ITeamMemberRepository team = scope.ServiceProvider.GetRequiredService<ITeamMemberRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = $"Member-{Guid.NewGuid():N}",
            Role = "Engineer",
            StatusDot = StatusDot.Green,
            CurrentFocus = "Testing"
        };
        member.Notes.Add(new TeamNote
        {
            Id = Guid.NewGuid(),
            TeamMemberId = member.Id,
            Type = NoteType.Standup,
            Title = "Standup",
            Text = "Shipped",
            CreatedAt = DateTimeOffset.UtcNow
        });
        member.Risks.Add(new TeamMemberRisk
        {
            Id = Guid.NewGuid(),
            TeamMemberId = member.Id,
            Title = "Load",
            Severity = TeamMemberRiskSeverity.Medium,
            RiskType = "Capacity",
            Status = TeamMemberRiskStatus.Open,
            Trend = TeamMemberRiskTrend.Stable,
            FirstNoticedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ImpactArea = "Delivery",
            Description = "Busy",
            CurrentAction = "Reduce WIP"
        });

        var connection = new AzureConnection
        {
            Id = Guid.NewGuid(),
            Organization = "contoso",
            Project = "Atlas",
            ProjectId = "proj-1",
            AreaPath = "Atlas",
            IsEnabled = true
        };
        var workItem = new AzureWorkItem
        {
            Id = Guid.NewGuid(),
            AzureConnectionId = connection.Id,
            WorkItemId = 501,
            Title = "WI",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "Atlas",
            IterationPath = "Sprint",
            Url = "https://example.com/501",
            ChangedDateUtc = DateTimeOffset.UtcNow
        };
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"P-{Guid.NewGuid():N}",
            Summary = "S",
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
        member.AzureWorkItemLinks.Add(new AzureWorkItemLink
        {
            Id = Guid.NewGuid(),
            AzureWorkItemId = workItem.Id,
            ProjectId = project.Id,
            TeamMemberId = member.Id,
            LinkedAtUtc = DateTimeOffset.UtcNow
        });

        db.AzureConnections.Add(connection);
        db.AzureWorkItems.Add(workItem);
        db.Projects.Add(project);
        await team.AddAsync(member, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        TeamMember? loaded = await team.GetByIdWithDetailsAsync(member.Id, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Notes.Should().ContainSingle();
        loaded.Risks.Should().ContainSingle();
        loaded.AzureWorkItemLinks.Should().ContainSingle();
        loaded.AzureWorkItemLinks[0].AzureWorkItem.Should().NotBeNull();
        loaded.AzureWorkItemLinks[0].AzureWorkItem!.WorkItemId.Should().Be(501);
    }

    [Fact]
    public async Task ProjectRepository_GetByIdWithDetails_IncludesRelatedCollections()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IProjectRepository projects = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = $"Details-{Guid.NewGuid():N}",
            Summary = "Summary",
            LastUpdatedAt = DateTimeOffset.UtcNow,
            Tags = [new ProjectTag { ProjectId = projectId, Value = "alpha" }],
            Links = [new ProjectLinkItem { ProjectId = projectId, Label = "Docs", Url = "https://example.com" }],
            TeamMembers = []
        };

        await projects.AddAsync(project, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        Project? loaded = await projects.GetByIdWithDetailsAsync(projectId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Tags.Should().ContainSingle(t => t.Value == "alpha");
        loaded.Links.Should().ContainSingle(l => l.Label == "Docs");
    }

    [Fact]
    public async Task RiskRepository_GetByIdWithDetails_IncludesHistory()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IRiskRepository risks = scope.ServiceProvider.GetRequiredService<IRiskRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var risk = new Risk
        {
            Id = Guid.NewGuid(),
            Title = "Slippage",
            Status = RiskStatus.Open,
            Severity = SeverityLevel.High,
            Description = "Desc",
            Evidence = "Evidence",
            LastUpdatedAt = DateTimeOffset.UtcNow,
            History =
            [
                new RiskHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    RiskId = Guid.Empty,
                    Text = "Noted",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };
        risk.History[0].RiskId = risk.Id;

        await risks.AddAsync(risk, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        Risk? loaded = await risks.GetByIdWithDetailsAsync(risk.Id, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.History.Should().ContainSingle(h => h.Text == "Noted");
    }

    [Fact]
    public async Task AzureWorkItemLinkRepository_LinkAndLookupByWorkItemIds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        IAzureWorkItemLinkRepository links = scope.ServiceProvider.GetRequiredService<IAzureWorkItemLinkRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var connection = new AzureConnection
        {
            Id = Guid.NewGuid(),
            Organization = "contoso",
            Project = "Atlas",
            ProjectId = "proj-1",
            AreaPath = "Atlas",
            IsEnabled = true
        };
        var workItem = new AzureWorkItem
        {
            Id = Guid.NewGuid(),
            AzureConnectionId = connection.Id,
            WorkItemId = 777,
            Title = "Link me",
            State = "Active",
            WorkItemType = "Task",
            AreaPath = "Atlas",
            IterationPath = "Sprint",
            Url = "https://example.com/777",
            ChangedDateUtc = DateTimeOffset.UtcNow
        };
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"Link-{Guid.NewGuid():N}",
            Summary = "S",
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
        db.AzureConnections.Add(connection);
        db.AzureWorkItems.Add(workItem);
        db.Projects.Add(project);

        var link = new AzureWorkItemLink
        {
            Id = Guid.NewGuid(),
            AzureWorkItemId = workItem.Id,
            ProjectId = project.Id,
            LinkedAtUtc = DateTimeOffset.UtcNow
        };
        await links.AddAsync(link, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        IReadOnlyList<AzureWorkItemLink> found = await links.GetByWorkItemIdsAsync([workItem.Id], CancellationToken.None);
        found.Should().ContainSingle(l => l.Id == link.Id && l.ProjectId == project.Id);

        IReadOnlyList<AzureWorkItemLink> missing = await links.GetByWorkItemIdsAsync([Guid.NewGuid()], CancellationToken.None);
        missing.Should().BeEmpty();
    }

    [Fact]
    public async Task AiSessionRepository_OrdersEventsAndAppendsSequence()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IAiConversationRepository conversations = scope.ServiceProvider.GetRequiredService<IAiConversationRepository>();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid conversationId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await conversations.AddAsync(new AiConversation
        {
            Id = conversationId,
            Title = "Conv",
            View = "Dashboard",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, CancellationToken.None);
        await sessions.AddAsync(new AiSession
        {
            Id = sessionId,
            ConversationId = conversationId,
            TurnIndex = 0,
            Title = "Turn",
            Prompt = "Hello",
            View = "Dashboard",
            CreatedAtUtc = now,
            Status = "created",
            IsTerminal = false
        }, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        AiSessionEvent first = await sessions.AppendEventAsync(sessionId, new AiSessionEvent
        {
            Type = "status",
            OccurredAtUtc = now,
            Status = "running",
            Message = "start",
            IsTerminal = false
        }, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        AiSessionEvent second = await sessions.AppendEventAsync(sessionId, new AiSessionEvent
        {
            Type = "delta",
            OccurredAtUtc = now.AddSeconds(1),
            Delta = "hi",
            IsTerminal = false
        }, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        AiSessionEvent terminal = await sessions.AppendEventAsync(sessionId, new AiSessionEvent
        {
            Type = "status",
            OccurredAtUtc = now.AddSeconds(2),
            Status = "completed",
            IsTerminal = true
        }, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        first.Sequence.Should().Be(1);
        second.Sequence.Should().Be(2);
        terminal.Sequence.Should().Be(3);

        IReadOnlyList<AiSessionEvent> ordered = await sessions.ListEventsAsync(sessionId, cancellationToken: CancellationToken.None);
        ordered.Select(e => e.Sequence).Should().Equal(1, 2, 3);

        IReadOnlyList<AiSessionEvent> afterFirst = await sessions.ListEventsAsync(sessionId, afterSequence: 1, CancellationToken.None);
        afterFirst.Select(e => e.Sequence).Should().Equal(2, 3);

        AiSession? withEvents = await sessions.GetByIdWithEventsAsync(sessionId, CancellationToken.None);
        withEvents.Should().NotBeNull();
        withEvents!.IsTerminal.Should().BeTrue();
        withEvents.Events.Select(e => e.Sequence).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task AiConversationRepository_NestsTurnsAndListsRecent()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IAiConversationRepository conversations = scope.ServiceProvider.GetRequiredService<IAiConversationRepository>();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        DateTimeOffset older = DateTimeOffset.UtcNow.AddMinutes(-5);
        DateTimeOffset newer = DateTimeOffset.UtcNow;

        var olderConv = new AiConversation
        {
            Id = Guid.NewGuid(),
            Title = "Older",
            View = "Dashboard",
            CreatedAtUtc = older,
            UpdatedAtUtc = older
        };
        var newerConv = new AiConversation
        {
            Id = Guid.NewGuid(),
            Title = "Newer",
            View = "Tasks",
            CreatedAtUtc = newer,
            UpdatedAtUtc = newer
        };

        await conversations.AddAsync(olderConv, CancellationToken.None);
        await conversations.AddAsync(newerConv, CancellationToken.None);

        Guid sessionId = Guid.NewGuid();
        await sessions.AddAsync(new AiSession
        {
            Id = sessionId,
            ConversationId = newerConv.Id,
            TurnIndex = 0,
            Title = "Turn",
            Prompt = "Prompt",
            View = "Tasks",
            CreatedAtUtc = newer,
            Status = "created",
            IsTerminal = false
        }, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        await sessions.AppendEventAsync(sessionId, new AiSessionEvent
        {
            Type = "status",
            OccurredAtUtc = newer,
            Status = "completed",
            IsTerminal = true
        }, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        AiConversation? loaded = await conversations.GetByIdWithTurnsAsync(newerConv.Id, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Turns.Should().ContainSingle();
        loaded.Turns[0].Events.Should().ContainSingle(e => e.IsTerminal);

        IReadOnlyList<AiConversation> recent = await conversations.ListRecentAsync(1, CancellationToken.None);
        recent.Should().ContainSingle().Which.Id.Should().Be(newerConv.Id);
    }

    private static TaskItem NewTask(string title) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Priority = Priority.Medium,
        Status = Domain.Enums.TaskStatus.NotStarted,
        EstimatedDurationText = "1h",
        EstimateConfidence = Confidence.Medium,
        Notes = string.Empty,
        LastTouchedAt = DateTimeOffset.UtcNow
    };
}
