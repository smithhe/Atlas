using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class TeamMemberRiskConfiguration : IEntityTypeConfiguration<TeamMemberRisk>
{
    public void Configure(EntityTypeBuilder<TeamMemberRisk> builder)
    {
        builder.ToTable("TeamMemberRisks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TeamMemberId).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.RiskType).IsRequired();
        builder.Property(x => x.ImpactArea).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.CurrentAction).IsRequired();

        builder.Property(x => x.FirstNoticedDate).HasColumnType("date");

        builder.HasOne(x => x.TeamMember)
            .WithMany(x => x.Risks)
            .HasForeignKey(x => x.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LinkedGlobalRisk)
            .WithMany()
            .HasForeignKey(x => x.LinkedGlobalRiskId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

