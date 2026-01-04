using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class SettingsConfiguration : IEntityTypeConfiguration<Settings>
{
    public void Configure(EntityTypeBuilder<Settings> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StaleDays).IsRequired();
        builder.Property(x => x.DefaultAiManualOnly).IsRequired();
        builder.Property(x => x.Theme).IsRequired();
        builder.Property(x => x.AzureDevOpsBaseUrl);
    }
}

