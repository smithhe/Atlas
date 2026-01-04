using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class GrowthGoalConfiguration : IEntityTypeConfiguration<GrowthGoal>
{
    public void Configure(EntityTypeBuilder<GrowthGoal> builder)
    {
        builder.ToTable("GrowthGoals");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GrowthId).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Summary).IsRequired();
        builder.Property(x => x.SuccessCriteria).IsRequired();

        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.TargetDate).HasColumnType("date");

        builder.HasMany(x => x.Actions)
            .WithOne()
            .HasForeignKey(x => x.GrowthGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CheckIns)
            .WithOne()
            .HasForeignKey(x => x.GrowthGoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

