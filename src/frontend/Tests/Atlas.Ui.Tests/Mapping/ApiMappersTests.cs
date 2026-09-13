using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using FluentAssertions;

namespace Atlas.Ui.Tests.Mapping;

public sealed class ApiMappersTests
{
    [Fact]
    public void MapTeamMember_WhenAzureWorkItemProjectCleared_MapsProjectIdToNull()
    {
        AtlasApiDTOsTeamMembersTeamMemberDto dto = new()
        {
            Id = Guid.NewGuid(),
            Name = "Ada",
            Role = "Engineer",
            AzureWorkItems =
            [
                new AtlasApiDTOsTeamMembersTeamMemberAzureWorkItemDto
                {
                    Id = "42",
                    Title = "Bug",
                    Status = "Active",
                    ProjectId = Guid.Empty
                }
            ]
        };

        TeamMemberMapResult result = ApiMappers.MapTeamMember(dto);

        result.Member.AzureItems.Should().ContainSingle().Which.ProjectId.Should().BeNull();
    }
}
