using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Atlas.Ui;
using Atlas.Ui.Contracts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAtlasUiServices(builder.Configuration);

WebAssemblyHost host = builder.Build();

// Single startup hydration path. Fire-and-forget is intentional: EnsureHydratedAsync
// latches one in-flight task, and pages still await it in OnInitializedAsync.
// ShellLayout must not start a second background hydrate.
IAppCacheService cache = host.Services.GetRequiredService<IAppCacheService>();
_ = HydrateInBackgroundAsync(cache);

await host.RunAsync();

static async Task HydrateInBackgroundAsync(IAppCacheService cache)
{
    try
    {
        await cache.EnsureHydratedAsync();
    }
    catch (Exception ex)
    {
        cache.RecordHydrationFailure(ex);
    }
}
