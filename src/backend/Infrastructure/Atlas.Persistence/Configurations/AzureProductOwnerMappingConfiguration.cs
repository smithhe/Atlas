using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AzureProductOwnerMappingConfiguration : IEntityTypeConfiguration<AzureProductOwnerMapping>
{
    public void Configure(EntityTypeBuilder<AzureProductOwnerMapping> builder)
    {
        builder.ToTable("AzureProductOwnerMappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AzureUniqueName).IsRequired();
        builder.Property(x => x.LinkedAtUtc).IsRequired();

        // Keep this as HasOne/WithMany on purpose. ProductOwner is deduped by name
        // during import, so multiple Azure identities may resolve to the same
        // ProductOwner row while each AzureUniqueName stays unique.
        builder.HasOne(x => x.ProductOwner)
            .WithMany()
            .HasForeignKey(x => x.ProductOwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AzureUniqueName).IsUnique();
    }
}
