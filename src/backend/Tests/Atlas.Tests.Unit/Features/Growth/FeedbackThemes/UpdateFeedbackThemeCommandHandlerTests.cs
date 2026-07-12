using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.FeedbackThemes.UpdateFeedbackTheme;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.FeedbackThemes;

public sealed class UpdateFeedbackThemeCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateFeedbackThemeCommandHandler _handler;

    public UpdateFeedbackThemeCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateFeedbackThemeCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenThemeExists_Updates()
    {
        var growthId = Guid.NewGuid();
        var themeId = Guid.NewGuid();
        var theme = new GrowthFeedbackTheme { Id = themeId, GrowthId = growthId, Title = "Old", Description = "D" };
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid(), FeedbackThemes = [theme] };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateFeedbackThemeCommand(growthId, themeId, " New ", " Desc ", "  "), CancellationToken.None);

        ok.Should().BeTrue();
        theme.Title.Should().Be("New");
        theme.ObservedSinceLabel.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenThemeMissing_ReturnsFalse()
    {
        var growthId = Guid.NewGuid();
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid() };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateFeedbackThemeCommand(growthId, Guid.NewGuid(), "T", "D", null), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
