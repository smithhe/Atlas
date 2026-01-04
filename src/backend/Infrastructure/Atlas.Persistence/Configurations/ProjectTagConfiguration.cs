using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class ProjectTagConfiguration : IEntityTypeConfiguration<ProjectTag>
{
    public void Configure(EntityTypeBuilder<ProjectTag> builder)
    {
        builder.ToTable("ProjectTags");

        // Clean SQL primary key: one tag value per project.
        builder.HasKey(x => new { x.ProjectId, x.Value });

        builder.Property(x => x.Value).IsRequired();

        builder.HasOne(x => x.Project)
            .WithMany(x => x.Tags)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

