using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AzureUserConfiguration : IEntityTypeConfiguration<AzureUser>
{
    public void Configure(EntityTypeBuilder<AzureUser> builder)
    {
        builder.ToTable("AzureUsers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisplayName).IsRequired();
        builder.Property(x => x.UniqueName).IsRequired();
        builder.Property(x => x.Descriptor);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.UniqueName).IsUnique();
    }
}
