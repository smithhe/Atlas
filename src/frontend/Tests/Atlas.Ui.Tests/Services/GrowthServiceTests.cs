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
            var persistFailed = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            service.PersistFailed += message => persistFailed.TrySetResult(message);

            try
            {
                string? validation = service.UpdateGoal(memberId, goalId, goal =>
                {
                    goal.Summary = "updated";
                    return goal;
                });

                validation.Should().BeNull();
                string failedMessage = await persistFailed.Task.WaitAsync(TimeSpan.FromSeconds(2));
                failedMessage.Should().Be("Unable to save goal changes. Please try again.");
            }
            finally
            {
                service.Dispose();
            }
        }

        [Fact]
        public async Task UpdateGoal_WhenPersistFails_RaisesPersistFailedBeforeRetryCompletes()
        {
            var memberId = Guid.NewGuid();
            var growthId = Guid.NewGuid();
            var goalId = Guid.NewGuid();
            var retryHang = new TaskCompletionSource<AtlasApiDTOsGrowthGrowthDto>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var persistFailed = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);

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
                .Returns(retryHang.Task);

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
            service.PersistFailed += message => persistFailed.TrySetResult(message);

            try
            {
                string? validation = service.UpdateGoal(memberId, goalId, goal =>
                {
                    goal.Summary = "updated";
                    return goal;
                });

                validation.Should().BeNull();
                string failedMessage = await persistFailed.Task.WaitAsync(TimeSpan.FromSeconds(2));
                failedMessage.Should().Be("Unable to save goal changes. Please try again.");
                retryHang.Task.IsCompleted.Should().BeFalse();
            }
            finally
            {
                service.Dispose();
                retryHang.TrySetCanceled();
            }
        }
    }
}
