using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using FluentAssertions;

namespace Atlas.Ui.Tests.Mapping
{
    public sealed class EntityIdMatchingTests
    {
        [Fact]
        public void MatchesIdFilter_WhenAll_AcceptsAnyId()
        {
            EntityIdMatching.MatchesIdFilter(null, EntityIdMatching.All).Should().BeTrue();
            EntityIdMatching.MatchesIdFilter(Guid.NewGuid(), EntityIdMatching.All).Should().BeTrue();
        }

        [Fact]
        public void MatchesIdFilter_WhenNone_AcceptsOnlyMissingId()
        {
            EntityIdMatching.MatchesIdFilter(null, EntityIdMatching.None).Should().BeTrue();
            EntityIdMatching.MatchesIdFilter(Guid.NewGuid(), EntityIdMatching.None).Should().BeFalse();
        }

        [Fact]
        public void MatchesIdFilter_WhenGuid_RequiresExactId()
        {
            var alpha = Guid.NewGuid();
            var beta = Guid.NewGuid();
            EntityIdMatching.MatchesIdFilter(alpha, alpha.ToString()).Should().BeTrue();
            EntityIdMatching.MatchesIdFilter(beta, alpha.ToString()).Should().BeFalse();
            EntityIdMatching.MatchesIdFilter(null, alpha.ToString()).Should().BeFalse();
        }

        [Fact]
        public void DisplayNameById_DoesNotCrossMatchDuplicateNames()
        {
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            Project[] projects =
            [
                new() { Id = firstId, Name = "Atlas" },
                new() { Id = secondId, Name = "Atlas" }
            ];

            EntityIdMatching.DisplayNameById(firstId, projects, p => p.Id, p => p.Name).Should().Be("Atlas");
            EntityIdMatching.DisplayNameById(secondId, projects, p => p.Id, p => p.Name).Should().Be("Atlas");
            EntityIdMatching.MatchesIdFilter(firstId, secondId.ToString()).Should().BeFalse();
        }

        [Fact]
        public void CompareLinkedDisplay_UsesNameThenId()
        {
            var alpha = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var beta = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            EntityIdMatching.CompareLinkedDisplay(alpha, beta, "Beta", "Alpha", 1).Should().BeGreaterThan(0);
            EntityIdMatching.CompareLinkedDisplay(alpha, beta, "Same", "Same", 1).Should().BeNegative();
        }
    }
}
