using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class GrowthGoalActionConfiguration : IEntityTypeConfiguration<GrowthGoalAction>
{
    public void Configure(EntityTypeBuilder<GrowthGoalAction> builder)
    {
        builder.ToTable("GrowthGoalActions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GrowthGoalId).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.Notes);
        builder.Property(x => x.Evidence);

        builder.HasIndex(x => x.GrowthGoalId);
    }
}

