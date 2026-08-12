using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Tests.Unit.Fakes;

internal sealed class FakeTaskRepository : ITaskRepository
{
    private readonly Dictionary<Guid, TaskItem> _tasks = new();

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tasks.GetValueOrDefault(id));

    public Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<TaskItem>> ListAsync(IReadOnlyList<Guid>? ids = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<TaskItem> items = _tasks.Values;
        if (ids is { Count: > 0 })
        {
            var idSet = ids.ToHashSet();
            items = items.Where(t => idSet.Contains(t.Id));
        }

        return Task.FromResult<IReadOnlyList<TaskItem>>(items.ToList());
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tasks.ContainsKey(id));

    public Task<IReadOnlyList<Guid>> GetDirectBlockerIdsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskItem? task))
        {
            return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        }

        return Task.FromResult<IReadOnlyList<Guid>>(task.BlockedBy.Select(d => d.BlockerTaskId).ToList());
    }

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task AddDependencyAsync(TaskDependency dependency, CancellationToken cancellationToken = default)
    {
        if (_tasks.TryGetValue(dependency.DependentTaskId, out TaskItem? task))
        {
            task.BlockedBy.Add(dependency);
        }

        return Task.CompletedTask;
    }

    public void Remove(TaskItem task) => _tasks.Remove(task.Id);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public void Seed(TaskItem task) => _tasks[task.Id] = task;
}

internal sealed class FakeGrowthRepository : IGrowthRepository
{
    private readonly Dictionary<Guid, Growth> _byId = new();
    private readonly Dictionary<Guid, Growth> _byMember = new();
    private readonly Dictionary<Guid, GrowthGoal> _goals = new();

    public Task<Growth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.GetValueOrDefault(id));

    public Task<Growth?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<Growth?> GetByTeamMemberIdAsync(Guid teamMemberId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byMember.GetValueOrDefault(teamMemberId));

    public Task<Growth?> GetByTeamMemberIdWithDetailsAsync(Guid teamMemberId, CancellationToken cancellationToken = default) =>
        GetByTeamMemberIdAsync(teamMemberId, cancellationToken);

    public Task<GrowthGoal?> GetGoalByIdAsync(Guid goalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_goals.GetValueOrDefault(goalId));

    public Task AddAsync(Growth growth, CancellationToken cancellationToken = default)
    {
        _byId[growth.Id] = growth;
        _byMember[growth.TeamMemberId] = growth;
        return Task.CompletedTask;
    }

    public Task AddGoalAsync(GrowthGoal goal, CancellationToken cancellationToken = default)
    {
        _goals[goal.Id] = goal;
        if (_byId.TryGetValue(goal.GrowthId, out Growth? plan))
        {
            plan.Goals.Add(goal);
        }

        return Task.CompletedTask;
    }

    public Task AddGoalActionAsync(GrowthGoalAction action, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task AddGoalCheckInAsync(GrowthGoalCheckIn checkIn, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task AddFeedbackThemeAsync(GrowthFeedbackTheme theme, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(Growth growth)
    {
        _byId.Remove(growth.Id);
        _byMember.Remove(growth.TeamMemberId);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public void Seed(Growth growth)
    {
        _byId[growth.Id] = growth;
        _byMember[growth.TeamMemberId] = growth;
    }
}

internal sealed class FakeSettingsRepository : ISettingsRepository
{
    public Settings? Singleton { get; set; }

    public Task<Settings?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Singleton?.Id == id ? Singleton : null);

    public Task<Settings?> GetSingletonAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Singleton);

    public Task AddAsync(Settings settings, CancellationToken cancellationToken = default)
    {
        Singleton = settings;
        return Task.CompletedTask;
    }

    public void Remove(Settings settings) => Singleton = null;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

internal sealed class FakeRiskRepository : IRiskRepository
{
    public List<Risk> Risks { get; } = [];

    public Task<Risk?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Risks.FirstOrDefault(x => x.Id == id));

    public Task<Risk?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Risk>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Risk>>(Risks.ToList());

    public Task AddAsync(Risk risk, CancellationToken cancellationToken = default)
    {
        Risks.Add(risk);
        return Task.CompletedTask;
    }

    public Task AddHistoryEntryAsync(RiskHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        Risk? risk = Risks.FirstOrDefault(x => x.Id == entry.RiskId);
        risk?.History.Add(entry);
        return Task.CompletedTask;
    }

    public void Remove(Risk risk) => Risks.Remove(risk);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public void Seed(Risk risk) => Risks.Add(risk);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IUnitOfWorkTransaction>(new FakeUnitOfWorkTransaction());

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

internal sealed class FakeUnitOfWorkTransaction : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
