using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class RiskTeamMemberConfiguration : IEntityTypeConfiguration<RiskTeamMember>
{
    public void Configure(EntityTypeBuilder<RiskTeamMember> builder)
    {
        builder.ToTable("RiskTeamMembers");

        builder.HasKey(x => new { x.RiskId, x.TeamMemberId });

        builder.HasOne(x => x.Risk)
            .WithMany(x => x.LinkedTeamMembers)
            .HasForeignKey(x => x.RiskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TeamMember)
            .WithMany(x => x.LinkedRisks)
            .HasForeignKey(x => x.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

