using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("TaskDependencies");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.DependentTaskId, x.BlockerTaskId }).IsUnique();

        builder.HasOne(x => x.DependentTask)
            .WithMany(x => x.BlockedBy)
            .HasForeignKey(x => x.DependentTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BlockerTask)
            .WithMany()
            .HasForeignKey(x => x.BlockerTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

