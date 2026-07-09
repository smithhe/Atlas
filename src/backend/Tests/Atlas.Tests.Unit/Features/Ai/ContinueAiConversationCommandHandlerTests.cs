using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Features.Ai.ContinueConversation;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class ContinueAiConversationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTurnSessionIdFromService()
    {
        var service = new Mock<IAiConversationService>();
        Guid conversationId = Guid.NewGuid();
        Guid turnId = Guid.NewGuid();
        service.Setup(s => s.ContinueConversationAsync(conversationId, "Follow up", It.IsAny<CancellationToken>()))
            .ReturnsAsync(turnId);

        var handler = new ContinueAiConversationCommandHandler(service.Object);
        ContinueAiConversationResult result = await handler.Handle(
            new ContinueAiConversationCommand(conversationId, "Follow up"), CancellationToken.None);

        result.TurnSessionId.Should().Be(turnId);
    }
}
