using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class GrowthGoalCheckInConfiguration : IEntityTypeConfiguration<GrowthGoalCheckIn>
{
    public void Configure(EntityTypeBuilder<GrowthGoalCheckIn> builder)
    {
        builder.ToTable("GrowthGoalCheckIns");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GrowthGoalId).IsRequired();
        builder.Property(x => x.Date).HasColumnType("date");
        builder.Property(x => x.Note).IsRequired();

        builder.HasIndex(x => new { x.GrowthGoalId, x.Date });
    }
}

