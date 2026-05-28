using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AiSessionConfiguration : IEntityTypeConfiguration<AiSession>
{
    public void Configure(EntityTypeBuilder<AiSession> builder)
    {
        builder.ToTable("AiSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Prompt).IsRequired();
        builder.Property(x => x.View).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ConversationId).IsRequired();
        builder.Property(x => x.TurnIndex).IsRequired();

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Turns)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Events)
            .WithOne(x => x.AiSession)
            .HasForeignKey(x => x.AiSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => new { x.ConversationId, x.TurnIndex }).IsUnique();
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.RiskId);
        builder.HasIndex(x => x.TeamMemberId);
    }
}
