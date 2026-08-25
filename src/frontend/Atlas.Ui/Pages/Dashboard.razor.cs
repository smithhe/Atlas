using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Dashboard : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;

    sealed record DashRow(string Key, string DotClass, string Title, string Meta, string? Pill, string To);
    sealed record DriftRow(string Key, string Tag, string Title, string Detail, string To);

    List<DashRow> _needsAction = [];
    List<DashRow> _watchlist = [];
    List<AtlasTask> _commitmentToday = [];
    List<AtlasTask> _commitmentWeek = [];
    List<TeamMember> _teamPulse = [];
    List<DriftRow> _drift = [];
    int _staleDays = 10;
    int _staleSoonDays = 7;
    string _nowIso = "";
    string _todayIsoDate = "";

    protected override void OnInitialized()
    {
        Cache.Changed += OnCacheChanged;
        Ai.SetContext("Context: Dashboard",
        [
            new AiAction("suggest-next-action", "Suggest Next Action"),
            new AiAction("summarize-week", "Summarize Incomplete Work (week)"),
        ]);
        _ = Cache.EnsureHydratedAsync();
        Rebuild();
    }

    void OnCacheChanged() => InvokeAsync(() => { Rebuild(); StateHasChanged(); });

    void Rebuild()
    {
        var nowIso = DateTimeOffset.UtcNow.ToString("o");
        string todayIsoDate = nowIso[..10];
        _nowIso = nowIso;
        _todayIsoDate = todayIsoDate;
        _staleDays = Cache.Settings?.StaleDays ?? 10;
        _staleSoonDays = Math.Max(1, _staleDays - 3);
        int staleDays = _staleDays;
        int staleSoonDays = _staleSoonDays;
        const int dueSoonDays = 2;

        List<DashRow> needs = new();
        List<DashRow> watch = new();

        foreach (AtlasTask t in Cache.Tasks)
        {
            int ageDays = DisplayLabels.DaysBetween(t.LastTouchedIso, nowIso);
            bool isBlocked = t.Status == Models.TaskStatus.Blocked;
            bool isHigh = t.Priority is Priority.High or Priority.Critical;
            bool isStaleSoon = ageDays >= staleSoonDays && ageDays < staleDays;
            bool isStale = ageDays >= staleDays;
            Risk? linkedOpenRisk = TaskLinkedToOpenRisk(t.Risk);
            bool dueSoon = !string.IsNullOrEmpty(t.DueDate)
                           && IsIsoDateBetweenInclusive(t.DueDate!, todayIsoDate, IsoDateAddDays(todayIsoDate, dueSoonDays));
            bool highAndDrifting = isHigh && (isStale || dueSoon || linkedOpenRisk is not null);

            string pill = !string.IsNullOrEmpty(t.DueDate)
                ? $"due {(t.DueDate == todayIsoDate ? "today" : t.DueDate)}"
                : Duration.FormatDurationFromMinutes(Duration.ParseDurationText(t.EstimatedDurationText)?.TotalMinutes ?? 0);

            string why = TaskWhy(t);
            var baseRow = new DashRow(
                t.Id.ToString(),
                PriorityDot(t.Priority),
                t.Title,
                $"Task • {(t.Status is null ? "" : $"{DisplayLabels.FormatTaskStatus(t.Status)} • ")}{why}",
                pill,
                $"/tasks/{t.Id}");

            if (isBlocked || highAndDrifting) needs.Add(baseRow);
            else if (!isBlocked && isStaleSoon)
            {
                watch.Add(baseRow with
                {
                    DotClass = "dot-stale",
                    Meta = $"Task • stale soon ({ageDays}d) • {why}"
                });
            }
        }

        foreach (Risk r in Cache.Risks)
        {
            int ageDays = DisplayLabels.DaysBetween(r.LastUpdatedIso, nowIso);
            var baseRow = new DashRow(
                r.Id.ToString(),
                $"dot-{r.Severity.ToLowerInvariant()}",
                r.Title,
                $"Risk • {r.Status}",
                $"{ageDays}d",
                $"/risks/{r.Id}");
            if (r.Status == RiskStatus.Open) needs.Add(baseRow);
            else if (r.Status == RiskStatus.Watching) watch.Add(baseRow);
        }

        foreach (TeamMember m in Cache.Team)
        {
            int? freshnessDays = DisplayLabels.DaysSince(m.ActivitySnapshot.LastUpdatedIso);
            bool missingBaseline = string.IsNullOrEmpty(m.ActivitySnapshot.LastUpdatedIso);
            string freshnessText = freshnessDays is null
                ? "no update yet"
                : freshnessDays > 7 ? "no update this week" : $"updated {freshnessDays}d ago";
            var baseRow = new DashRow(
                m.Id.ToString(),
                $"dot-{m.StatusDot.ToLowerInvariant()}",
                m.Name,
                m.CurrentFocus,
                freshnessText,
                $"/team/{m.Id}");
            if (string.Equals(m.StatusDot, "Red", StringComparison.OrdinalIgnoreCase)) needs.Add(baseRow);
            if (string.Equals(m.StatusDot, "Yellow", StringComparison.OrdinalIgnoreCase) || missingBaseline) watch.Add(baseRow);
        }

        needs.Sort((a, b) => UrgencyRank(a).CompareTo(UrgencyRank(b)));
        _needsAction = needs.Take(8).ToList();
        _watchlist = watch.Take(8).ToList();

        List<AtlasTask> dueToday = Cache.Tasks.Where(t => t.DueDate == todayIsoDate).ToList();
        List<AtlasTask> dueThisWeek = Cache.Tasks.Where(t =>
            !string.IsNullOrEmpty(t.DueDate)
            && t.DueDate != todayIsoDate
            && IsIsoDateBetweenInclusive(t.DueDate!, todayIsoDate, IsoDateAddDays(todayIsoDate, 7))).ToList();
        List<AtlasTask> noDue = Cache.Tasks.Where(t => string.IsNullOrEmpty(t.DueDate)).ToList();
        List<AtlasTask> touchedDesc = noDue.OrderByDescending(t => t.LastTouchedIso).ToList();
        List<AtlasTask> todayBucket = new();
        List<AtlasTask> weekBucket = new();
        foreach (AtlasTask t in touchedDesc)
        {
            if (todayBucket.Count >= 4) break;
            todayBucket.Add(t);
        }
        foreach (AtlasTask t in noDue.Where(t => t.Priority is Priority.High or Priority.Critical))
        {
            if (todayBucket.Any(x => x.Id == t.Id)) continue;
            if (todayBucket.Count >= 6) break;
            todayBucket.Add(t);
        }
        foreach (AtlasTask t in touchedDesc)
        {
            if (todayBucket.Any(x => x.Id == t.Id)) continue;
            if (weekBucket.Count >= 6) break;
            weekBucket.Add(t);
        }
        foreach (AtlasTask t in noDue.Where(t => t.Priority == Priority.Medium))
        {
            if (todayBucket.Any(x => x.Id == t.Id) || weekBucket.Any(x => x.Id == t.Id)) continue;
            if (weekBucket.Count >= 8) break;
            weekBucket.Add(t);
        }

        _commitmentToday = dueToday.Concat(todayBucket).Take(8).ToList();
        _commitmentWeek = dueThisWeek.Concat(weekBucket).Take(10).ToList();

        Dictionary<string, int> statusRank = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Red"] = 0, ["Yellow"] = 1, ["Green"] = 2
        };
        _teamPulse = Cache.Team.OrderBy(m => statusRank.GetValueOrDefault(m.StatusDot, 9))
            .ThenByDescending(m => DisplayLabels.DaysSince(m.ActivitySnapshot.LastUpdatedIso) ?? 999)
            .ToList();

        List<DriftRow> drift = new();
        foreach (AtlasTask t in Cache.Tasks)
        {
            int ageDays = DisplayLabels.DaysBetween(t.LastTouchedIso, nowIso);
            if (ageDays >= staleDays)
            {
                var textForDocs = $"{t.Title} {t.Project ?? ""}";
                string tag = IncludesDocsKeyword(textForDocs) ? "Docs" : "Project";
                drift.Add(new DriftRow($"drift-task-stale-{t.Id}", tag, t.Title, $"Task stale ({ageDays}d since touched)", $"/tasks/{t.Id}"));
            }
            else if ((t.Priority is Priority.High or Priority.Critical) && string.IsNullOrEmpty(t.DueDate))
            {
                drift.Add(new DriftRow($"drift-task-high-nodue-{t.Id}", "Project", t.Title, "High priority task has no due date", $"/tasks/{t.Id}"));
            }
        }

        foreach (Risk r in Cache.Risks)
        {
            int ageDays = DisplayLabels.DaysBetween(r.LastUpdatedIso, nowIso);
            if (r.Status == RiskStatus.Watching && ageDays >= 14)
                drift.Add(new DriftRow($"drift-risk-watching-{r.Id}", "Risk", r.Title, $"Watching {ageDays}d since update", $"/risks/{r.Id}"));
            if (r.Status == RiskStatus.Open && ageDays >= 10)
                drift.Add(new DriftRow($"drift-risk-open-stale-{r.Id}", "Risk", r.Title, $"Open risk not updated recently ({ageDays}d)", $"/risks/{r.Id}"));
        }

        foreach (TeamMember m in Cache.Team)
        {
            int? days = DisplayLabels.DaysSince(m.ActivitySnapshot.LastUpdatedIso);
            if (days is not null && days > 7)
                drift.Add(new DriftRow($"drift-team-noupdate-{m.Id}", "Team", m.Name, "No update this week", $"/team/{m.Id}"));
            if (string.IsNullOrEmpty(m.ActivitySnapshot.LastUpdatedIso))
                drift.Add(new DriftRow($"drift-team-nobaseline-{m.Id}", "Team", m.Name, "No activity yet", $"/team/{m.Id}"));
        }

        foreach (Project p in Cache.Projects)
        {
            if (p.Health is HealthSignal.Yellow or HealthSignal.Red)
            {
                drift.Add(new DriftRow($"drift-proj-health-{p.Id}", "Project", p.Name, $"Project health: {p.Health}", $"/projects/{p.Id}"));
            }

            if (!string.IsNullOrEmpty(p.LastUpdatedIso))
            {
                int ageDays = DisplayLabels.DaysBetween(p.LastUpdatedIso!, nowIso);
                if (ageDays > 7)
                    drift.Add(new DriftRow($"drift-proj-stale-{p.Id}", "Project", p.Name, $"No recent project update ({ageDays}d)", $"/projects/{p.Id}"));
            }
        }

        _drift = drift.Take(10).ToList();
    }

    Risk? TaskLinkedToOpenRisk(string? taskRisk)
    {
        if (string.IsNullOrWhiteSpace(taskRisk)) return null;
        string needle = taskRisk.Trim().ToLowerInvariant();
        List<Risk> open = Cache.Risks.Where(r => r.Status == RiskStatus.Open).ToList();
        Risk? exact = open.FirstOrDefault(r => r.Title.Trim().ToLowerInvariant() == needle);
        if (exact is not null) return exact;
        return open.FirstOrDefault(r =>
        {
            string title = r.Title.Trim().ToLowerInvariant();
            return title.Contains(needle) || needle.Contains(title);
        });
    }

    static string TaskWhy(AtlasTask t)
    {
        List<string> parts = new();
        if (!string.IsNullOrEmpty(t.Risk)) parts.Add($"Risk: {t.Risk}");
        if (!string.IsNullOrEmpty(t.Project)) parts.Add(t.Project!);
        if (parts.Count > 0) return string.Join(" • ", parts);
        return $"{t.Priority}{(t.Status is null ? "" : $" • {DisplayLabels.FormatTaskStatus(t.Status)}")}";
    }

    static int UrgencyRank(DashRow row)
    {
        string meta = row.Meta.ToLowerInvariant();
        if (meta.StartsWith("task") && meta.Contains("blocked")) return 0;
        if (meta.StartsWith("risk") && meta.Contains("open")) return 1;
        return 2;
    }

    static string IsoDateAddDays(string isoDate, int days)
    {
        DateTime d = DateTime.Parse($"{isoDate}T00:00:00.000Z").ToUniversalTime();
        return d.AddDays(days).ToString("yyyy-MM-dd");
    }

    static bool IsIsoDateBetweenInclusive(string isoDate, string start, string end) =>
        string.CompareOrdinal(isoDate, start) >= 0 && string.CompareOrdinal(isoDate, end) <= 0;

    static bool IncludesDocsKeyword(string text)
    {
        string t = text.ToLowerInvariant();
        return new[] { "docs", "documentation", "onboarding", "runbook", "playbook" }.Any(k => t.Contains(k));
    }

    static string PriorityDot(Priority p) => p switch
    {
        Priority.Critical => "dot-critical",
        Priority.High => "dot-high",
        Priority.Medium => "dot-medium",
        Priority.Low => "dot-low",
        _ => "dot-stale"
    };

    string CommitmentTodayPill(AtlasTask t) =>
        !string.IsNullOrEmpty(t.DueDate)
            ? $"due {(t.DueDate == _todayIsoDate ? "today" : t.DueDate)}"
            : $"{DisplayLabels.DaysBetween(t.LastTouchedIso, _nowIso)}d";

    string CommitmentWeekPill(AtlasTask t) =>
        !string.IsNullOrEmpty(t.DueDate)
            ? $"due {t.DueDate}"
            : $"{DisplayLabels.DaysBetween(t.LastTouchedIso, _nowIso)}d";

    void AskAiAttention() => Ai.RunAction("suggest-next-action");

    void GoTask(Guid id) => Nav.NavigateTo($"/tasks/{id}");
    void GoTeam(Guid id) => Nav.NavigateTo($"/team/{id}");

    void Go(string? to)
    {
        if (!string.IsNullOrEmpty(to)) Nav.NavigateTo(to);
    }

    public void Dispose() => Cache.Changed -= OnCacheChanged;
}
