using Microsoft.AspNetCore.Components;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class Login
    {
        [Inject] private NavigationManager _nav { get; set; } = null!;

        private void Continue() => this._nav.NavigateTo("/dashboard");
    }
}
