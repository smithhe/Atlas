using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.Signals.UpdateTeamMemberSignals;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.Signals;

public sealed class UpdateTeamMemberSignalsCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateTeamMemberSignalsCommandHandler _handler;

    public UpdateTeamMemberSignalsCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateTeamMemberSignalsCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExists_UpdatesSignals()
    {
        var member = new TeamMember { Id = Guid.NewGuid(), Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        _team.Setup(t => t.GetByIdAsync(member.Id, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new UpdateTeamMemberSignalsCommand(member.Id, LoadSignal.Heavy, DeliverySignal.AtRisk, SupportNeededSignal.High), CancellationToken.None);

        ok.Should().BeTrue();
        member.Signals.Load.Should().Be(LoadSignal.Heavy);
        member.Signals.Delivery.Should().Be(DeliverySignal.AtRisk);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _team.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        bool ok = await _handler.Handle(new UpdateTeamMemberSignalsCommand(Guid.NewGuid(), LoadSignal.Normal, DeliverySignal.OnTrack, SupportNeededSignal.Low), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
