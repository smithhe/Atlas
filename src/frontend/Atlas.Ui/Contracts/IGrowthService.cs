using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface IGrowthService : IDisposable
    {
        event Action<string>? PersistFailed;

        void AbandonGoalPersists();

        string? UpdateGoal(Guid memberId, Guid goalId, Func<GrowthGoal, GrowthGoal> update);

        string? UpdateAction(Guid memberId, Guid goalId, Guid actionId, Action<GrowthGoalAction> patch);

        string? UpdateCheckIn(Guid memberId, Guid goalId, Guid checkInId, Action<GrowthGoalCheckIn> patch);

        Task<GrowthGoal> AddGoalAsync(
            Guid memberId,
            Guid growthId,
            GrowthGoal draft,
            CancellationToken cancellationToken = default);

        Task<GrowthGoalAction> AddActionAsync(
            Guid memberId,
            Guid growthId,
            Guid goalId,
            GrowthGoalAction draft,
            CancellationToken cancellationToken = default);

        Task<GrowthGoalCheckIn> AddCheckInAsync(
            Guid memberId,
            Guid growthId,
            Guid goalId,
            GrowthGoalCheckIn draft,
            CancellationToken cancellationToken = default);

        Task SetSkillsInProgressAsync(
            Guid memberId,
            Guid growthId,
            IReadOnlyList<string> skills,
            CancellationToken cancellationToken = default);

        Task<GrowthFeedbackTheme> AddFeedbackThemeAsync(
            Guid memberId,
            Guid growthId,
            GrowthFeedbackTheme draft,
            CancellationToken cancellationToken = default);

        Task UpdateFeedbackThemeAsync(
            Guid memberId,
            Guid growthId,
            GrowthFeedbackTheme theme,
            CancellationToken cancellationToken = default);

        Task DeleteFeedbackThemeAsync(
            Guid memberId,
            Guid growthId,
            Guid themeId,
            CancellationToken cancellationToken = default);

        Task UpdateFocusAreasAsync(
            Guid memberId,
            Guid growthId,
            string? markdown,
            CancellationToken cancellationToken = default);
    }
}
