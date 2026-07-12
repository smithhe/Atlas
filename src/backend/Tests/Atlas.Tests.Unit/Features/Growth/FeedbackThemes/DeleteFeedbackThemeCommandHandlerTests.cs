using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.FeedbackThemes.DeleteFeedbackTheme;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.FeedbackThemes;

public sealed class DeleteFeedbackThemeCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteFeedbackThemeCommandHandler _handler;

    public DeleteFeedbackThemeCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteFeedbackThemeCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenThemeExists_Removes()
    {
        var growthId = Guid.NewGuid();
        var themeId = Guid.NewGuid();
        var plan = new Domain.Entities.Growth
        {
            Id = growthId,
            TeamMemberId = Guid.NewGuid(),
            FeedbackThemes = [new GrowthFeedbackTheme { Id = themeId, GrowthId = growthId, Title = "T", Description = "D" }]
        };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new DeleteFeedbackThemeCommand(growthId, themeId), CancellationToken.None);

        ok.Should().BeTrue();
        plan.FeedbackThemes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _growth.Setup(g => g.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        bool ok = await _handler.Handle(new DeleteFeedbackThemeCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
