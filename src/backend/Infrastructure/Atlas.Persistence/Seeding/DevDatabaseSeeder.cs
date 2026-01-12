using Microsoft.EntityFrameworkCore;

namespace Atlas.Persistence.Seeding;

public static class DevDatabaseSeeder
{
    public static async Task SeedAsync(AtlasDbContext db, CancellationToken cancellationToken = default)
    {
        // Idempotency: if any core table already has data, assume the DB is initialized.
        if (await db.TeamMembers.AnyAsync(cancellationToken) ||
            await db.Projects.AnyAsync(cancellationToken) ||
            await db.Tasks.AnyAsync(cancellationToken))
        {
            return;
        }

        var seed = DevSeedData.Build(DateTimeOffset.UtcNow);

        // Settings (singleton)
        await db.Settings.AddAsync(seed.Settings, cancellationToken);

        // Team + notes
        await db.TeamMembers.AddRangeAsync(seed.TeamMembers, cancellationToken);
        await db.TeamNotes.AddRangeAsync(seed.TeamNotes, cancellationToken);

        // Product owners
        await db.ProductOwners.AddRangeAsync(seed.ProductOwners, cancellationToken);

        // Projects + aux rows
        await db.Projects.AddRangeAsync(seed.Projects, cancellationToken);
        await db.ProjectTeamMembers.AddRangeAsync(seed.ProjectTeamMembers, cancellationToken);
        await db.ProjectTags.AddRangeAsync(seed.ProjectTags, cancellationToken);
        await db.ProjectLinks.AddRangeAsync(seed.ProjectLinks, cancellationToken);

        // Risks + links
        await db.Risks.AddRangeAsync(seed.Risks, cancellationToken);
        await db.RiskHistoryEntries.AddRangeAsync(seed.RiskHistory, cancellationToken);
        await db.RiskTeamMembers.AddRangeAsync(seed.RiskTeamMembers, cancellationToken);

        // Tasks + dependencies
        await db.Tasks.AddRangeAsync(seed.Tasks, cancellationToken);
        await db.TaskDependencies.AddRangeAsync(seed.TaskDependencies, cancellationToken);

        // Team member risks
        await db.TeamMemberRisks.AddRangeAsync(seed.TeamMemberRisks, cancellationToken);

        // Growth
        await db.GrowthPlans.AddRangeAsync(seed.GrowthPlans, cancellationToken);
        await db.GrowthGoals.AddRangeAsync(seed.GrowthGoals, cancellationToken);
        await db.GrowthGoalActions.AddRangeAsync(seed.GrowthGoalActions, cancellationToken);
        await db.GrowthGoalCheckIns.AddRangeAsync(seed.GrowthGoalCheckIns, cancellationToken);
        await db.GrowthFeedbackThemes.AddRangeAsync(seed.GrowthFeedbackThemes, cancellationToken);
        await db.GrowthSkillsInProgress.AddRangeAsync(seed.GrowthSkillsInProgress, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }
}

