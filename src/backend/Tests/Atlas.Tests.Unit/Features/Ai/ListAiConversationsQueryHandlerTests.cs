using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Ai.ListConversations;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class ListAiConversationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_PassesTakeToRepository()
    {
        var repo = new Mock<IAiConversationRepository>();
        IReadOnlyList<AiConversation> expected = [];
        repo.Setup(r => r.ListRecentAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new ListAiConversationsQueryHandler(repo.Object);
        (await handler.Handle(new ListAiConversationsQuery(10), CancellationToken.None)).Should().BeSameAs(expected);
        repo.Verify(r => r.ListRecentAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
