using Atlas.Application.Features.AzureDevOps.ProductOwners;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class ImportAzureProductOwnersCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTeamMemberMappingExists_Skips()
    {
        var teamMappings = new FakeAzureUserMappingRepository();
        teamMappings.Mappings.Add(new AzureUserMapping
        {
            Id = Guid.NewGuid(),
            AzureUniqueName = "ada@example.com",
            TeamMemberId = Guid.NewGuid(),
            LinkedAtUtc = DateTimeOffset.UtcNow
        });

        var productOwners = new FakeProductOwnerRepository();
        ImportAzureProductOwnersCommandHandler handler = CreateHandler(teamMappings: teamMappings, productOwners: productOwners);

        ImportAzureProductOwnersResult result = await handler.Handle(
            new ImportAzureProductOwnersCommand([new AzureProductOwnerSelection("Ada", "ada@example.com", null)]),
            CancellationToken.None);

        Assert.Equal(0, result.ProductOwnersCreated);
        Assert.Equal(0, result.MappingsCreated);
        Assert.Empty(productOwners.Owners);
    }

    [Fact]
    public async Task Handle_NewUser_CreatesProductOwnerAndMapping()
    {
        var azureUsers = new FakeAzureUserRepository();
        var productOwners = new FakeProductOwnerRepository();
        var mappings = new FakeAzureProductOwnerMappingRepository();
        ImportAzureProductOwnersCommandHandler handler = CreateHandler(
            azureUsers: azureUsers,
            productOwnerMappings: mappings,
            productOwners: productOwners);

        ImportAzureProductOwnersResult result = await handler.Handle(
            new ImportAzureProductOwnersCommand([new AzureProductOwnerSelection("Ada Lovelace", "ada@example.com", "d1")]),
            CancellationToken.None);

        Assert.Equal(1, result.UsersAdded);
        Assert.Equal(1, result.ProductOwnersCreated);
        Assert.Equal(1, result.MappingsCreated);
        Assert.Equal("Ada Lovelace", productOwners.Owners[0].Name);
        Assert.Equal(productOwners.Owners[0].Id, mappings.Mappings[0].ProductOwnerId);
    }

    [Fact]
    public async Task Handle_DuplicateDisplayName_ReusesProductOwner()
    {
        var productOwners = new FakeProductOwnerRepository();
        productOwners.Seed(new ProductOwner { Id = Guid.NewGuid(), Name = "Ada Lovelace" });
        var mappings = new FakeAzureProductOwnerMappingRepository();

        ImportAzureProductOwnersCommandHandler handler = CreateHandler(
            productOwnerMappings: mappings,
            productOwners: productOwners);

        ImportAzureProductOwnersResult result = await handler.Handle(
            new ImportAzureProductOwnersCommand([new AzureProductOwnerSelection("Ada Lovelace", "ada2@example.com", null)]),
            CancellationToken.None);

        Assert.Equal(0, result.ProductOwnersCreated);
        Assert.Equal(1, result.MappingsCreated);
        Assert.Single(productOwners.Owners);
        Assert.Equal(productOwners.Owners[0].Id, mappings.Mappings[0].ProductOwnerId);
    }

    [Fact]
    public async Task Handle_ExistingProductOwnerMapping_SkipsSecondMapping()
    {
        var mappings = new FakeAzureProductOwnerMappingRepository();
        mappings.Mappings.Add(new AzureProductOwnerMapping
        {
            Id = Guid.NewGuid(),
            AzureUniqueName = "ada@example.com",
            ProductOwnerId = Guid.NewGuid(),
            LinkedAtUtc = DateTimeOffset.UtcNow
        });

        var azureUsers = new FakeAzureUserRepository();
        azureUsers.Users.Add(new AzureUser
        {
            Id = Guid.NewGuid(),
            DisplayName = "Ada",
            UniqueName = "ada@example.com",
            IsActive = true
        });

        ImportAzureProductOwnersCommandHandler handler = CreateHandler(
            azureUsers: azureUsers,
            productOwnerMappings: mappings);

        ImportAzureProductOwnersResult result = await handler.Handle(
            new ImportAzureProductOwnersCommand([new AzureProductOwnerSelection("Ada", "ada@example.com", null)]),
            CancellationToken.None);

        Assert.Equal(1, result.UsersUpdated);
        Assert.Equal(0, result.MappingsCreated);
        Assert.Single(mappings.Mappings);
    }

    private static ImportAzureProductOwnersCommandHandler CreateHandler(
        FakeAzureUserRepository? azureUsers = null,
        FakeAzureUserMappingRepository? teamMappings = null,
        FakeAzureProductOwnerMappingRepository? productOwnerMappings = null,
        FakeProductOwnerRepository? productOwners = null) =>
        new(
            azureUsers ?? new FakeAzureUserRepository(),
            teamMappings ?? new FakeAzureUserMappingRepository(),
            productOwnerMappings ?? new FakeAzureProductOwnerMappingRepository(),
            productOwners ?? new FakeProductOwnerRepository(),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider());
}
