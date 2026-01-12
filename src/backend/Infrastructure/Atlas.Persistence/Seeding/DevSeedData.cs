using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Domain.ValueObjects;

namespace Atlas.Persistence.Seeding;

internal sealed record DevSeedModel(
    Settings Settings,
    IReadOnlyList<ProductOwner> ProductOwners,
    IReadOnlyList<TeamMember> TeamMembers,
    IReadOnlyList<TeamNote> TeamNotes,
    IReadOnlyList<Project> Projects,
    IReadOnlyList<ProjectTeamMember> ProjectTeamMembers,
    IReadOnlyList<ProjectTag> ProjectTags,
    IReadOnlyList<ProjectLinkItem> ProjectLinks,
    IReadOnlyList<Risk> Risks,
    IReadOnlyList<RiskHistoryEntry> RiskHistory,
    IReadOnlyList<RiskTeamMember> RiskTeamMembers,
    IReadOnlyList<TaskItem> Tasks,
    IReadOnlyList<TaskDependency> TaskDependencies,
    IReadOnlyList<TeamMemberRisk> TeamMemberRisks,
    IReadOnlyList<Growth> GrowthPlans,
    IReadOnlyList<GrowthGoal> GrowthGoals,
    IReadOnlyList<GrowthGoalAction> GrowthGoalActions,
    IReadOnlyList<GrowthGoalCheckIn> GrowthGoalCheckIns,
    IReadOnlyList<GrowthFeedbackTheme> GrowthFeedbackThemes,
    IReadOnlyList<GrowthSkillInProgress> GrowthSkillsInProgress);

