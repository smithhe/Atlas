using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AzureWorkItemLinkConfiguration : IEntityTypeConfiguration<AzureWorkItemLink>
{
    public void Configure(EntityTypeBuilder<AzureWorkItemLink> builder)
    {
        builder.ToTable("AzureWorkItemLinks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LinkedAtUtc).IsRequired();

        builder.HasOne(x => x.AzureWorkItem)
            .WithMany()
            .HasForeignKey(x => x.AzureWorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TeamMember)
            .WithMany()
            .HasForeignKey(x => x.TeamMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.AzureWorkItemId).IsUnique();
    }
}
