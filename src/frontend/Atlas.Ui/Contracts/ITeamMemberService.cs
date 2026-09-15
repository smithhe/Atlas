using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface ITeamMemberService
    {
        Task UpdateAsync(TeamMember previous, TeamMember next, CancellationToken cancellationToken = default);
    }
}
