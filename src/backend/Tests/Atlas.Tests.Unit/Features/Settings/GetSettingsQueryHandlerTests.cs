using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Settings.GetSettings;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Settings;

public sealed class GetSettingsQueryHandlerTests
{
    private readonly Mock<ISettingsRepository> _settings = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly GetSettingsQueryHandler _handler;

    public GetSettingsQueryHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new GetSettingsQueryHandler(_settings.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenSingletonExists_ReturnsItWithoutCreating()
    {
        var existing = new Domain.Entities.Settings
        {
            Id = Guid.NewGuid(),
            StaleDays = 7,
            DefaultAiManualOnly = false,
            Theme = Theme.Light
        };
        _settings.Setup(s => s.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        Domain.Entities.Settings result = await _handler.Handle(new GetSettingsQuery(), CancellationToken.None);

        result.Should().BeSameAs(existing);
        _settings.Verify(s => s.AddAsync(It.IsAny<Domain.Entities.Settings>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMissing_CreatesDefaultSingleton()
    {
        _settings.SetupSequence(s => s.GetSingletonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Settings?)null)
            .ReturnsAsync((Domain.Entities.Settings?)null);

        Domain.Entities.Settings? captured = null;
        _settings.Setup(s => s.AddAsync(It.IsAny<Domain.Entities.Settings>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Settings, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        Domain.Entities.Settings result = await _handler.Handle(new GetSettingsQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        captured.Should().NotBeNull();
        captured!.StaleDays.Should().Be(10);
        captured.Theme.Should().Be(Theme.Dark);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
