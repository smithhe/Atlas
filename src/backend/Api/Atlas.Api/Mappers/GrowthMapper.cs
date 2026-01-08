using Atlas.Api.DTOs.Growth;
using Atlas.Domain.Entities;

namespace Atlas.Api.Mappers;

internal static class GrowthMapper
{
    public static GrowthDto ToDto(Atlas.Domain.Entities.Growth g)
    {
        var goals = (g.Goals ?? [])
            .Select(ToGoalDto)
            .ToList();

        var skills = (g.SkillsInProgress ?? [])
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var themes = (g.FeedbackThemes ?? [])
            .Select(t => new GrowthFeedbackThemeDto(t.Id, t.Title, t.Description, t.ObservedSinceLabel))
            .ToList();

        return new GrowthDto(
            g.Id,
            g.TeamMemberId,
            goals,
            skills,
            themes,
            g.FocusAreasMarkdown ?? string.Empty);
    }

    private static GrowthGoalDto ToGoalDto(GrowthGoal goal)
    {
        var actions = (goal.Actions ?? [])
            .Select(a => new GrowthGoalActionDto(a.Id, a.Title, a.DueDate, a.State, a.Priority, a.Notes, a.Evidence))
            .ToList();

        var checkIns = (goal.CheckIns ?? [])
            .Select(c => new GrowthGoalCheckInDto(c.Id, c.Date, c.Signal, c.Note))
            .OrderByDescending(c => c.Date)
            .ToList();

        return new GrowthGoalDto(
            goal.Id,
            goal.Title,
            goal.Description,
            goal.Status,
            goal.Category,
            goal.Priority,
            goal.StartDate,
            goal.TargetDate,
            goal.LastUpdatedAt,
            goal.ProgressPercent,
            NormalizeEmptyToNull(goal.Summary),
            SplitBullets(goal.SuccessCriteria),
            actions,
            checkIns);
    }

    private static string? NormalizeEmptyToNull(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static IReadOnlyList<string> SplitBullets(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        return text
            .Split('\n')
            .Select(x => x.Trim())
            .Select(x => x.StartsWith("-", StringComparison.Ordinal) ? x.TrimStart('-').Trim() : x)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}

