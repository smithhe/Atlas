using System.Text;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Ai.Context;

public sealed class RisksPromptContextBuilder : IAiPromptContextBuilder
{
    private readonly IRiskRepository _risks;
    private readonly IProjectRepository _projects;

    public RisksPromptContextBuilder(IRiskRepository risks, IProjectRepository projects)
    {
        _risks = risks;
        _projects = projects;
    }

    public AiViewScope Scope => AiViewScope.Risks;

    public async Task<string> BuildContextAsync(AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Risk> allRisks = await _risks.ListAsync(cancellationToken);
        IReadOnlyList<Project> allProjects = await _projects.ListAsync(cancellationToken);
        var projectById = allProjects.ToDictionary(p => p.Id, p => p.Name);

        var sb = new StringBuilder();
        sb.AppendLine("Risks context:");
        sb.AppendLine($"- Total risks: {allRisks.Count}");
        sb.AppendLine($"- Active (non-resolved): {allRisks.Count(r => r.Status != RiskStatus.Resolved)}");

        if (request.RiskId.HasValue)
        {
            Risk? selected = await _risks.GetByIdWithDetailsAsync(request.RiskId.Value, cancellationToken);
            if (selected is not null)
            {
                sb.AppendLine("- Selected risk:");
                sb.AppendLine($"  - Title: {selected.Title}");
                sb.AppendLine($"  - Severity: {selected.Severity}");
                sb.AppendLine($"  - Status: {selected.Status}");
                if (selected.ProjectId.HasValue && projectById.TryGetValue(selected.ProjectId.Value, out var projectName))
                {
                    sb.AppendLine($"  - Project: {projectName}");
                }

                if (!string.IsNullOrWhiteSpace(selected.Description))
                {
                    sb.AppendLine($"  - Description: {Truncate(selected.Description, 220)}");
                }

                if (!string.IsNullOrWhiteSpace(selected.Evidence))
                {
                    sb.AppendLine($"  - Evidence: {Truncate(selected.Evidence, 180)}");
                }

                if (selected.Tasks.Count > 0)
                {
                    sb.AppendLine("  - Linked tasks:");
                    foreach (TaskItem task in selected.Tasks.OrderByDescending(t => t.Priority).Take(8))
                    {
                        sb.AppendLine($"    - {task.Title} | {task.Priority} | {task.Status}");
                    }
                }

                if (selected.LinkedTeamMembers.Count > 0)
                {
                    sb.AppendLine($"  - Linked team members: {selected.LinkedTeamMembers.Count}");
                }

                if (selected.History.Count > 0)
                {
                    sb.AppendLine($"  - History entries: {selected.History.Count}");
                }
            }
        }

        var activeRisks = allRisks
            .Where(r => r.Status != RiskStatus.Resolved)
            .OrderByDescending(r => r.Severity)
            .ThenBy(r => r.Title)
            .Take(10)
            .ToList();

        if (activeRisks.Count > 0)
        {
            sb.AppendLine("- Active risks snapshot:");
            foreach (Risk risk in activeRisks)
            {
                var project = risk.ProjectId.HasValue && projectById.TryGetValue(risk.ProjectId.Value, out var name)
                    ? name
                    : "none";
                sb.AppendLine($"  - {risk.Title} | {risk.Severity} | {risk.Status} | project={project}");
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
