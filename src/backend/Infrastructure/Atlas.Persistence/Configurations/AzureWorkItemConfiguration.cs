using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AzureWorkItemConfiguration : IEntityTypeConfiguration<AzureWorkItem>
{
    public void Configure(EntityTypeBuilder<AzureWorkItem> builder)
    {
        builder.ToTable("AzureWorkItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkItemId).IsRequired();
        builder.Property(x => x.Rev).IsRequired();
        builder.Property(x => x.ChangedDateUtc).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.State).IsRequired();
        builder.Property(x => x.WorkItemType).IsRequired();
        builder.Property(x => x.AreaPath).IsRequired();
        builder.Property(x => x.IterationPath).IsRequired();
        builder.Property(x => x.AssignedToUniqueName);
        builder.Property(x => x.Url).IsRequired();

        builder.HasOne(x => x.AzureConnection)
            .WithMany()
            .HasForeignKey(x => x.AzureConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.AzureConnectionId, x.WorkItemId }).IsUnique();
    }
}
