using Atlas.Ui.Api.Generated;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using FluentAssertions;
using Moq;

namespace Atlas.Ui.Tests.Services
{
    public sealed class GrowthServiceTests
    {
        [Fact]
        public async Task UpdateGoal_WhenPersistFails_RaisesPersistFailedWithoutDialogs()
        {
            var memberId = Guid.NewGuid();
            var growthId = Guid.NewGuid();
            var goalId = Guid.NewGuid();

            Mock<IAtlasApiClient> api = new();
            api.Setup(x => x.AtlasApiEndpointsGrowthGoalsUpdateGrowthGoalEndpointAsync(
                    growthId,
                    goalId,
                    It.IsAny<AtlasApiDTOsGrowthGoalsUpdateGrowthGoalRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));
            api.Setup(x => x.AtlasApiEndpointsGrowthGetGrowthByTeamMemberEndpointAsync(
                    memberId,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("reload"));

            AppCacheService cache = AutosaveTestSupport.CreateCache(api.Object);
            cache.UpdateGrowth(new Growth
            {
                Id = growthId,
                MemberId = memberId,
                Goals =
                [
                    new GrowthGoal
                    {
                        Id = goalId,
                        Title = "Ship house style",
                        Description = "Finish ATLAS-19"
                    }
                ]
            });

            GrowthService service = new(api.Object, cache);
            string? failedMessage = null;
            service.PersistFailed += message => failedMessage = message;

            string? validation = service.UpdateGoal(memberId, goalId, goal =>
            {
                goal.Summary = "updated";
                return goal;
            });

            validation.Should().BeNull();
            await Task.Delay(700);

            failedMessage.Should().Be("Unable to save goal changes. Please try again.");
        }
    }
}
