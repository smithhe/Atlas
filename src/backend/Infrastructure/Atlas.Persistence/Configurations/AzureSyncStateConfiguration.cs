using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AzureSyncStateConfiguration : IEntityTypeConfiguration<AzureSyncState>
{
    public void Configure(EntityTypeBuilder<AzureSyncState> builder)
    {
        builder.ToTable("AzureSyncStates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LastRunStatus).IsRequired();
        builder.Property(x => x.LastError);

        builder.HasOne(x => x.AzureConnection)
            .WithMany()
            .HasForeignKey(x => x.AzureConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AzureConnectionId).IsUnique();
    }
}
