using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class TeamNoteConfiguration : IEntityTypeConfiguration<TeamNote>
{
    public void Configure(EntityTypeBuilder<TeamNote> builder)
    {
        builder.ToTable("TeamNotes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TeamMemberId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.LastModifiedAt);
        builder.Property(x => x.Title);
        builder.Property(x => x.Text).IsRequired();
        builder.Property(x => x.PinnedOrder);

        builder.HasIndex(x => new { x.TeamMemberId, x.PinnedOrder });
    }
}

