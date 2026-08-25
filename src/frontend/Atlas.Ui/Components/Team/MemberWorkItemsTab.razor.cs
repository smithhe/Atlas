using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberWorkItemsTab
{
    [Inject] NavigationManager Nav { get; set; } = default!;

    [Parameter, EditorRequired] public TeamMember Member { get; set; } = default!;

    string _query = "";
    string _quickFilter = "All";
    string _statusFilter = "All";

    List<string> StatusOptions =>
        new[] { "All" }.Concat(Member.AzureItems.Select(a => a.Status).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s)).ToList();

    List<AzureItem> Filtered
    {
        get
        {
            string q = _query.Trim().ToLowerInvariant();
            IEnumerable<AzureItem> items = Member.AzureItems;
            if (_quickFilter == "Current")
                items = items.Where(a => TeamLogic.IsCurrentTicketStatus(a.Status));
            else if (_quickFilter == "Blocked")
                items = items.Where(a => a.Status.Contains("blocked", StringComparison.OrdinalIgnoreCase));
            else if (_quickFilter == "InReview")
                items = items.Where(a =>
                {
                    string s = a.Status.ToLowerInvariant();
                    return s.Contains("code review") || s.Contains("in review") || s.Contains("review");
                });

            if (_statusFilter != "All")
                items = items.Where(a => a.Status == _statusFilter);

            if (!string.IsNullOrEmpty(q))
                items = items.Where(a => $"{a.Id} {a.Title} {a.Status}".ToLowerInvariant().Contains(q));

            return items.ToList();
        }
    }

    void OpenItem(string id) => Nav.NavigateTo($"/team/{Member.Id}/work-items/{id}");
}
