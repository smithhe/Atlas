using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class AzureUserMappingConfiguration : IEntityTypeConfiguration<AzureUserMapping>
{
    public void Configure(EntityTypeBuilder<AzureUserMapping> builder)
    {
        builder.ToTable("AzureUserMappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AzureUniqueName).IsRequired();
        builder.Property(x => x.LinkedAtUtc).IsRequired();

        builder.HasOne(x => x.TeamMember)
            .WithMany()
            .HasForeignKey(x => x.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AzureUniqueName).IsUnique();
    }
}
