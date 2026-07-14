using System.Text;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Ai.Context;

public sealed class ProjectsPromptContextBuilder : IAiPromptContextBuilder
{
    private readonly IProjectRepository _projects;

    public ProjectsPromptContextBuilder(IProjectRepository projects)
    {
        _projects = projects;
    }

    public AiViewScope Scope => AiViewScope.Projects;

    public async Task<string> BuildContextAsync(AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Project> allProjects = await _projects.ListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Projects context:");
        sb.AppendLine($"- Total projects: {allProjects.Count}");

        if (request.ProjectId.HasValue)
        {
            Project? selected = await _projects.GetByIdWithDetailsAsync(request.ProjectId.Value, cancellationToken);
            if (selected is not null)
            {
                sb.AppendLine("- Selected project:");
                sb.AppendLine($"  - Name: {selected.Name}");
                sb.AppendLine($"  - Status: {selected.Status?.ToString() ?? "none"}");
                sb.AppendLine($"  - Health: {selected.Health?.ToString() ?? "none"}");
                sb.AppendLine($"  - Priority: {selected.Priority?.ToString() ?? "none"}");
                if (selected.TargetDate.HasValue)
                {
                    sb.AppendLine($"  - Target date: {selected.TargetDate.Value:yyyy-MM-dd}");
                }

                if (!string.IsNullOrWhiteSpace(selected.Summary))
                {
                    sb.AppendLine($"  - Summary: {Truncate(selected.Summary, 200)}");
                }

                if (!string.IsNullOrWhiteSpace(selected.Description))
                {
                    sb.AppendLine($"  - Description: {Truncate(selected.Description, 200)}");
                }

                sb.AppendLine($"  - Team members: {selected.TeamMembers.Count}");

                if (selected.Tasks.Count > 0)
                {
                    sb.AppendLine("  - Related tasks:");
                    foreach (TaskItem task in selected.Tasks
                                 .OrderByDescending(t => t.Priority)
                                 .ThenBy(t => t.Title)
                                 .Take(8))
                    {
                        sb.AppendLine($"    - {task.Title} | {task.Priority} | {task.Status}");
                    }
                }

                if (selected.Risks.Count > 0)
                {
                    sb.AppendLine("  - Related risks:");
                    foreach (Risk risk in selected.Risks
                                 .Where(r => r.Status != RiskStatus.Resolved)
                                 .OrderByDescending(r => r.Severity)
                                 .ThenBy(r => r.Title)
                                 .Take(6))
                    {
                        sb.AppendLine($"    - {risk.Title} | {risk.Severity} | {risk.Status}");
                    }
                }
            }
        }

        var snapshot = allProjects
            .OrderBy(p => p.Name)
            .Take(12)
            .ToList();

        if (snapshot.Count > 0)
        {
            sb.AppendLine("- Projects snapshot:");
            foreach (Project project in snapshot)
            {
                sb.AppendLine($"  - {project.Name} | status={project.Status?.ToString() ?? "none"} | health={project.Health?.ToString() ?? "none"} | priority={project.Priority?.ToString() ?? "none"}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string Truncate(string value, int max)
    {
        var trimmed = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (trimmed.Length <= max)
        {
            return trimmed;
        }

        return trimmed[..(max - 1)] + "…";
    }
}
