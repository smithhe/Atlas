using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class GrowthSkillInProgressConfiguration : IEntityTypeConfiguration<GrowthSkillInProgress>
{
    public void Configure(EntityTypeBuilder<GrowthSkillInProgress> builder)
    {
        builder.ToTable("GrowthSkillsInProgress");

        // Clean SQL primary key: one skill value per growth plan.
        builder.HasKey(x => new { x.GrowthId, x.Value });

        builder.Property(x => x.Value).IsRequired();

        builder.HasOne(x => x.Growth)
            .WithMany(x => x.SkillsInProgress)
            .HasForeignKey(x => x.GrowthId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

