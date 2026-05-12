using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AiSessionEventConfiguration : IEntityTypeConfiguration<AiSessionEvent>
{
    public void Configure(EntityTypeBuilder<AiSessionEvent> builder)
    {
        builder.ToTable("AiSessionEvents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sequence).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();

        builder.HasIndex(x => x.AiSessionId);
        builder.HasIndex(x => new { x.AiSessionId, x.Sequence }).IsUnique();
        builder.HasIndex(x => new { x.AiSessionId, x.OccurredAtUtc });
    }
}
