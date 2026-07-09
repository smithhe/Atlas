using Atlas.Application.Features.AzureDevOps.Team;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class ImportAzureTeamMembersCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmptyUsers_ReturnsZeros()
    {
        ImportAzureTeamMembersCommandHandler handler = CreateHandler();
        ImportAzureTeamMembersResult result = await handler.Handle(
            new ImportAzureTeamMembersCommand([]),
            CancellationToken.None);

        Assert.Equal(0, result.UsersAdded);
        Assert.Equal(0, result.TeamMembersCreated);
        Assert.Equal(0, result.MappingsCreated);
    }

    [Fact]
    public async Task Handle_NewUser_CreatesAzureUserTeamMemberAndMapping()
    {
        var azureUsers = new FakeAzureUserRepository();
        var mappings = new FakeAzureUserMappingRepository();
        var teamMembers = new FakeTeamMemberRepository();
        ImportAzureTeamMembersCommandHandler handler = CreateHandler(azureUsers, mappings, teamMembers: teamMembers);

        ImportAzureTeamMembersResult result = await handler.Handle(
            new ImportAzureTeamMembersCommand([new AzureTeamMemberSelection("Ada Lovelace", "ada@example.com", "desc-1")]),
            CancellationToken.None);

        Assert.Equal(1, result.UsersAdded);
        Assert.Equal(1, result.TeamMembersCreated);
        Assert.Equal(1, result.MappingsCreated);
        Assert.Single(azureUsers.Users);
        Assert.Single(teamMembers.Members);
        Assert.Equal("Ada Lovelace", teamMembers.Members[0].Name);
        Assert.Equal("ada@example.com", mappings.Mappings[0].AzureUniqueName);
    }

    [Fact]
    public async Task Handle_ExistingMapping_SkipsTeamMemberCreation()
    {
        var azureUsers = new FakeAzureUserRepository();
        azureUsers.Users.Add(new AzureUser
        {
            Id = Guid.NewGuid(),
            DisplayName = "Ada",
            UniqueName = "ada@example.com",
            IsActive = true
        });

        var mappings = new FakeAzureUserMappingRepository();
        mappings.Mappings.Add(new AzureUserMapping
        {
            Id = Guid.NewGuid(),
            AzureUniqueName = "ada@example.com",
            TeamMemberId = Guid.NewGuid(),
            LinkedAtUtc = DateTimeOffset.UtcNow
        });

        var teamMembers = new FakeTeamMemberRepository();
        ImportAzureTeamMembersCommandHandler handler = CreateHandler(azureUsers, mappings, teamMembers: teamMembers);

        ImportAzureTeamMembersResult result = await handler.Handle(
            new ImportAzureTeamMembersCommand([new AzureTeamMemberSelection("Ada Updated", "ada@example.com", null)]),
            CancellationToken.None);

        Assert.Equal(0, result.UsersAdded);
        Assert.Equal(1, result.UsersUpdated);
        Assert.Equal(0, result.TeamMembersCreated);
        Assert.Equal(0, result.MappingsCreated);
        Assert.Empty(teamMembers.Members);
        Assert.Equal("Ada Updated", azureUsers.Users[0].DisplayName);
    }

    [Fact]
    public async Task Handle_WhenUserIsProductOwner_SkipsImport()
    {
        var productOwnerMappings = new FakeAzureProductOwnerMappingRepository();
        productOwnerMappings.Mappings.Add(new AzureProductOwnerMapping
        {
            Id = Guid.NewGuid(),
            AzureUniqueName = "ada@example.com",
            ProductOwnerId = Guid.NewGuid(),
            LinkedAtUtc = DateTimeOffset.UtcNow
        });

        var azureUsers = new FakeAzureUserRepository();
        var teamMembers = new FakeTeamMemberRepository();
        ImportAzureTeamMembersCommandHandler handler = CreateHandler(
            azureUsers,
            productOwnerMappings: productOwnerMappings,
            teamMembers: teamMembers);

        ImportAzureTeamMembersResult result = await handler.Handle(
            new ImportAzureTeamMembersCommand([new AzureTeamMemberSelection("Ada", "ada@example.com", null)]),
            CancellationToken.None);

        Assert.Equal(0, result.UsersAdded);
        Assert.Equal(0, result.TeamMembersCreated);
        Assert.Empty(azureUsers.Users);
        Assert.Empty(teamMembers.Members);
    }

    [Fact]
    public async Task Handle_DuplicateUniqueNames_Dedupes()
    {
        var azureUsers = new FakeAzureUserRepository();
        ImportAzureTeamMembersCommandHandler handler = CreateHandler(azureUsers);

        ImportAzureTeamMembersResult result = await handler.Handle(
            new ImportAzureTeamMembersCommand(
            [
                new AzureTeamMemberSelection("Ada", "ADA@example.com", null),
                new AzureTeamMemberSelection("Ada Duplicate", "ada@example.com", null)
            ]),
            CancellationToken.None);

        Assert.Equal(1, result.UsersAdded);
        Assert.Equal(1, result.TeamMembersCreated);
        Assert.Single(azureUsers.Users);
    }

    private static ImportAzureTeamMembersCommandHandler CreateHandler(
        FakeAzureUserRepository? azureUsers = null,
        FakeAzureUserMappingRepository? mappings = null,
        FakeAzureProductOwnerMappingRepository? productOwnerMappings = null,
        FakeTeamMemberRepository? teamMembers = null) =>
        new(
            azureUsers ?? new FakeAzureUserRepository(),
            mappings ?? new FakeAzureUserMappingRepository(),
            productOwnerMappings ?? new FakeAzureProductOwnerMappingRepository(),
            teamMembers ?? new FakeTeamMemberRepository(),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider());
}
