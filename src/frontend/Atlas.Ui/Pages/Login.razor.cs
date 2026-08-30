using Microsoft.AspNetCore.Components;

namespace Atlas.Ui.Pages;

public partial class Login
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private void Continue() => Nav.NavigateTo("/dashboard");
}
