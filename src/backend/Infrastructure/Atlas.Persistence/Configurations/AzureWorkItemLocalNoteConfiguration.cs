using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AzureWorkItemLocalNoteConfiguration : IEntityTypeConfiguration<AzureWorkItemLocalNote>
{
    public void Configure(EntityTypeBuilder<AzureWorkItemLocalNote> builder)
    {
        builder.ToTable("AzureWorkItemLocalNotes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text).IsRequired();

        builder.HasIndex(x => new { x.TeamMemberId, x.WorkItemId, x.CreatedAt });

        builder.HasOne(x => x.TeamMember)
            .WithMany(x => x.AzureWorkItemLocalNotes)
            .HasForeignKey(x => x.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
