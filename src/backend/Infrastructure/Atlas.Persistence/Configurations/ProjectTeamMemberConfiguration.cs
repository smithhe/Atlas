using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class ProjectTeamMemberConfiguration : IEntityTypeConfiguration<ProjectTeamMember>
{
    public void Configure(EntityTypeBuilder<ProjectTeamMember> builder)
    {
        builder.ToTable("ProjectTeamMembers");

        builder.HasKey(x => new { x.ProjectId, x.TeamMemberId });

        builder.HasOne(x => x.Project)
            .WithMany(x => x.TeamMembers)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TeamMember)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

