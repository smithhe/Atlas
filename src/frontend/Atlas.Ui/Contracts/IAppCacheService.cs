using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface IAppCacheService
    {
        bool IsHydrating { get; }

        bool TasksReady { get; }

        bool RisksReady { get; }

        bool ProjectsReady { get; }

        bool TeamReady { get; }

        Settings? Settings { get; }

        IReadOnlyList<Project> Projects { get; }

        IReadOnlyList<ProductOwner> ProductOwners { get; }

        IReadOnlyList<TeamMember> Team { get; }

        IReadOnlyList<TeamMemberRisk> TeamMemberRisks { get; }

        IReadOnlyList<Risk> Risks { get; }

        IReadOnlyList<AtlasTask> Tasks { get; }

        string? LastError { get; }

        event Action? Changed;

        Task EnsureHydratedAsync(CancellationToken cancellationToken = default);

        void RecordHydrationFailure(Exception ex);

        Task RefetchProjectsAsync(CancellationToken cancellationToken = default);

        Task RefetchTeamAsync(CancellationToken cancellationToken = default);

        Task RefetchProductOwnersAsync(CancellationToken cancellationToken = default);

        Task RefetchSettingsAsync(CancellationToken cancellationToken = default);

        void PatchSettings(Settings settings);

        void AddTask(AtlasTask task);

        void UpdateTask(AtlasTask task);

        AtlasTask? TryGetTask(Guid taskId);

        void RemoveTask(Guid taskId);

        void AddRisk(Risk risk);

        Risk? TryGetRisk(Guid riskId);

        void UpdateRisk(Risk risk);

        void RemoveRisk(Guid riskId);

        void AddProject(Project project);

        void UpdateProject(Project project);

        void RemoveProject(Guid projectId);

        void UpdateTeamMember(TeamMember member);

        void AddTeamMemberRisk(TeamMemberRisk risk);

        void UpdateTeamMemberRisk(TeamMemberRisk risk);

        void RemoveTeamMemberRisk(Guid riskId);

        Growth? GetGrowth(Guid memberId);

        GrowthLoadStatus GetGrowthLoadStatus(Guid memberId);

        string? GetGrowthLoadError(Guid memberId);

        void UpdateGrowth(Growth growth);

        Task<Growth?> EnsureGrowthLoadedAsync(Guid memberId, CancellationToken cancellationToken = default);

        Task<Growth?> RetryGrowthLoadAsync(Guid memberId, CancellationToken cancellationToken = default);

        Task<Guid> EnsureGrowthIdAsync(Guid memberId, CancellationToken cancellationToken = default);

        void ReplaceTeamMembers(IReadOnlyList<TeamMember> team, IReadOnlyList<TeamMemberRisk> risks);
    }
}
