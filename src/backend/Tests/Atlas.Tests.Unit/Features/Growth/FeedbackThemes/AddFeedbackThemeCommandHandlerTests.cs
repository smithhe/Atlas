using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.FeedbackThemes.AddFeedbackTheme;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.FeedbackThemes;

public sealed class AddFeedbackThemeCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly AddFeedbackThemeCommandHandler _handler;

    public AddFeedbackThemeCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new AddFeedbackThemeCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenPlanExists_AddsTheme()
    {
        var plan = new Domain.Entities.Growth { Id = Guid.NewGuid(), TeamMemberId = Guid.NewGuid() };
        _growth.Setup(g => g.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        GrowthFeedbackTheme? captured = null;
        _growth.Setup(g => g.AddFeedbackThemeAsync(It.IsAny<GrowthFeedbackTheme>(), It.IsAny<CancellationToken>()))
            .Callback<GrowthFeedbackTheme, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(new AddFeedbackThemeCommand(plan.Id, " Title ", " Desc ", " Q1 "), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured!.Title.Should().Be("Title");
        captured.ObservedSinceLabel.Should().Be("Q1");
    }

    [Fact]
    public async Task Handle_WhenPlanMissing_ReturnsEmptyGuid()
    {
        _growth.Setup(g => g.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        Guid id = await _handler.Handle(new AddFeedbackThemeCommand(Guid.NewGuid(), "T", "D", null), CancellationToken.None);

        id.Should().Be(Guid.Empty);
    }
}
