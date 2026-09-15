using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface ITeamMemberRiskService
    {
        Task<TeamMemberRisk> AddAsync(
            Guid memberId,
            TeamMemberRisk draft,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(Guid memberId, TeamMemberRisk next, CancellationToken cancellationToken = default);
    }
}
