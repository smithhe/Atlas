using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class GrowthConfiguration : IEntityTypeConfiguration<Growth>
{
    public void Configure(EntityTypeBuilder<Growth> builder)
    {
        builder.ToTable("GrowthPlans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TeamMemberId).IsRequired();
        builder.Property(x => x.FocusAreasMarkdown).IsRequired();

        // One growth plan per team member (enforced at DB level).
        builder.HasIndex(x => x.TeamMemberId).IsUnique();

        builder.HasMany(x => x.Goals)
            .WithOne()
            .HasForeignKey(x => x.GrowthId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FeedbackThemes)
            .WithOne()
            .HasForeignKey(x => x.GrowthId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SkillsInProgress)
            .WithOne(x => x.Growth)
            .HasForeignKey(x => x.GrowthId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

