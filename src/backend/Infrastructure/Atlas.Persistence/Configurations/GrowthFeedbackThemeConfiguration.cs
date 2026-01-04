using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class GrowthFeedbackThemeConfiguration : IEntityTypeConfiguration<GrowthFeedbackTheme>
{
    public void Configure(EntityTypeBuilder<GrowthFeedbackTheme> builder)
    {
        builder.ToTable("GrowthFeedbackThemes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GrowthId).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Description).IsRequired();

        builder.HasIndex(x => x.GrowthId);
    }
}

