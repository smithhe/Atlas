using Microsoft.AspNetCore.Components;

namespace Atlas.Ui.Shared;

public partial class PageTitleRow
{
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string AddLabel { get; set; } = "Add";
    [Parameter] public string AddingLabel { get; set; } = "Adding…";
    [Parameter] public bool Adding { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
}
