using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Ai.Context;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class TeamPromptContextBuilderTests
{
    [Fact]
    public async Task BuildContextAsync_IncludesMemberSnapshotAndSelectedDetails()
    {
        var memberId = Guid.NewGuid();
        var member = new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Engineer",
            StatusDot = StatusDot.Green,
            CurrentFocus = "Ship Phase 4",
            Signals = new TeamMemberSignals
            {
                Load = LoadSignal.Heavy,
                Delivery = DeliverySignal.OnTrack,
                SupportNeeded = SupportNeededSignal.Low
            },
            Notes =
            [
                new TeamNote
                {
                    Id = Guid.NewGuid(),
                    TeamMemberId = memberId,
                    Type = NoteType.Quick,
                    Title = "1:1",
                    Text = "Discuss blockers",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var team = new Mock<ITeamMemberRepository>();
        team.Setup(t => t.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TeamMember> { member });
        team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        var growth = new Mock<IGrowthRepository>();
        growth.Setup(g => g.GetByTeamMemberIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Entities.Growth
            {
                Id = Guid.NewGuid(),
                TeamMemberId = memberId,
                Goals = [new GrowthGoal { Id = Guid.NewGuid(), Title = "Mentorship", Status = GrowthGoalStatus.OnTrack }],
                SkillsInProgress = [new GrowthSkillInProgress { Value = "Systems design", SortOrder = 0 }]
            });

        var builder = new TeamPromptContextBuilder(team.Object, growth.Object);
        var context = await builder.BuildContextAsync(
            new AiSessionStartRequest(Guid.NewGuid(), 0, "help", AiViewScope.Team, null, null, null, null, memberId),
            CancellationToken.None);

        context.Should().StartWith("Team context:");
        context.Should().Contain("Ada");
        context.Should().Contain("Ship Phase 4");
        context.Should().Contain("1:1");
        context.Should().Contain("Mentorship");
        context.Should().Contain("Team member snapshot:");
    }
}
