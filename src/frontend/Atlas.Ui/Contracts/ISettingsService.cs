using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface ISettingsService
    {
        void PatchLocal(Settings patch);

        Task UpdateAsync(Settings settings, CancellationToken cancellationToken = default);
    }
}
