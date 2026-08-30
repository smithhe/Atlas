using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
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
            await _api.AtlasApiEndpointsProjectsCreateProjectEndpointAsync(
                EntityRequestMappers.ToCreateProjectRequest(draft),
                cancellationToken);
        draft.Id = res.Id ?? Guid.Empty;
        _cache.AddProject(draft);
        return draft;
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        Project? previous = _cache.Projects.FirstOrDefault(p => p.Id == project.Id);
        Project? previousClone = previous is not null ? EntityClone.Project(previous) : null;
        _cache.UpdateProject(project);
        try
        {
            await _api.AtlasApiEndpointsProjectsUpdateProjectEndpointAsync(
                project.Id,
                EntityRequestMappers.ToUpdateProjectRequest(project),
                cancellationToken);
        }
        catch
        {
            if (previousClone is not null)
            {
                _cache.UpdateProject(previousClone);
            }

            throw;
        }
    }

    public async Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsProjectsDeleteProjectEndpointAsync(projectId, cancellationToken);
        _cache.RemoveProject(projectId);
    }
}
