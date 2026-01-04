using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class RiskHistoryEntryConfiguration : IEntityTypeConfiguration<RiskHistoryEntry>
{
    public void Configure(EntityTypeBuilder<RiskHistoryEntry> builder)
    {
        builder.ToTable("RiskHistoryEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.RiskId);
    }
}

