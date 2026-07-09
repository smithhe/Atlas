using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.AzureDevOps.Users;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class ListImportedAzureUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_UnionsMappedNamesAndLoadsUsers()
    {
        var azureUsers = new Mock<IAzureUserRepository>();
        var teamMappings = new Mock<IAzureUserMappingRepository>();
        var poMappings = new Mock<IAzureProductOwnerMappingRepository>();

        teamMappings.Setup(m => m.ListUniqueNamesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(["ada@x", "bob@x"]);
        poMappings.Setup(m => m.ListUniqueNamesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(["BOB@x", "cara@x"]);

        IReadOnlyList<AzureUser> expected = [new AzureUser { Id = Guid.NewGuid(), DisplayName = "Ada", UniqueName = "ada@x" }];
        azureUsers.Setup(u => u.GetByUniqueNamesAsync(
                It.Is<IReadOnlyList<string>>(names => names.Count == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new ListImportedAzureUsersQueryHandler(azureUsers.Object, teamMappings.Object, poMappings.Object);
        (await handler.Handle(new ListImportedAzureUsersQuery(), CancellationToken.None)).Should().BeSameAs(expected);
    }
}
