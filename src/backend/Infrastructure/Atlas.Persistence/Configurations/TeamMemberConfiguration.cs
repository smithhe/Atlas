using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Role);
        builder.Property(x => x.CurrentFocus).IsRequired();

        builder.OwnsOne(x => x.Profile, owned =>
        {
            owned.Property(p => p.TimeZone);
            owned.Property(p => p.TypicalHours);
        });

        builder.OwnsOne(x => x.Signals, owned =>
        {
            owned.Property(p => p.Load);
            owned.Property(p => p.Delivery);
            owned.Property(p => p.SupportNeeded);
        });

        builder.HasMany(x => x.Notes)
            .WithOne()
            .HasForeignKey(x => x.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Risks)
            .WithOne(x => x.TeamMember)
            .HasForeignKey(x => x.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

