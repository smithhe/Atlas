using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Projects.CreateProject;

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _uow;

    public CreateProjectCommandHandler(IProjectRepository projects, IUnitOfWork uow)
    {
        _projects = projects;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var tagValues = (request.Tags ?? Array.Empty<string>())
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var linkValues = (request.Links ?? Array.Empty<Application.DTOs.ProjectLinkDto>())
            .Select(l => new { Label = l.Label.Trim(), Url = l.Url.Trim() })
            .Where(l => l.Label.Length > 0 && l.Url.Length > 0)
            .DistinctBy(l => (l.Label.ToUpperInvariant(), l.Url.ToUpperInvariant()))
            .ToList();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Summary = request.Summary,
            Description = request.Description,
            Status = request.Status,
            Health = request.Health,
            TargetDate = request.TargetDate,
            Priority = request.Priority,
            ProductOwnerId = request.ProductOwnerId,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        project.Tags.AddRange(tagValues.Select(v => new ProjectTag { ProjectId = project.Id, Value = v }));
        project.Links.AddRange(linkValues.Select(l => new ProjectLinkItem { ProjectId = project.Id, Label = l.Label, Url = l.Url }));

        await _projects.AddAsync(project, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return project.Id;
    }
}

