using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class GrowthSkillInProgressConfiguration : IEntityTypeConfiguration<GrowthSkillInProgress>
{
    public void Configure(EntityTypeBuilder<GrowthSkillInProgress> builder)
    {
        builder.ToTable("GrowthSkillsInProgress");

        // Clean SQL primary key: one skill value per growth plan.
        builder.HasKey(x => new { x.GrowthId, x.Value });

        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.Value).IsRequired().HasMaxLength(200);

        // Supports stable ordering queries per growth plan.
        // Also prevents duplicate sort positions (ambiguous ordering) within a growth plan.
        builder.HasIndex(x => new { x.GrowthId, x.SortOrder }).IsUnique();

        builder.HasOne(x => x.Growth)
            .WithMany(x => x.SkillsInProgress)
            .HasForeignKey(x => x.GrowthId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

