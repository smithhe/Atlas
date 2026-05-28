using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("AiConversations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.View).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasMany(x => x.Turns)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UpdatedAtUtc);
        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.RiskId);
        builder.HasIndex(x => x.TeamMemberId);
    }
}
