using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using FluentAssertions;
using Moq;

namespace Atlas.Ui.Tests.Services;

public sealed class RiskServiceAutosaveTests
{
    [Fact]
    public async Task UpdateAsync_WhenDebouncedDescriptionThenImmediateStatusFromCache_PreservesBothChanges()
    {
        var riskId = Guid.NewGuid();
        Risk initial = new()
        {
            Id = riskId,
            Title = "Risk",
            Status = RiskStatus.Open,
            Severity = "Low",
            Description = "initial",
            Evidence = ""
        };

        List<AtlasApiDTOsRisksUpdateRiskRequest> persisted = [];
        Mock<IAtlasApiClient> api = new();
        api.Setup(x => x.AtlasApiEndpointsRisksUpdateRiskEndpointAsync(
                riskId,
                It.IsAny<AtlasApiDTOsRisksUpdateRiskRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AtlasApiDTOsRisksUpdateRiskRequest, CancellationToken>((_, req, _) => persisted.Add(req))
            .Returns(Task.CompletedTask);

        AppCacheService cache = AutosaveTestSupport.CreateCache(api.Object);
        cache.AddRisk(initial);
        RiskService service = new(api.Object, cache);

        Task debouncedDescription = service.UpdateAsync(
            riskId,
            r => EntityClone.Risk(r, description: "typed description"),
            debounce: true);

        await service.UpdateAsync(
            riskId,
            r => EntityClone.Risk(r, status: RiskStatus.Watching));

        await debouncedDescription;

        Risk cached = cache.TryGetRisk(riskId)!;
        cached.Description.Should().Be("typed description");
        cached.Status.Should().Be(RiskStatus.Watching);
        persisted.Last().Description.Should().Be("typed description");
        persisted.Last().Status.Should().Be(AtlasDomainEnumsRiskStatus.Watching);
        service.GetSaveState(riskId).Should().Be(EntitySaveState.Saved);
    }

    [Fact]
    public async Task SetTeamMembersAsync_WhenRequestFailsAfterConcurrentDebouncedText_RefetchesServerEntityAndPreservesNewerText()
    {
        var riskId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        Risk initial = new()
        {
            Id = riskId,
            Title = "Risk",
            Status = RiskStatus.Open,
            Severity = "Low",
            Description = "initial",
            Evidence = "initial evidence",
            LinkedTeamMemberIds = []
        };

        int refetchCount = 0;
        TaskCompletionSource teamPersistEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseTeamPersist = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<AtlasApiDTOsRisksUpdateRiskRequest> persistedUpdates = [];

        Mock<IAtlasApiClient> api = new();
        api.Setup(x => x.AtlasApiEndpointsRisksSetRiskTeamMembersEndpointAsync(
                riskId,
                It.IsAny<AtlasApiDTOsRisksSetRiskTeamMembersRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                teamPersistEntered.TrySetResult();
                await releaseTeamPersist.Task;
                throw new InvalidOperationException("team members failed");
            });
        api.Setup(x => x.AtlasApiEndpointsRisksGetRiskEndpointAsync(riskId, It.IsAny<CancellationToken>()))
            .Callback(() => refetchCount++)
            .ReturnsAsync(new AtlasApiDTOsRisksRiskDto
            {
                Id = riskId,
                Title = initial.Title,
                Status = AtlasDomainEnumsRiskStatus.Open,
                Severity = AtlasDomainEnumsSeverityLevel.Low,
                Description = initial.Description,
                Evidence = initial.Evidence,
                LinkedTeamMemberIds = []
            });
        api.Setup(x => x.AtlasApiEndpointsRisksUpdateRiskEndpointAsync(
                riskId,
                It.IsAny<AtlasApiDTOsRisksUpdateRiskRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AtlasApiDTOsRisksUpdateRiskRequest, CancellationToken>((_, req, _) => persistedUpdates.Add(req))
            .Returns(Task.CompletedTask);

        AppCacheService cache = AutosaveTestSupport.CreateCache(api.Object);
        cache.AddRisk(initial);
        RiskService service = new(api.Object, cache, debounceDelay: TimeSpan.FromSeconds(30));

        Task debouncedText = service.UpdateAsync(
            riskId,
            r => EntityClone.Risk(r, description: "typed description", evidence: "typed evidence"),
            debounce: true);

        Task teamMembers = service.SetTeamMembersAsync(riskId, [memberId]);
        await teamPersistEntered.Task;
        releaseTeamPersist.TrySetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => teamMembers);
        await debouncedText;

        refetchCount.Should().Be(1);
        Risk cachedAfterFailure = cache.TryGetRisk(riskId)!;
        cachedAfterFailure.Description.Should().Be("typed description");
        cachedAfterFailure.Evidence.Should().Be("typed evidence");
        cachedAfterFailure.LinkedTeamMemberIds.Should().BeEmpty();
        persistedUpdates.Should().BeEmpty();
        service.GetSaveState(riskId).Should().Be(EntitySaveState.Failed);

        await service.UpdateAsync(riskId, r => r, debounce: false);

        persistedUpdates.Should().ContainSingle();
        persistedUpdates[0].Description.Should().Be("typed description");
        persistedUpdates[0].Evidence.Should().Be("typed evidence");
        service.GetSaveState(riskId).Should().Be(EntitySaveState.Saved);
    }

    [Fact]
    public async Task SetTeamMembersAsync_WhenDebouncedTextThenTeamMembers_PersistsDescriptionViaUpdateRisk()
    {
        var riskId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        Risk initial = new()
        {
            Id = riskId,
            Title = "Risk",
            Status = RiskStatus.Open,
            Severity = "Low",
            Description = "initial",
            Evidence = "",
            LinkedTeamMemberIds = []
        };

        List<AtlasApiDTOsRisksUpdateRiskRequest> persistedUpdates = [];
        int teamMemberCalls = 0;
        Mock<IAtlasApiClient> api = new();
        api.Setup(x => x.AtlasApiEndpointsRisksSetRiskTeamMembersEndpointAsync(
                riskId,
                It.IsAny<AtlasApiDTOsRisksSetRiskTeamMembersRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => teamMemberCalls++)
            .Returns(Task.CompletedTask);
        api.Setup(x => x.AtlasApiEndpointsRisksUpdateRiskEndpointAsync(
                riskId,
                It.IsAny<AtlasApiDTOsRisksUpdateRiskRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AtlasApiDTOsRisksUpdateRiskRequest, CancellationToken>((_, req, _) => persistedUpdates.Add(req))
            .Returns(Task.CompletedTask);

        AppCacheService cache = AutosaveTestSupport.CreateCache(api.Object);
        cache.AddRisk(initial);
        RiskService service = new(api.Object, cache, debounceDelay: TimeSpan.FromSeconds(30));

        Task debouncedText = service.UpdateAsync(
            riskId,
            r => EntityClone.Risk(r, description: "typed description"),
            debounce: true);

        await service.SetTeamMembersAsync(riskId, [memberId]);
        await debouncedText;

        persistedUpdates.Should().ContainSingle();
        persistedUpdates[0].Description.Should().Be("typed description");
        teamMemberCalls.Should().Be(1);
        service.GetSaveState(riskId).Should().Be(EntitySaveState.Saved);
    }

    [Fact]
    public async Task SetTeamMembersAsync_WhenConcurrentDebouncedTextCompletes_PersistsBothChanges()
    {
        var riskId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        Risk initial = new()
        {
            Id = riskId,
            Title = "Risk",
            Status = RiskStatus.Open,
            Severity = "Low",
            Description = "initial",
            Evidence = "",
            LinkedTeamMemberIds = []
        };

        List<IReadOnlyList<Guid>> persistedMembers = [];
        List<AtlasApiDTOsRisksUpdateRiskRequest> persistedUpdates = [];
        Mock<IAtlasApiClient> api = new();
        api.Setup(x => x.AtlasApiEndpointsRisksSetRiskTeamMembersEndpointAsync(
                riskId,
                It.IsAny<AtlasApiDTOsRisksSetRiskTeamMembersRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AtlasApiDTOsRisksSetRiskTeamMembersRequest, CancellationToken>((_, req, _) =>
                persistedMembers.Add((req.TeamMemberIds ?? []).ToList()))
            .Returns(Task.CompletedTask);
        api.Setup(x => x.AtlasApiEndpointsRisksUpdateRiskEndpointAsync(
                riskId,
                It.IsAny<AtlasApiDTOsRisksUpdateRiskRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AtlasApiDTOsRisksUpdateRiskRequest, CancellationToken>((_, req, _) => persistedUpdates.Add(req))
            .Returns(Task.CompletedTask);

        AppCacheService cache = AutosaveTestSupport.CreateCache(api.Object);
        cache.AddRisk(initial);
        RiskService service = new(api.Object, cache);

        Task debouncedText = service.UpdateAsync(
            riskId,
            r => EntityClone.Risk(r, description: "typed description"),
            debounce: true);

        await service.SetTeamMembersAsync(riskId, [memberId]);
        await debouncedText;

        Risk cached = cache.TryGetRisk(riskId)!;
        cached.Description.Should().Be("typed description");
        cached.LinkedTeamMemberIds.Should().ContainSingle().Which.Should().Be(memberId);
        persistedMembers.Should().ContainSingle().Which.Should().Contain(memberId);
        persistedUpdates.Should().ContainSingle();
        persistedUpdates[0].Description.Should().Be("typed description");
        service.GetSaveState(riskId).Should().Be(EntitySaveState.Saved);
    }
}
