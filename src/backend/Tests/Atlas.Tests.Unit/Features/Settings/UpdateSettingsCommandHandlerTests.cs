using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Settings.UpdateSettings;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Settings;

public sealed class UpdateSettingsCommandHandlerTests
{
    private readonly Mock<ISettingsRepository> _settings = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateSettingsCommandHandler _handler;

    public UpdateSettingsCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateSettingsCommandHandler(_settings.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExisting_UpdatesFields()
    {
        var existing = new Domain.Entities.Settings
        {
            Id = Guid.NewGuid(),
            StaleDays = 10,
            DefaultAiManualOnly = true,
            Theme = Theme.Dark
        };
        _settings.Setup(s => s.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        bool ok = await _handler.Handle(new UpdateSettingsCommand(5, false, Theme.Light, " https://dev.azure.com "), CancellationToken.None);

        ok.Should().BeTrue();
        existing.StaleDays.Should().Be(5);
        existing.DefaultAiManualOnly.Should().BeFalse();
        existing.Theme.Should().Be(Theme.Light);
        existing.AzureDevOpsBaseUrl.Should().Be("https://dev.azure.com");
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMissing_CreatesThenUpdates()
    {
        _settings.Setup(s => s.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Settings?)null);
        Domain.Entities.Settings? captured = null;
        _settings.Setup(s => s.AddAsync(It.IsAny<Domain.Entities.Settings>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Settings, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        bool ok = await _handler.Handle(new UpdateSettingsCommand(3, true, Theme.Dark, "  "), CancellationToken.None);

        ok.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.StaleDays.Should().Be(3);
        captured.AzureDevOpsBaseUrl.Should().BeNull();
    }
}
