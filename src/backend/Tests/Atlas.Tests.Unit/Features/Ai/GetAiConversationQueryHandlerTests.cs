using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Ai.GetConversation;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class GetAiConversationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsConversationWithTurns()
    {
        var repo = new Mock<IAiConversationRepository>();
        var id = Guid.NewGuid();
        var conversation = new AiConversation { Id = id, Title = "T", View = "Dashboard", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        repo.Setup(r => r.GetByIdWithTurnsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(conversation);

        var handler = new GetAiConversationQueryHandler(repo.Object);
        (await handler.Handle(new GetAiConversationQuery(id), CancellationToken.None)).Should().BeSameAs(conversation);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsNull()
    {
        var repo = new Mock<IAiConversationRepository>();
        repo.Setup(r => r.GetByIdWithTurnsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AiConversation?)null);

        var handler = new GetAiConversationQueryHandler(repo.Object);
        (await handler.Handle(new GetAiConversationQuery(Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
    }
}
