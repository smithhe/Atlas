using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class ProjectLinkItemConfiguration : IEntityTypeConfiguration<ProjectLinkItem>
{
    public void Configure(EntityTypeBuilder<ProjectLinkItem> builder)
    {
        builder.ToTable("ProjectLinks");

        // Clean SQL primary key: one link (label+url) per project.
        builder.HasKey(x => new { x.ProjectId, x.Label, x.Url });

        builder.Property(x => x.Label).IsRequired();
        builder.Property(x => x.Url).IsRequired();

        builder.HasOne(x => x.Project)
            .WithMany(x => x.Links)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

