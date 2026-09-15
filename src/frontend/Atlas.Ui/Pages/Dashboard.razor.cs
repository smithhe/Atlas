using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class Dashboard : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private IAiStateService _ai { get; set; } = null!;

        private sealed record DashRow(string Key, string DotClass, string Title, string Meta, string? Pill, string To);

        private sealed record DriftRow(string Key, string Tag, string Title, string Detail, string To);

        private List<DashRow> NeedsAction { get; set; } = [];
        private List<DashRow> Watchlist { get; set; } = [];
        private List<AtlasTask> CommitmentToday { get; set; } = [];
        private List<AtlasTask> CommitmentWeek { get; set; } = [];
        private List<TeamMember> TeamPulse { get; set; } = [];
        private List<DriftRow> Drift { get; set; } = [];
        private int StaleDays { get; set; } = 10;
        private int StaleSoonDays { get; set; } = 7;
        private string NowIso { get; set; } = "";
        private string TodayIsoDate { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            this._cache.Changed += OnCacheChangedAsync;
            this._ai.SetContext("Context: Dashboard",
            [
                new AiAction("suggest-next-action", "Suggest Next Action"),
                new AiAction("summarize-week", "Summarize Incomplete Work (week)"),
            ]);
            await this._cache.EnsureHydratedAsync();
            Rebuild();
        }

        private async void OnCacheChangedAsync()
        {
            try
            {
                await InvokeAsync(() =>
                {
                    Rebuild();
                    StateHasChanged();
                });
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        }

        private void Rebuild()
        {
            var nowIso = DateTimeOffset.UtcNow.ToString("o");
            var todayIsoDate = nowIso[..10];
            this.NowIso = nowIso;
            this.TodayIsoDate = todayIsoDate;
            this.StaleDays = this._cache.Settings?.StaleDays ?? 10;
            this.StaleSoonDays = Math.Max(1, this.StaleDays - 3);
            var staleDays = this.StaleDays;
            var staleSoonDays = this.StaleSoonDays;
            const int dueSoonDays = 2;

            List<DashRow> needs = new();
            List<DashRow> watch = new();

            foreach (AtlasTask t in this._cache.Tasks)
            {
                var ageDays = DisplayLabels.DaysBetween(t.LastTouchedIso, nowIso);
                var isBlocked = t.Status == Models.TaskStatus.Blocked;
                var isHigh = t.Priority is Priority.High or Priority.Critical;
                var isStaleSoon = ageDays >= staleSoonDays && ageDays < staleDays;
                var isStale = ageDays >= staleDays;
                Risk? linkedOpenRisk = this.TaskLinkedToOpenRisk(t.RiskId);
                var dueSoon = !string.IsNullOrEmpty(t.DueDate)
                              && IsIsoDateBetweenInclusive(t.DueDate!, todayIsoDate, IsoDateAddDays(todayIsoDate, dueSoonDays));
                var highAndDrifting = isHigh && (isStale || dueSoon || linkedOpenRisk is not null);

                var pill = !string.IsNullOrEmpty(t.DueDate)
                    ? $"due {(t.DueDate == todayIsoDate ? "today" : t.DueDate)}"
                    : Duration.FormatDurationFromMinutes(Duration.ParseDurationText(t.EstimatedDurationText)?.TotalMinutes ?? 0);

                var why = TaskWhy(t);
                var baseRow = new DashRow(
                    t.Id.ToString(),
                    PriorityDot(t.Priority),
                    t.Title,
                    $"Task • {(t.Status is null ? "" : $"{DisplayLabels.FormatTaskStatus(t.Status)} • ")}{why}",
                    pill,
                    $"/tasks/{t.Id}");

                if (isBlocked || highAndDrifting)
                {
                    needs.Add(baseRow);
                }
                else if (!isBlocked && isStaleSoon)
                {
                    watch.Add(baseRow with
                    {
                        DotClass = "dot-stale",
                        Meta = $"Task • stale soon ({ageDays}d) • {why}"
                    });
                }
            }

            foreach (Risk r in this._cache.Risks)
            {
                var ageDays = DisplayLabels.DaysBetween(r.LastUpdatedIso, nowIso);
                var baseRow = new DashRow(
                    r.Id.ToString(),
                    $"dot-{r.Severity.ToLowerInvariant()}",
                    r.Title,
                    $"Risk • {r.Status}",
                    $"{ageDays}d",
                    $"/risks/{r.Id}");
                if (r.Status == RiskStatus.Open)
                {
                    needs.Add(baseRow);
                }
                else if (r.Status == RiskStatus.Watching)
                {
                    watch.Add(baseRow);
                }
            }

            foreach (TeamMember m in this._cache.Team)
            {
                var freshnessDays = DisplayLabels.DaysSince(m.ActivitySnapshot.LastUpdatedIso);
                var missingBaseline = string.IsNullOrEmpty(m.ActivitySnapshot.LastUpdatedIso);
                var freshnessText = freshnessDays is null
                    ? "no update yet"
                    : freshnessDays > 7 ? "no update this week" : $"updated {freshnessDays}d ago";
                var baseRow = new DashRow(
                    m.Id.ToString(),
                    $"dot-{m.StatusDot.ToLowerInvariant()}",
                    m.Name,
                    m.CurrentFocus,
                    freshnessText,
                    $"/team/{m.Id}");
                if (string.Equals(m.StatusDot, "Red", StringComparison.OrdinalIgnoreCase))
                {
                    needs.Add(baseRow);
                }

                if (string.Equals(m.StatusDot, "Yellow", StringComparison.OrdinalIgnoreCase) || missingBaseline)
                {
                    watch.Add(baseRow);
                }
            }

            needs.Sort((a, b) => UrgencyRank(a).CompareTo(UrgencyRank(b)));
            this.NeedsAction = needs.Take(8).ToList();
            this.Watchlist = watch.Take(8).ToList();

            var dueToday = this._cache.Tasks.Where(t => t.DueDate == todayIsoDate).ToList();
            var dueThisWeek = this._cache.Tasks.Where(t =>
                !string.IsNullOrEmpty(t.DueDate)
                && t.DueDate != todayIsoDate
                && IsIsoDateBetweenInclusive(t.DueDate!, todayIsoDate, IsoDateAddDays(todayIsoDate, 7))).ToList();
            var noDue = this._cache.Tasks.Where(t => string.IsNullOrEmpty(t.DueDate)).ToList();
            var touchedDesc = noDue.OrderByDescending(t => t.LastTouchedIso).ToList();
            List<AtlasTask> todayBucket = new();
            List<AtlasTask> weekBucket = new();
            foreach (AtlasTask t in touchedDesc)
            {
                if (todayBucket.Count >= 4)
                {
                    break;
                }

                todayBucket.Add(t);
            }
            foreach (AtlasTask t in noDue.Where(t => t.Priority is Priority.High or Priority.Critical))
            {
                if (todayBucket.Any(x => x.Id == t.Id))
                {
                    continue;
                }

                if (todayBucket.Count >= 6)
                {
                    break;
                }

                todayBucket.Add(t);
            }
            foreach (AtlasTask t in touchedDesc)
            {
                if (todayBucket.Any(x => x.Id == t.Id))
                {
                    continue;
                }

                if (weekBucket.Count >= 6)
                {
                    break;
                }

                weekBucket.Add(t);
            }
            foreach (AtlasTask t in noDue.Where(t => t.Priority == Priority.Medium))
            {
                if (todayBucket.Any(x => x.Id == t.Id) || weekBucket.Any(x => x.Id == t.Id))
                {
                    continue;
                }

                if (weekBucket.Count >= 8)
                {
                    break;
                }

                weekBucket.Add(t);
            }

            this.CommitmentToday = dueToday.Concat(todayBucket).Take(8).ToList();
            this.CommitmentWeek = dueThisWeek.Concat(weekBucket).Take(10).ToList();

            Dictionary<string, int> statusRank = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Red"] = 0, ["Yellow"] = 1, ["Green"] = 2
            };
            this.TeamPulse = this._cache.Team.OrderBy(m => statusRank.GetValueOrDefault(m.StatusDot, 9))
                .ThenByDescending(m => DisplayLabels.DaysSince(m.ActivitySnapshot.LastUpdatedIso) ?? 999)
                .ToList();

            List<DriftRow> drift = new();
            foreach (AtlasTask t in this._cache.Tasks)
            {
                var ageDays = DisplayLabels.DaysBetween(t.LastTouchedIso, nowIso);
                if (ageDays >= staleDays)
                {
                    var textForDocs = $"{t.Title} {t.Project ?? ""}";
                    var tag = IncludesDocsKeyword(textForDocs) ? "Docs" : "Project";
                    drift.Add(new DriftRow($"drift-task-stale-{t.Id}", tag, t.Title, $"Task stale ({ageDays}d since touched)", $"/tasks/{t.Id}"));
                }
                else if ((t.Priority is Priority.High or Priority.Critical) && string.IsNullOrEmpty(t.DueDate))
                {
                    drift.Add(new DriftRow($"drift-task-high-nodue-{t.Id}", "Project", t.Title, "High priority task has no due date", $"/tasks/{t.Id}"));
                }
            }

            foreach (Risk r in this._cache.Risks)
            {
                var ageDays = DisplayLabels.DaysBetween(r.LastUpdatedIso, nowIso);
                if (r.Status == RiskStatus.Watching && ageDays >= 14)
                {
                    drift.Add(new DriftRow($"drift-risk-watching-{r.Id}", "Risk", r.Title, $"Watching {ageDays}d since update", $"/risks/{r.Id}"));
                }

                if (r.Status == RiskStatus.Open && ageDays >= 10)
                {
                    drift.Add(new DriftRow($"drift-risk-open-stale-{r.Id}", "Risk", r.Title, $"Open risk not updated recently ({ageDays}d)", $"/risks/{r.Id}"));
                }
            }

            foreach (TeamMember m in this._cache.Team)
            {
                var days = DisplayLabels.DaysSince(m.ActivitySnapshot.LastUpdatedIso);
                if (days is not null && days > 7)
                {
                    drift.Add(new DriftRow($"drift-team-noupdate-{m.Id}", "Team", m.Name, "No update this week", $"/team/{m.Id}"));
                }

                if (string.IsNullOrEmpty(m.ActivitySnapshot.LastUpdatedIso))
                {
                    drift.Add(new DriftRow($"drift-team-nobaseline-{m.Id}", "Team", m.Name, "No activity yet", $"/team/{m.Id}"));
                }
            }

            foreach (Project p in this._cache.Projects)
            {
                if (p.Health is HealthSignal.Yellow or HealthSignal.Red)
                {
                    drift.Add(new DriftRow($"drift-proj-health-{p.Id}", "Project", p.Name, $"Project health: {p.Health}", $"/projects/{p.Id}"));
                }

                if (!string.IsNullOrEmpty(p.LastUpdatedIso))
                {
                    var ageDays = DisplayLabels.DaysBetween(p.LastUpdatedIso!, nowIso);
                    if (ageDays > 7)
                    {
                        drift.Add(new DriftRow($"drift-proj-stale-{p.Id}", "Project", p.Name, $"No recent project update ({ageDays}d)", $"/projects/{p.Id}"));
                    }
                }
            }

            this.Drift = drift.Take(10).ToList();
        }

        private Risk? TaskLinkedToOpenRisk(Guid? riskId)
        {
            if (riskId is not Guid id)
            {
                return null;
            }

            return this._cache.Risks.FirstOrDefault(r => r.Id == id && r.Status == RiskStatus.Open);
        }

        private static string TaskWhy(AtlasTask t)
        {
            List<string> parts = new();
            if (!string.IsNullOrEmpty(t.Risk))
            {
                parts.Add($"Risk: {t.Risk}");
            }

            if (!string.IsNullOrEmpty(t.Project))
            {
                parts.Add(t.Project!);
            }

            if (parts.Count > 0)
            {
                return string.Join(" • ", parts);
            }

            return $"{t.Priority}{(t.Status is null ? "" : $" • {DisplayLabels.FormatTaskStatus(t.Status)}")}";
        }

        private static int UrgencyRank(DashRow row)
        {
            var meta = row.Meta.ToLowerInvariant();
            if (meta.StartsWith("task") && meta.Contains("blocked"))
            {
                return 0;
            }

            if (meta.StartsWith("risk") && meta.Contains("open"))
            {
                return 1;
            }

            return 2;
        }

        private static string IsoDateAddDays(string isoDate, int days)
        {
            DateTime d = DateTime.Parse($"{isoDate}T00:00:00.000Z").ToUniversalTime();
            return d.AddDays(days).ToString("yyyy-MM-dd");
        }

        private static bool IsIsoDateBetweenInclusive(string isoDate, string start, string end) =>
            string.CompareOrdinal(isoDate, start) >= 0 && string.CompareOrdinal(isoDate, end) <= 0;

        private static bool IncludesDocsKeyword(string text)
        {
            var t = text.ToLowerInvariant();
            return new[] { "docs", "documentation", "onboarding", "runbook", "playbook" }.Any(k => t.Contains(k));
        }

        private static string PriorityDot(Priority p) => p switch
        {
            Priority.Critical => "dot-critical",
            Priority.High => "dot-high",
            Priority.Medium => "dot-medium",
            Priority.Low => "dot-low",
            _ => "dot-stale"
        };

        private string CommitmentTodayPill(AtlasTask t) =>
            !string.IsNullOrEmpty(t.DueDate)
                ? $"due {(t.DueDate == this.TodayIsoDate ? "today" : t.DueDate)}"
                : $"{DisplayLabels.DaysBetween(t.LastTouchedIso, this.NowIso)}d";

        private string CommitmentWeekPill(AtlasTask t) =>
            !string.IsNullOrEmpty(t.DueDate)
                ? $"due {t.DueDate}"
                : $"{DisplayLabels.DaysBetween(t.LastTouchedIso, this.NowIso)}d";

        private void AskAiAttention() => this._ai.RunAction("suggest-next-action");

        private void GoTask(Guid id) => this._nav.NavigateTo($"/tasks/{id}");
        private void GoTeam(Guid id) => this._nav.NavigateTo($"/team/{id}");

        private void Go(string? to)
        {
            if (!string.IsNullOrEmpty(to))
            {
                this._nav.NavigateTo(to);
            }
        }

        public void Dispose() => this._cache.Changed -= OnCacheChangedAsync;
    }
}
