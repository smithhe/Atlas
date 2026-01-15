using Atlas.Domain.Entities;

namespace Atlas.Persistence;

public sealed class AtlasDbContext : DbContext
{
    public AtlasDbContext(DbContextOptions<AtlasDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
    public DbSet<ProjectLinkItem> ProjectLinks => Set<ProjectLinkItem>();
    public DbSet<ProjectTeamMember> ProjectTeamMembers => Set<ProjectTeamMember>();

    public DbSet<Risk> Risks => Set<Risk>();
    public DbSet<RiskHistoryEntry> RiskHistoryEntries => Set<RiskHistoryEntry>();
    public DbSet<RiskTeamMember> RiskTeamMembers => Set<RiskTeamMember>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamNote> TeamNotes => Set<TeamNote>();
    public DbSet<TeamMemberRisk> TeamMemberRisks => Set<TeamMemberRisk>();

    public DbSet<AzureConnection> AzureConnections => Set<AzureConnection>();
    public DbSet<AzureSyncState> AzureSyncStates => Set<AzureSyncState>();
    public DbSet<AzureWorkItem> AzureWorkItems => Set<AzureWorkItem>();
    public DbSet<AzureUser> AzureUsers => Set<AzureUser>();
    public DbSet<AzureUserMapping> AzureUserMappings => Set<AzureUserMapping>();
    public DbSet<AzureWorkItemLink> AzureWorkItemLinks => Set<AzureWorkItemLink>();

    public DbSet<Growth> GrowthPlans => Set<Growth>();
    public DbSet<GrowthGoal> GrowthGoals => Set<GrowthGoal>();
    public DbSet<GrowthGoalAction> GrowthGoalActions => Set<GrowthGoalAction>();
    public DbSet<GrowthGoalCheckIn> GrowthGoalCheckIns => Set<GrowthGoalCheckIn>();
    public DbSet<GrowthFeedbackTheme> GrowthFeedbackThemes => Set<GrowthFeedbackTheme>();
    public DbSet<GrowthSkillInProgress> GrowthSkillsInProgress => Set<GrowthSkillInProgress>();

    public DbSet<ProductOwner> ProductOwners => Set<ProductOwner>();
    public DbSet<Settings> Settings => Set<Settings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtlasDbContext).Assembly);
    }
}

