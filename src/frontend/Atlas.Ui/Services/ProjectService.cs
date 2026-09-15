using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{

/// <summary>Project mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class ProjectService : IProjectService
{
    private readonly IAtlasApiClient _api;
    private readonly IAppCacheService _cache;

    public ProjectService(IAtlasApiClient api, IAppCacheService cache)
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

    public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        Project? previous = _cache.Projects.FirstOrDefault(p => p.Id == project.Id);
        return OptimisticCache.ApplyAsync(
            previous,
            project,
            p => EntityClone.Project(p),
            _cache.UpdateProject,
            () => _api.AtlasApiEndpointsProjectsUpdateProjectEndpointAsync(
                project.Id,
                EntityRequestMappers.ToUpdateProjectRequest(project),
                cancellationToken));
    }

    public async Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsProjectsDeleteProjectEndpointAsync(projectId, cancellationToken);
        _cache.RemoveProject(projectId);
    }
}
}
