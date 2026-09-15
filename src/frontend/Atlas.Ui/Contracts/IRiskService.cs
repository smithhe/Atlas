using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Contracts
{
    public interface IRiskService
    {
        event Action<Guid>? SaveStateChanged;

        EntitySaveState GetSaveState(Guid riskId);

        Task<Risk> CreateAsync(Risk draft, CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Guid riskId,
            Func<Risk, Risk> edit,
            bool debounce = false,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid riskId, CancellationToken cancellationToken = default);

        Task SetTeamMembersAsync(
            Guid riskId,
            IReadOnlyList<Guid> memberIds,
            CancellationToken cancellationToken = default);
    }
}
