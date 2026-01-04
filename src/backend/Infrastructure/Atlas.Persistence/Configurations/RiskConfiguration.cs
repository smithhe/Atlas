using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class RiskConfiguration : IEntityTypeConfiguration<Risk>
{
    public void Configure(EntityTypeBuilder<Risk> builder)
    {
        builder.ToTable("Risks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Evidence).IsRequired();

        builder.Property(x => x.LastUpdatedAt).IsRequired();

        builder.HasOne(x => x.Project)
            .WithMany(x => x.Risks)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Tasks)
            .WithOne(x => x.Risk)
            .HasForeignKey(x => x.RiskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.LinkedTeamMembers)
            .WithOne(x => x.Risk)
            .HasForeignKey(x => x.RiskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.History)
            .WithOne()
            .HasForeignKey(x => x.RiskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

