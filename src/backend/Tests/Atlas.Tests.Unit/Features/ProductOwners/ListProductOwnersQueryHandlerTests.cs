using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.ProductOwners.ListProductOwners;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.ProductOwners;

public sealed class ListProductOwnersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRepositoryList()
    {
        var repo = new Mock<IProductOwnerRepository>();
        IReadOnlyList<ProductOwner> expected = [new ProductOwner { Id = Guid.NewGuid(), Name = "PO" }];
        repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new ListProductOwnersQueryHandler(repo.Object);
        IReadOnlyList<ProductOwner> result = await handler.Handle(new ListProductOwnersQuery(), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }
}
