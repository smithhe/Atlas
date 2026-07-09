using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IGrowthRepository
{
    Task<Growth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Growth?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Growth?> GetByTeamMemberIdAsync(Guid teamMemberId, CancellationToken cancellationToken = default);
    Task<Growth?> GetByTeamMemberIdWithDetailsAsync(Guid teamMemberId, CancellationToken cancellationToken = default);
    Task<GrowthGoal?> GetGoalByIdAsync(Guid goalId, CancellationToken cancellationToken = default);

    Task AddAsync(Growth growth, CancellationToken cancellationToken = default);
    Task AddGoalAsync(GrowthGoal goal, CancellationToken cancellationToken = default);
    Task AddGoalActionAsync(GrowthGoalAction action, CancellationToken cancellationToken = default);
    Task AddGoalCheckInAsync(GrowthGoalCheckIn checkIn, CancellationToken cancellationToken = default);
    Task AddFeedbackThemeAsync(GrowthFeedbackTheme theme, CancellationToken cancellationToken = default);
    void Remove(Growth growth);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

