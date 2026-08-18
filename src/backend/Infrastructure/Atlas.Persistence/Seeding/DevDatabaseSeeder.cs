using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using TaskStatus = Atlas.Domain.Enums.TaskStatus;

namespace Atlas.Persistence.Seeding;

public static class DevDatabaseSeeder
{
    public static async Task SeedAsync(AtlasDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Projects.AnyAsync(cancellationToken) || await db.TeamMembers.AnyAsync(cancellationToken))
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        var alex = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = "Alex Rivera",
            Role = "Staff Engineer",
            StatusDot = StatusDot.Green,
            CurrentFocus = "Atlas API reliability"
        };
        var jordan = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = "Jordan Lee",
            Role = "Senior Engineer",
            StatusDot = StatusDot.Yellow,
            CurrentFocus = "Dashboard heuristics"
        };
        var sam = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = "Sam Okonkwo",
            Role = "Engineering Manager",
            StatusDot = StatusDot.Green,
            CurrentFocus = "Team capacity planning"
        };

        alex.Notes.Add(new TeamNote
        {
            Id = Guid.NewGuid(),
            TeamMemberId = alex.Id,
            Type = NoteType.Standup,
            Title = "Standup",
            Text = "Wrapped health endpoint work; starting Compose packaging next.",
            CreatedAt = now.AddDays(-1)
        });
        jordan.Notes.Add(new TeamNote
        {
            Id = Guid.NewGuid(),
            TeamMemberId = jordan.Id,
            Type = NoteType.Progress,
            Title = "1:1",
            Text = "Discussed load on dashboard queries; follow up on list DTO consolidation.",
            CreatedAt = now.AddDays(-2)
        });
        sam.Notes.Add(new TeamNote
        {
            Id = Guid.NewGuid(),
            TeamMemberId = sam.Id,
            Type = NoteType.Quick,
            Title = "Quick note",
            Text = "Demo environment should stay empty unless --profile demo is used.",
            CreatedAt = now.AddHours(-6),
            PinnedOrder = 1
        });

        var platform = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Atlas Platform",
            Summary = "Local EM cockpit for tasks, risks, and team pulse.",
            Description = "Core product surface for day-to-day engineering management.",
            Status = ProjectStatus.Active,
            Health = HealthSignal.Green,
            Priority = Priority.High,
            LastUpdatedAt = now
        };
        var delivery = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Delivery Insights",
            Summary = "Heuristics and dashboards for team delivery signals.",
            Status = ProjectStatus.Active,
            Health = HealthSignal.Yellow,
            Priority = Priority.Medium,
            LastUpdatedAt = now.AddHours(-3)
        };

        platform.TeamMembers.Add(new ProjectTeamMember { ProjectId = platform.Id, TeamMemberId = alex.Id });
        platform.TeamMembers.Add(new ProjectTeamMember { ProjectId = platform.Id, TeamMemberId = sam.Id });
        delivery.TeamMembers.Add(new ProjectTeamMember { ProjectId = delivery.Id, TeamMemberId = jordan.Id });
        delivery.TeamMembers.Add(new ProjectTeamMember { ProjectId = delivery.Id, TeamMemberId = sam.Id });

        var schemaRisk = new Risk
        {
            Id = Guid.NewGuid(),
            Title = "Schema drift between EnsureCreated and migrations",
            Status = RiskStatus.Open,
            Severity = SeverityLevel.High,
            Description = "Bare Development uses EnsureCreated while Compose applies EF migrations.",
            Evidence = "Documented in docker.md; keep environments from sharing the same database volume casually.",
            ProjectId = platform.Id,
            LastUpdatedAt = now.AddHours(-2)
        };
        var aiKeyRisk = new Risk
        {
            Id = Guid.NewGuid(),
            Title = "Missing OpenAI key blocks AI panel",
            Status = RiskStatus.Open,
            Severity = SeverityLevel.Medium,
            Description = "AI features require OpenAI__ApiKey; CRUD remains usable without it.",
            Evidence = "Settings/AI panel shows setup guidance when the key is absent.",
            ProjectId = delivery.Id,
            LastUpdatedAt = now.AddHours(-5)
        };

        schemaRisk.LinkedTeamMembers.Add(new RiskTeamMember { RiskId = schemaRisk.Id, TeamMemberId = alex.Id });
        aiKeyRisk.LinkedTeamMembers.Add(new RiskTeamMember { RiskId = aiKeyRisk.Id, TeamMemberId = jordan.Id });

        var tasks = new List<TaskItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Ship Docker Compose packaging",
                Priority = Priority.High,
                Status = TaskStatus.InProgress,
                AssigneeId = alex.Id,
                ProjectId = platform.Id,
                RiskId = schemaRisk.Id,
                EstimatedDurationText = "1d",
                EstimateConfidence = Confidence.High,
                Notes = "API + UI images, healthchecks, demo profile seeder.",
                LastTouchedAt = now.AddMinutes(-30),
                DueDate = DateOnly.FromDateTime(now.UtcDateTime.AddDays(2))
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Document OpenAI setup for Compose",
                Priority = Priority.Medium,
                Status = TaskStatus.NotStarted,
                AssigneeId = jordan.Id,
                ProjectId = delivery.Id,
                RiskId = aiKeyRisk.Id,
                EstimatedDurationText = "2h",
                EstimateConfidence = Confidence.Medium,
                Notes = string.Empty,
                LastTouchedAt = now.AddHours(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Verify path routes behind nginx",
                Priority = Priority.Medium,
                Status = TaskStatus.NotStarted,
                AssigneeId = alex.Id,
                ProjectId = platform.Id,
                EstimatedDurationText = "1h",
                EstimateConfidence = Confidence.High,
                Notes = "UI uses Blazor path routing; deep links like /tasks should work.",
                LastTouchedAt = now.AddHours(-4)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Capacity check for next sprint",
                Priority = Priority.Low,
                Status = TaskStatus.NotStarted,
                AssigneeId = sam.Id,
                ProjectId = delivery.Id,
                EstimatedDurationText = "3h",
                EstimateConfidence = Confidence.Low,
                Notes = string.Empty,
                LastTouchedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Retrospective notes cleanup",
                Priority = Priority.Low,
                Status = TaskStatus.Done,
                AssigneeId = jordan.Id,
                ProjectId = platform.Id,
                EstimatedDurationText = "30m",
                EstimateConfidence = Confidence.High,
                ActualDurationText = "25m",
                Notes = "Cleared stale draft notes before demo seed.",
                LastTouchedAt = now.AddDays(-3)
            }
        };

        db.TeamMembers.AddRange(alex, jordan, sam);
        db.Projects.AddRange(platform, delivery);
        db.Risks.AddRange(schemaRisk, aiKeyRisk);
        db.Tasks.AddRange(tasks);

        await db.SaveChangesAsync(cancellationToken);
    }
}
