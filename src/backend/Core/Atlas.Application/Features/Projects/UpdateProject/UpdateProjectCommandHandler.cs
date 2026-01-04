using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Projects.UpdateProject;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, bool>
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _uow;

    public UpdateProjectCommandHandler(IProjectRepository projects, IUnitOfWork uow)
    {
        _projects = projects;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var project = await _projects.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (project is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var desiredTags = (request.Tags ?? Array.Empty<string>())
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var desiredLinks = (request.Links ?? Array.Empty<Application.DTOs.ProjectLinkDto>())
            .Select(l => new { Label = l.Label.Trim(), Url = l.Url.Trim() })
            .Where(l => l.Label.Length > 0 && l.Url.Length > 0)
            .Select(l => (Key: (l.Label.ToUpperInvariant(), l.Url.ToUpperInvariant()), Label: l.Label, Url: l.Url))
            .GroupBy(x => x.Key)
            .Select(g => g.First())
            .ToDictionary(x => x.Key, x => (x.Label, x.Url));

        project.Name = request.Name;
        project.Summary = request.Summary;
        project.Description = request.Description;
        project.Status = request.Status;
        project.Health = request.Health;
        project.TargetDate = request.TargetDate;
        project.Priority = request.Priority;
        project.ProductOwnerId = request.ProductOwnerId;

        // Sync tags: remove missing, add new.
        project.Tags.RemoveAll(t => !desiredTags.Contains(t.Value));
        foreach (var tag in desiredTags)
        {
            if (project.Tags.Any(t => string.Equals(t.Value, tag, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            project.Tags.Add(new ProjectTag { ProjectId = project.Id, Value = tag });
        }

        // Sync links: remove missing, add new.
        project.Links.RemoveAll(l => !desiredLinks.ContainsKey((l.Label.ToUpperInvariant(), l.Url.ToUpperInvariant())));
        foreach (var kvp in desiredLinks)
        {
            var exists = project.Links.Any(l =>
                string.Equals(l.Label, kvp.Value.Label, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(l.Url, kvp.Value.Url, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                continue;
            }

            project.Links.Add(new ProjectLinkItem { ProjectId = project.Id, Label = kvp.Value.Label, Url = kvp.Value.Url });
        }

        project.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

