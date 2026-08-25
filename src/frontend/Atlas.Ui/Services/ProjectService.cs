using Atlas.Ui.Api.Generated;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Project mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class ProjectService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;

    public ProjectService(IAtlasApiClient api, AppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<Project> CreateAsync(Project draft, CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsProjectsCreateProjectResponse res =
            await _api.AtlasApiEndpointsProjectsCreateProjectEndpointAsync(_cache.ToCreateProjectRequest(draft), cancellationToken);
        draft.Id = res.Id ?? Guid.Empty;
        _cache.AddProject(draft);
        return draft;
    }

    public Task UpdateAsync(Project project, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsProjectsUpdateProjectEndpointAsync(
            project.Id, _cache.ToUpdateProjectRequest(project), cancellationToken);

    public async Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsProjectsDeleteProjectEndpointAsync(projectId, cancellationToken);
        _cache.RemoveProject(projectId);
    }
}
