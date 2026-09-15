using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Team
{
    public partial class MemberWorkItemsTab
    {
        [Inject] private NavigationManager _nav { get; set; } = null!;

        [Parameter, EditorRequired] public TeamMember Member { get; set; } = null!;

        private string Query { get; set; } = "";
        private string QuickFilter { get; set; } = "All";
        private string StatusFilter { get; set; } = "All";

        private List<string> StatusOptions =>
            new[] { "All" }.Concat(Member.AzureItems.Select(a => a.Status).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s)).ToList();

        private List<AzureItem> Filtered
        {
            get
            {
                var q = this.Query.Trim().ToLowerInvariant();
                IEnumerable<AzureItem> items = Member.AzureItems;
                if (this.QuickFilter == "Current")
                {
                    items = items.Where(a => TeamLogic.IsCurrentTicketStatus(a.Status));
                }
                else if (this.QuickFilter == "Blocked")
                {
                    items = items.Where(a => a.Status.Contains("blocked", StringComparison.OrdinalIgnoreCase));
                }
                else if (this.QuickFilter == "InReview")
                {
                    items = items.Where(a =>
                    {
                        var s = a.Status.ToLowerInvariant();
                        return s.Contains("code review") || s.Contains("in review") || s.Contains("review");
                    });
                }

                if (this.StatusFilter != "All")
                {
                    items = items.Where(a => a.Status == this.StatusFilter);
                }

                if (!string.IsNullOrEmpty(q))
                {
                    items = items.Where(a => $"{a.Id} {a.Title} {a.Status}".ToLowerInvariant().Contains(q));
                }

                return items.ToList();
            }
        }

        private void OpenItem(string id) => this._nav.NavigateTo($"/team/{Member.Id}/work-items/{id}");
    }
}
