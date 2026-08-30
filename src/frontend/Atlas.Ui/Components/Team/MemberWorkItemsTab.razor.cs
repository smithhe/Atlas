using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Components.Team;

public partial class MemberWorkItemsTab
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Parameter, EditorRequired] public TeamMember Member { get; set; } = null!;

    private string _query = "";
    private string _quickFilter = "All";
    private string _statusFilter = "All";

    private List<string> StatusOptions =>
        new[] { "All" }.Concat(Member.AzureItems.Select(a => a.Status).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s)).ToList();

    private List<AzureItem> Filtered
    {
        get
        {
            var q = _query.Trim().ToLowerInvariant();
            IEnumerable<AzureItem> items = Member.AzureItems;
            if (_quickFilter == "Current")
            {
                items = items.Where(a => TeamLogic.IsCurrentTicketStatus(a.Status));
            }
            else if (_quickFilter == "Blocked")
            {
                items = items.Where(a => a.Status.Contains("blocked", StringComparison.OrdinalIgnoreCase));
            }
            else if (_quickFilter == "InReview")
            {
                items = items.Where(a =>
                {
                    var s = a.Status.ToLowerInvariant();
                    return s.Contains("code review") || s.Contains("in review") || s.Contains("review");
                });
            }

            if (_statusFilter != "All")
            {
                items = items.Where(a => a.Status == _statusFilter);
            }

            if (!string.IsNullOrEmpty(q))
            {
                items = items.Where(a => $"{a.Id} {a.Title} {a.Status}".ToLowerInvariant().Contains(q));
            }

            return items.ToList();
        }
    }

    private void OpenItem(string id) => Nav.NavigateTo($"/team/{Member.Id}/work-items/{id}");
}
