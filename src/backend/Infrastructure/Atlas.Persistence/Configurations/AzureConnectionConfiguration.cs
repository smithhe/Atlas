using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AzureConnectionConfiguration : IEntityTypeConfiguration<AzureConnection>
{
    public void Configure(EntityTypeBuilder<AzureConnection> builder)
    {
        builder.ToTable("AzureConnections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Organization).IsRequired();
        builder.Property(x => x.Project).IsRequired();
        builder.Property(x => x.AreaPath).IsRequired();
        builder.Property(x => x.TeamName);
        builder.Property(x => x.IsEnabled).IsRequired();
    }
}
