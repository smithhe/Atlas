using Atlas.Domain.Entities;

namespace Atlas.Persistence.Configurations;

public sealed class ProductOwnerConfiguration : IEntityTypeConfiguration<ProductOwner>
{
    public void Configure(EntityTypeBuilder<ProductOwner> builder)
    {
        builder.ToTable("ProductOwners");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired();
    }
}

