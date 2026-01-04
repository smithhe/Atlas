using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Summary);
        builder.Property(x => x.Notes).IsRequired();
        builder.Property(x => x.LastTouchedAt).IsRequired();

        builder.Property(x => x.DueDate).HasColumnType("date");

        builder.HasOne(x => x.Project)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Risk)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.RiskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.BlockedBy)
            .WithOne(x => x.DependentTask)
            .HasForeignKey(x => x.DependentTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