internal static class DevSeedData
{
    public static DevSeedModel Build(DateTimeOffset now)
    {
        static DateOnly DateOnlyUtcDaysFromNow(int days)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days));
        }

        DateTimeOffset NowMinusDays(int days) => now.AddDays(-days);

        var settings = new Settings
        {
            Id = SeedIds.Settings,
            StaleDays = 10,
            DefaultAiManualOnly = true,
            Theme = Theme.Dark,
            AzureDevOpsBaseUrl = "(placeholder)"
        };

        var productOwners = new List<ProductOwner>
        {
            new() { Id = SeedIds.ProductOwnerDana, Name = "Dana" },
            new() { Id = SeedIds.ProductOwnerCharlie, Name = "Charlie" }
        };

        var teamMembers = new List<TeamMember>
        {
            new()
            {
                Id = SeedIds.TeamMemberAlice,
                Name = "Alice",
                Role = "Senior Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = "Checkout flow performance",
                Profile = new TeamMemberProfile { TimeZone = "PT", TypicalHours = "9–5" },
                Signals = new TeamMemberSignals { Load = LoadSignal.Heavy, Delivery = DeliverySignal.OnTrack, SupportNeeded = SupportNeededSignal.Low }
            },
            new()
            {
                Id = SeedIds.TeamMemberBob,
                Name = "Bob",
                Role = "Engineer",
                StatusDot = StatusDot.Yellow,
                CurrentFocus = "Refactor proposal review + instrumentation",
                Profile = new TeamMemberProfile { TimeZone = "ET", TypicalHours = "10–6" },
                Signals = new TeamMemberSignals { Load = LoadSignal.Normal, Delivery = DeliverySignal.OnTrack, SupportNeeded = SupportNeededSignal.Medium }
            },
            new()
            {
                Id = SeedIds.TeamMemberCharlie,
                Name = "Charlie",
                Role = "Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = "On-call stability",
                Profile = new TeamMemberProfile { TimeZone = "CT", TypicalHours = "9–5" },
                Signals = new TeamMemberSignals { Load = LoadSignal.Light, Delivery = DeliverySignal.OnTrack, SupportNeeded = SupportNeededSignal.Low }
            },
            new()
            {
                Id = SeedIds.TeamMemberDana,
                Name = "Dana",
                Role = "Tech Lead",
                StatusDot = StatusDot.Green,
                CurrentFocus = "Project planning + dependencies",
                Profile = new TeamMemberProfile { TimeZone = "PT", TypicalHours = "8–4" },
                Signals = new TeamMemberSignals { Load = LoadSignal.Heavy, Delivery = DeliverySignal.AtRisk, SupportNeeded = SupportNeededSignal.Medium }
            },
            new()
            {
                Id = SeedIds.TeamMemberEvan,
                Name = "Evan",
                Role = "Engineer",
                StatusDot = StatusDot.Red,
                CurrentFocus = "Blocked on environment flakiness",
                Profile = new TeamMemberProfile { TimeZone = "ET", TypicalHours = "9–5" },
                Signals = new TeamMemberSignals { Load = LoadSignal.Heavy, Delivery = DeliverySignal.Blocked, SupportNeeded = SupportNeededSignal.High }
            }
        };

        var teamNotes = new List<TeamNote>
        {
            // Alice notes (PinnedOrder derived from UI's pinned list order)
            new()
            {
                Id = SeedIds.NoteAliceMarkdownDemo,
                TeamMemberId = SeedIds.TeamMemberAlice,
                CreatedAt = NowMinusDays(0),
                Type = NoteType.Quick,
                Text =
                    "# Markdown demo\n\n" +
                    "This note supports **bold**, _italic_, and `inline code`.\n\n" +
                    "## Lists\n" +
                    "- Bullet one\n" +
                    "- Bullet two\n" +
                    "  - Nested bullet\n\n" +
                    "## Task list (GFM)\n" +
                    "- [x] Shipped initial Markdown rendering\n" +
                    "- [ ] Add editor preview toggle (later)\n\n" +
                    "## Code block\n" +
                    "```ts\n" +
                    "function greet(name: string) {\n" +
                    "  return `Hello, ${name}!`\n" +
                    "}\n" +
                    "```\n\n" +
                    "Link: [React Markdown](https://github.com/remarkjs/react-markdown)\n"
            },
            new()
            {
                Id = SeedIds.NoteAlice7,
                TeamMemberId = SeedIds.TeamMemberAlice,
                CreatedAt = NowMinusDays(0),
                Type = NoteType.Standup,
                Text =
                    "Today: validate p95 improvement after the query batching change.\n" +
                    "Next: re-run load test with realistic cart sizes and confirm we did not regress error rates.\n" +
                    "Watchouts: cache invalidation + retry storms if downstream starts timing out.",
                PinnedOrder = 0
            },
            new()
            {
                Id = SeedIds.NoteAlice6,
                TeamMemberId = SeedIds.TeamMemberAlice,
                CreatedAt = NowMinusDays(1),
                Type = NoteType.Concern,
                Text =
                    "We still see sporadic spikes in checkout latency when inventory calls are slow.\n" +
                    "Hypothesis: fallback path is doing extra DB reads + serializing large payloads.\n" +
                    "Action: capture a trace sample set and compare \"fast\" vs \"slow\" requests.",
                PinnedOrder = 1
            },
            new()
            {
                Id = SeedIds.NoteAlice5,
                TeamMemberId = SeedIds.TeamMemberAlice,
                CreatedAt = NowMinusDays(3),
                Type = NoteType.Praise,
                Text =
                    "Excellent collaboration with QA on reproducing the worst-case cart scenario.\n" +
                    "The repro doc is clear and should help future on-calls.\n" +
                    "Keep doing write-ups like this — they scale.",
                PinnedOrder = 2
            },
            new()
            {
                Id = SeedIds.NoteAlice4,
                TeamMemberId = SeedIds.TeamMemberAlice,
                CreatedAt = NowMinusDays(5),
                Type = NoteType.Progress,
                Text =
                    "Identified top contributors to checkout time:\n" +
                    "- redundant product lookups per line item\n" +
                    "- N+1 tax calculation calls\n" +
                    "- serialization overhead in the response builder\n" +
                    "Next step: land batching behind a feature flag and monitor.",
                PinnedOrder = 3
            },
            new()
            {
                Id = SeedIds.NoteAlice3,
                TeamMemberId = SeedIds.TeamMemberAlice,
                CreatedAt = NowMinusDays(7),
                Type = NoteType.Standup,
                Text =
                    "Blocked briefly by flaky perf env; workaround was to pin the dataset snapshot.\n" +
                    "Follow-up: we should automate dataset refresh + pinning so perf runs are reproducible."
            },
            new()
            {
                Id = SeedIds.NoteAlice2,
                TeamMemberId = SeedIds.TeamMemberAlice,
                CreatedAt = NowMinusDays(9),
                Type = NoteType.Blocker,
                Text =
                    "Load test run failed due to intermittent 502s from the gateway.\n" +
                    "Need infra assist to confirm whether this is rate limiting or an upstream timeout.\n" +
                    "In the meantime: run smaller-scale tests locally and capture traces.",
                PinnedOrder = 4
            },
            new()
            {
                Id = SeedIds.NoteAlice1,
                TeamMemberId = SeedIds.TeamMemberAlice,
                CreatedAt = NowMinusDays(2),
                Type = NoteType.Progress,
                Text = "Great progress on profiling + query batching."
            },

            // Bob
            new()
            {
                Id = SeedIds.NoteBob1,
                TeamMemberId = SeedIds.TeamMemberBob,
                CreatedAt = NowMinusDays(10),
                Type = NoteType.Concern,
                Text = "Needs tighter definition of “done” before starting refactor.",
                PinnedOrder = 0
            },

            // Dana
            new()
            {
                Id = SeedIds.NoteDana1,
                TeamMemberId = SeedIds.TeamMemberDana,
                CreatedAt = NowMinusDays(1),
                Type = NoteType.Standup,
                Text = "Flagged cross-team dependency risk; will schedule alignment.",
                PinnedOrder = 0
            },

            // Evan
            new()
            {
                Id = SeedIds.NoteEvan1,
                TeamMemberId = SeedIds.TeamMemberEvan,
                CreatedAt = NowMinusDays(3),
                Type = NoteType.Blocker,
                Text = "CI environment flakes on integration tests; needs triage.",
                PinnedOrder = 0
            }
        };

        var projects = new List<Project>
        {
            new()
            {
                Id = SeedIds.ProjectCorePlatform,
                Name = "Core Platform",
                Summary = "Shared services, reliability, and performance initiatives.",
                Description = "Shared services, reliability, and performance initiatives across core systems.",
                Status = ProjectStatus.Active,
                Health = HealthSignal.Green,
                TargetDate = new DateOnly(2026, 3, 15),
                Priority = Priority.High,
                ProductOwnerId = SeedIds.ProductOwnerDana,
                LastUpdatedAt = NowMinusDays(0),
                LatestCheckIn = new ProjectCheckIn(new DateOnly(2025, 12, 30),
                    "Checkout performance work is tracking; next focus is stabilizing inventory timeouts and tightening refactor success metrics.")
            },
            new()
            {
                Id = SeedIds.ProjectDevEx,
                Name = "DevEx",
                Summary = "Developer experience: tooling, onboarding, and CI hygiene.",
                Description = "Developer experience: onboarding, tooling, and CI stability improvements.",
                Status = ProjectStatus.Active,
                Health = HealthSignal.Yellow,
                TargetDate = new DateOnly(2026, 2, 7),
                Priority = Priority.Medium,
                ProductOwnerId = SeedIds.ProductOwnerCharlie,
                LastUpdatedAt = NowMinusDays(2),
                LatestCheckIn = new ProjectCheckIn(new DateOnly(2025, 12, 29),
                    "Onboarding doc refresh in progress; CI flakiness mitigation plan drafted with ownership rotation.")
            }
        };

        var projectTeamMembers = new List<ProjectTeamMember>
        {
            new() { ProjectId = SeedIds.ProjectCorePlatform, TeamMemberId = SeedIds.TeamMemberAlice },
            new() { ProjectId = SeedIds.ProjectCorePlatform, TeamMemberId = SeedIds.TeamMemberBob },
            new() { ProjectId = SeedIds.ProjectCorePlatform, TeamMemberId = SeedIds.TeamMemberDana },

            new() { ProjectId = SeedIds.ProjectDevEx, TeamMemberId = SeedIds.TeamMemberCharlie },
            new() { ProjectId = SeedIds.ProjectDevEx, TeamMemberId = SeedIds.TeamMemberEvan }
        };

        var projectTags = new List<ProjectTag>
        {
            new() { ProjectId = SeedIds.ProjectCorePlatform, Value = "platform" },
            new() { ProjectId = SeedIds.ProjectCorePlatform, Value = "reliability" },
            new() { ProjectId = SeedIds.ProjectCorePlatform, Value = "performance" },

            new() { ProjectId = SeedIds.ProjectDevEx, Value = "tech-debt" },
            new() { ProjectId = SeedIds.ProjectDevEx, Value = "ci" },
            new() { ProjectId = SeedIds.ProjectDevEx, Value = "onboarding" }
        };

        var projectLinks = new List<ProjectLinkItem>
        {
            new() { ProjectId = SeedIds.ProjectCorePlatform, Label = "Spec", Url = "(placeholder)" },
            new() { ProjectId = SeedIds.ProjectCorePlatform, Label = "Dashboard", Url = "(placeholder)" },
            new() { ProjectId = SeedIds.ProjectCorePlatform, Label = "Repo", Url = "(placeholder)" },

            new() { ProjectId = SeedIds.ProjectDevEx, Label = "Spec", Url = "(placeholder)" },
            new() { ProjectId = SeedIds.ProjectDevEx, Label = "Dashboard", Url = "(placeholder)" },
            new() { ProjectId = SeedIds.ProjectDevEx, Label = "Repo", Url = "(placeholder)" }
        };

        var risks = new List<Risk>
        {
            new()
            {
                Id = SeedIds.RiskRefactorJustification,
                Title = "Refactor justification",
                Status = RiskStatus.Open,
                Severity = SeverityLevel.High,
                ProjectId = SeedIds.ProjectCorePlatform,
                Description =
                    "## Summary\n\n" +
                    "We may spend multiple sprints refactoring without clear ROI or success metrics.\n\n" +
                    "### What we need before starting\n" +
                    "- Define **success metrics** (e.g. p95 latency, error rate, cost).\n" +
                    "- Agree on **scope boundaries** (what is explicitly *out of scope*).\n" +
                    "- Write a short plan for **migration/deprecation**.\n\n" +
                    "> Suggestion: treat this like a product bet — make the hypothesis testable.\n",
                Evidence =
                    "### Signals we’ve seen\n" +
                    "- Prior refactors expanded scope.\n" +
                    "- Uncertain performance wins.\n" +
                    "- Unclear deprecation plan.\n\n" +
                    "### Useful references\n" +
                    "- [Refactor checklist](https://example.com) (placeholder)\n\n" +
                    "### Example acceptance criteria\n" +
                    "```text\n" +
                    "p95 checkout latency improves by 15% with no increase in error rate.\n" +
                    "```",
                LastUpdatedAt = NowMinusDays(2)
            },
            new()
            {
                Id = SeedIds.RiskOnboardingDrift,
                Title = "Onboarding drift",
                Status = RiskStatus.Watching,
                Severity = SeverityLevel.Medium,
                ProjectId = SeedIds.ProjectDevEx,
                Description = "Onboarding docs lag behind current reality, increasing time-to-productivity.",
                Evidence = "Multiple new hires hit the same setup issues; tribal knowledge in DMs.",
                LastUpdatedAt = NowMinusDays(11)
            },
            new()
            {
                Id = SeedIds.RiskOverCapacity,
                Title = "Sustained over-capacity across multiple streams",
                Status = RiskStatus.Watching,
                Severity = SeverityLevel.High,
                ProjectId = SeedIds.ProjectCorePlatform,
                Description = "Capacity constraints may create delivery risk and increase burnout probability if left unaddressed.",
                Evidence = "PR cycle time increasing; missed follow-ups due to context switching; rising WIP.",
                LastUpdatedAt = NowMinusDays(3)
            }
        };

        var riskHistory = new List<RiskHistoryEntry>
        {
            new()
            {
                Id = SeedIds.RiskHistory1,
                RiskId = SeedIds.RiskRefactorJustification,
                CreatedAt = NowMinusDays(5),
                Text = "Risk created after refactor proposal discussion."
            },
            new()
            {
                Id = SeedIds.RiskHistory2,
                RiskId = SeedIds.RiskOnboardingDrift,
                CreatedAt = NowMinusDays(12),
                Text = "Noticed repeated setup failures in week 1."
            },
            new()
            {
                Id = SeedIds.RiskHistory3,
                RiskId = SeedIds.RiskOverCapacity,
                CreatedAt = NowMinusDays(4),
                Text = "Added as a global risk to track mitigation experiments (ownership + load balancing)."
            }
        };

        var riskTeamMembers = new List<RiskTeamMember>
        {
            new() { RiskId = SeedIds.RiskRefactorJustification, TeamMemberId = SeedIds.TeamMemberBob },
            new() { RiskId = SeedIds.RiskRefactorJustification, TeamMemberId = SeedIds.TeamMemberDana },

            new() { RiskId = SeedIds.RiskOnboardingDrift, TeamMemberId = SeedIds.TeamMemberAlice },
            new() { RiskId = SeedIds.RiskOnboardingDrift, TeamMemberId = SeedIds.TeamMemberCharlie }
        };

        var tasks = new List<TaskItem>
        {
            new()
            {
                Id = SeedIds.Task1,
                Title = "Review refactor proposal",
                Priority = Priority.High,
                Status = Atlas.Domain.Enums.TaskStatus.NotStarted,
                ProjectId = SeedIds.ProjectCorePlatform,
                RiskId = SeedIds.RiskRefactorJustification,
                EstimatedDurationText = "2d",
                EstimateConfidence = Confidence.Medium,
                ActualDurationText = null,
                Notes = "Focus: validate scope, sequencing, and ROI. Capture assumptions + risks.",
                LastTouchedAt = NowMinusDays(1)
            },
            new()
            {
                Id = SeedIds.Task2,
                Title = "Update onboarding docs",
                Priority = Priority.Medium,
                Status = Atlas.Domain.Enums.TaskStatus.NotStarted,
                ProjectId = SeedIds.ProjectDevEx,
                RiskId = SeedIds.RiskOnboardingDrift,
                EstimatedDurationText = "1d",
                EstimateConfidence = Confidence.Low,
                ActualDurationText = null,
                Notes = "Collect top “gotchas” from the last two onboards. Add checklists.",
                LastTouchedAt = NowMinusDays(11)
            },
            new()
            {
                Id = SeedIds.Task3,
                Title = "Review pull requests",
                Priority = Priority.Medium,
                Status = Atlas.Domain.Enums.TaskStatus.NotStarted,
                ProjectId = SeedIds.ProjectCorePlatform,
                EstimatedDurationText = "2h",
                EstimateConfidence = Confidence.High,
                ActualDurationText = null,
                Notes = "Batch reviews. Leave clear next steps for each PR.",
                LastTouchedAt = NowMinusDays(0)
            },
            new()
            {
                Id = SeedIds.Task4,
                Title = "Triage flaky test failure",
                Priority = Priority.Low,
                Status = Atlas.Domain.Enums.TaskStatus.NotStarted,
                ProjectId = SeedIds.ProjectDevEx,
                EstimatedDurationText = "45m",
                EstimateConfidence = Confidence.Medium,
                ActualDurationText = null,
                Notes = "Quick repro + isolate recent changes. Capture next steps.",
                LastTouchedAt = NowMinusDays(9)
            },
            new()
            {
                Id = SeedIds.Task5,
                Title = "Prep 1:1 agenda for next week",
                Priority = Priority.Low,
                Status = Atlas.Domain.Enums.TaskStatus.NotStarted,
                EstimatedDurationText = "30m",
                EstimateConfidence = Confidence.High,
                ActualDurationText = null,
                Notes = "Draft topics + collect feedback items.",
                LastTouchedAt = NowMinusDays(2)
            },
            new()
            {
                Id = SeedIds.Task6,
                Title = "Review risk mitigations backlog",
                Priority = Priority.Medium,
                Status = Atlas.Domain.Enums.TaskStatus.NotStarted,
                ProjectId = SeedIds.ProjectCorePlatform,
                RiskId = SeedIds.RiskRefactorJustification,
                EstimatedDurationText = "1h",
                EstimateConfidence = Confidence.Medium,
                ActualDurationText = null,
                Notes = "Identify next concrete mitigation steps and owners.",
                LastTouchedAt = NowMinusDays(6)
            }
        };

        var taskDependencies = new List<TaskDependency>
        {
            new()
            {
                Id = SeedIds.TaskDependency1,
                DependentTaskId = SeedIds.Task1,
                BlockerTaskId = SeedIds.Task6
            }
        };

        var teamMemberRisks = new List<TeamMemberRisk>
        {
            new()
            {
                Id = SeedIds.TeamMemberRiskBob,
                TeamMemberId = SeedIds.TeamMemberBob,
                Title = "Sustained Over-Capacity Across Multiple Streams",
                Severity = TeamMemberRiskSeverity.High,
                RiskType = "Workload / Capacity",
                Status = TeamMemberRiskStatus.Mitigating,
                Trend = TeamMemberRiskTrend.Worsening,
                FirstNoticedDate = DateOnlyUtcDaysFromNow(-21),
                ImpactArea = "Delivery & Sustainability",
                Description =
                    "Engineer is currently owning three parallel initiatives across two teams. PR turnaround time has increased and context switching is causing missed follow-ups. Risk of burnout if load remains unchanged.",
                CurrentAction =
                    "Reduced active work-in-progress to one primary feature. Secondary work deferred or reassigned. Weekly check-in added to reassess capacity.",
                LastReviewedAt = NowMinusDays(3),
                LinkedGlobalRiskId = SeedIds.RiskOverCapacity
            },
            new()
            {
                Id = SeedIds.TeamMemberRiskAlice,
                TeamMemberId = SeedIds.TeamMemberAlice,
                Title = "Perf testing blocked by intermittent gateway errors",
                Severity = TeamMemberRiskSeverity.Medium,
                RiskType = "Environment / Reliability",
                Status = TeamMemberRiskStatus.Open,
                Trend = TeamMemberRiskTrend.Stable,
                FirstNoticedDate = DateOnlyUtcDaysFromNow(-14),
                ImpactArea = "Delivery",
                Description =
                    "Intermittent gateway 502s are making performance test runs unreliable, slowing iteration and increasing the chance we ship without confidence in impact.",
                CurrentAction =
                    "Collect trace samples; coordinate with infra for rate limiting/timeouts; run smaller-scale tests locally as a stopgap.",
                LastReviewedAt = NowMinusDays(2),
                LinkedGlobalRiskId = SeedIds.RiskRefactorJustification
            },
            new()
            {
                Id = SeedIds.TeamMemberRiskEvan,
                TeamMemberId = SeedIds.TeamMemberEvan,
                Title = "Sustained blocking due to CI flakiness",
                Severity = TeamMemberRiskSeverity.High,
                RiskType = "Tooling / CI",
                Status = TeamMemberRiskStatus.Mitigating,
                Trend = TeamMemberRiskTrend.Stable,
                FirstNoticedDate = DateOnlyUtcDaysFromNow(-35),
                ImpactArea = "Delivery & Sustainability",
                Description = "CI instability on integration tests is blocking progress and increasing frustration/interrupt load.",
                CurrentAction =
                    "Triage top flaky suites; isolate recent changes; schedule a dedicated stabilization sprint slice; add ownership rotation.",
                LastReviewedAt = NowMinusDays(1),
                LinkedGlobalRiskId = SeedIds.RiskOnboardingDrift
            }
        };

        // Growth (Alice)
        var growthPlans = new List<Growth>
        {
            new()
            {
                Id = SeedIds.GrowthAlice,
                TeamMemberId = SeedIds.TeamMemberAlice,
                FocusAreasMarkdown =
                    "- Practicing earlier communication of technical risks during sprint execution\n" +
                    "- Stepping into design discussions when ambiguity is high\n" +
                    "- Delegating tasks during high workload instead of absorbing work\n"
            }
        };

        var growthGoals = new List<GrowthGoal>
        {
            new()
            {
                Id = SeedIds.GrowthGoalAlice1,
                GrowthId = SeedIds.GrowthAlice,
                Title = "Move toward Technical Lead responsibilities",
                Description = "Increasing architectural ownership and supporting teammates during implementation.",
                Status = GrowthGoalStatus.OnTrack,
                Category = "Leadership",
                Priority = Priority.Medium,
                StartDate = DateOnlyUtcDaysFromNow(-30),
                TargetDate = DateOnlyUtcDaysFromNow(60),
                LastUpdatedAt = NowMinusDays(2),
                ProgressPercent = 55,
                Summary = "Focus on taking clearer ownership of architecture decisions and helping unblock others during execution.",
                SuccessCriteria =
                    "- Lead at least one design discussion end-to-end each sprint\n" +
                    "- Write/ship a short design note before major changes\n" +
                    "- Proactively unblock teammates during implementation (pairing, reviews, clear next steps)"
            },
            new()
            {
                Id = SeedIds.GrowthGoalAlice2,
                GrowthId = SeedIds.GrowthAlice,
                Title = "Improve cross-team communication",
                Description = "Sharing risks and decisions earlier with stakeholders and peers.",
                Status = GrowthGoalStatus.NeedsAttention,
                Category = "Communication",
                Priority = Priority.High,
                StartDate = DateOnlyUtcDaysFromNow(-20),
                TargetDate = DateOnlyUtcDaysFromNow(40),
                LastUpdatedAt = NowMinusDays(6),
                ProgressPercent = 30,
                Summary = "Make risk/decision communication earlier and more consistent to reduce surprises and rework.",
                SuccessCriteria =
                    "- Flag risks within 24 hours of noticing them (in the right channel)\n" +
                    "- Share key decisions with a short context + tradeoffs note"
            }
        };

        static string? EvidenceFromLinks(params string[] links)
        {
            var cleaned = links.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            return cleaned.Count == 0 ? null : string.Join('\n', cleaned);
        }

        var growthGoalActions = new List<GrowthGoalAction>
        {
            // Goal 1
            new()
            {
                Id = SeedIds.GrowthActionAlice1,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Title = "Schedule a weekly 30m design sync for upcoming work",
                DueDate = DateOnlyUtcDaysFromNow(-7),
                State = GrowthGoalActionState.InProgress,
                Priority = Priority.Medium,
                Notes = "Keep it lightweight: agenda + decisions + next steps.",
                Evidence = EvidenceFromLinks("(doc placeholder) Design sync agenda template")
            },
            new()
            {
                Id = SeedIds.GrowthActionAlice2,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Title = "Draft a design note for the next checkout perf iteration",
                DueDate = DateOnlyUtcDaysFromNow(-14),
                State = GrowthGoalActionState.Planned,
                Priority = Priority.Medium,
                Notes = "Aim for 1–2 pages: context, options, tradeoffs, risks.",
                Evidence = EvidenceFromLinks("(doc placeholder) Design note template")
            },
            new()
            {
                Id = SeedIds.GrowthActionAlice4,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Title = "Run a 45m “alignment” session before starting the next cross-cutting change",
                DueDate = DateOnlyUtcDaysFromNow(-3),
                State = GrowthGoalActionState.Complete,
                Priority = Priority.Medium,
                Notes = "Outcome: agreed on scope boundaries + what “done” means.",
                Evidence = EvidenceFromLinks("(notes placeholder) Alignment summary")
            },
            new()
            {
                Id = SeedIds.GrowthActionAlice5,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Title = "Pair-review two PRs this sprint to model “why + tradeoffs” writeups",
                DueDate = DateOnlyUtcDaysFromNow(-1),
                State = GrowthGoalActionState.InProgress,
                Priority = Priority.Low,
                Notes = "Pick one medium change and one higher-risk change; focus on communicating edge cases.",
                Evidence = EvidenceFromLinks("(placeholder) PR-4821", "(placeholder) PR-4840")
            },
            new()
            {
                Id = SeedIds.GrowthActionAlice6,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Title = "Publish a short “decision log” note after each design decision",
                DueDate = DateOnlyUtcDaysFromNow(10),
                State = GrowthGoalActionState.Planned,
                Priority = Priority.Low,
                Notes = "Keep it to: decision + context + alternatives + why.",
                Evidence = null
            },

            // Goal 2
            new()
            {
                Id = SeedIds.GrowthActionAlice3,
                GrowthGoalId = SeedIds.GrowthGoalAlice2,
                Title = "Post a weekly “risks & decisions” update in the cross-team channel",
                DueDate = DateOnlyUtcDaysFromNow(-5),
                State = GrowthGoalActionState.InProgress,
                Priority = Priority.High,
                Notes = "Keep it to 3 bullets: risk, decision, ask.",
                Evidence = EvidenceFromLinks("(placeholder) #cross-team weekly update thread")
            },
            new()
            {
                Id = SeedIds.GrowthActionAlice7,
                GrowthGoalId = SeedIds.GrowthGoalAlice2,
                Title = "Add a “Risks / Watchouts” section to PR descriptions for higher-risk changes",
                DueDate = DateOnlyUtcDaysFromNow(-2),
                State = GrowthGoalActionState.InProgress,
                Priority = Priority.High,
                Notes = "Goal: make downstream impacts visible before review starts.",
                Evidence = EvidenceFromLinks("(placeholder) PR template snippet")
            },
            new()
            {
                Id = SeedIds.GrowthActionAlice8,
                GrowthGoalId = SeedIds.GrowthGoalAlice2,
                Title = "Share a one-paragraph “why now” context before asking for partner work",
                DueDate = DateOnlyUtcDaysFromNow(-8),
                State = GrowthGoalActionState.Complete,
                Priority = Priority.Medium,
                Notes = "Tested twice; got quicker responses when context was clear.",
                Evidence = null
            },
            new()
            {
                Id = SeedIds.GrowthActionAlice9,
                GrowthGoalId = SeedIds.GrowthGoalAlice2,
                Title = "Ask one stakeholder: “Was anything surprising this week?”",
                DueDate = DateOnlyUtcDaysFromNow(7),
                State = GrowthGoalActionState.Planned,
                Priority = Priority.Low,
                Notes = "Capture patterns and adjust weekly update format.",
                Evidence = null
            }
        };

        var growthGoalCheckIns = new List<GrowthGoalCheckIn>
        {
            // Goal 1
            new()
            {
                Id = SeedIds.GrowthCheckInAlice1,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Date = DateOnlyUtcDaysFromNow(-10),
                Signal = GrowthGoalCheckInSignal.Positive,
                Note = "Had a strong architecture discussion and aligned early on tradeoffs; felt smoother than last sprint."
            },
            new()
            {
                Id = SeedIds.GrowthCheckInAlice2,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Date = DateOnlyUtcDaysFromNow(-3),
                Signal = GrowthGoalCheckInSignal.Mixed,
                Note = "Good progress, but I still waited too long to surface a dependency risk to the stakeholders."
            },
            new()
            {
                Id = SeedIds.GrowthCheckInAlice4,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Date = DateOnlyUtcDaysFromNow(-1),
                Signal = GrowthGoalCheckInSignal.Positive,
                Note = "Proactively pulled in two teammates early; pairing helped unblock and improved confidence in the approach."
            },
            new()
            {
                Id = SeedIds.GrowthCheckInAlice5,
                GrowthGoalId = SeedIds.GrowthGoalAlice1,
                Date = DateOnlyUtcDaysFromNow(0),
                Signal = GrowthGoalCheckInSignal.Mixed,
                Note = "I led the decision, but the write-up was too sparse; reviewers asked about edge cases I hadn’t documented."
            },

            // Goal 2
            new()
            {
                Id = SeedIds.GrowthCheckInAlice3,
                GrowthGoalId = SeedIds.GrowthGoalAlice2,
                Date = DateOnlyUtcDaysFromNow(-7),
                Signal = GrowthGoalCheckInSignal.Concern,
                Note = "I raised a major risk late; it caused replanning and made partners feel surprised."
            },
            new()
            {
                Id = SeedIds.GrowthCheckInAlice6,
                GrowthGoalId = SeedIds.GrowthGoalAlice2,
                Date = DateOnlyUtcDaysFromNow(-4),
                Signal = GrowthGoalCheckInSignal.Mixed,
                Note = "I posted the risk earlier this time, but the message was too long; I need a tighter format."
            },
            new()
            {
                Id = SeedIds.GrowthCheckInAlice7,
                GrowthGoalId = SeedIds.GrowthGoalAlice2,
                Date = DateOnlyUtcDaysFromNow(-2),
                Signal = GrowthGoalCheckInSignal.Positive,
                Note = "A short “decision + tradeoffs” note reduced back-and-forth and avoided rework."
            }
        };

        var growthFeedbackThemes = new List<GrowthFeedbackTheme>
        {
            new()
            {
                Id = SeedIds.GrowthThemeAlice1,
                GrowthId = SeedIds.GrowthAlice,
                Title = "Strong execution, emerging leadership",
                Description = "Consistently delivers quality work and is beginning to unblock others.",
                ObservedSinceLabel = "Observed since Jan"
            },
            new()
            {
                Id = SeedIds.GrowthThemeAlice2,
                GrowthId = SeedIds.GrowthAlice,
                Title = "Late risk communication",
                Description = "Risks are usually handled well, but surfacing them earlier would improve planning.",
                ObservedSinceLabel = "Observed since Feb"
            }
        };

        var growthSkillsInProgress = new List<GrowthSkillInProgress>
        {
            new() { GrowthId = SeedIds.GrowthAlice, SortOrder = 0, Value = "System Design: Intermediate → Advanced" },
            new() { GrowthId = SeedIds.GrowthAlice, SortOrder = 1, Value = "Technical Communication: Developing → Solid" },
            new() { GrowthId = SeedIds.GrowthAlice, SortOrder = 2, Value = "Delegation: Early → Developing" }
        };

        return new DevSeedModel(
            settings,
            productOwners,
            teamMembers,
            teamNotes,
            projects,
            projectTeamMembers,
            projectTags,
            projectLinks,
            risks,
            riskHistory,
            riskTeamMembers,
            tasks,
            taskDependencies,
            teamMemberRisks,
            growthPlans,
            growthGoals,
            growthGoalActions,
            growthGoalCheckIns,
            growthFeedbackThemes,
            growthSkillsInProgress);
    }
}

