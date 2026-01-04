using Atlas.Domain.Entities;
using Atlas.Domain.ValueObjects;

namespace Atlas.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Summary).IsRequired();
        builder.Property(x => x.Description);

        builder.Property(x => x.TargetDate).HasColumnType("date");

        builder.Property(x => x.LastUpdatedAt);

        builder.HasOne(x => x.ProductOwner)
            .WithMany()
            .HasForeignKey(x => x.ProductOwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.OwnsOne(x => x.LatestCheckIn, owned =>
        {
            owned.Property(p => p.Date).HasColumnType("date");
            owned.Property(p => p.Note).IsRequired();
        });
        builder.Navigation(x => x.LatestCheckIn).IsRequired(false);

        builder.HasMany(x => x.Tasks)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Risks)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.TeamMembers)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Tags)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Links)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

