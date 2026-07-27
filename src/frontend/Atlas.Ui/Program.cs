using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Atlas.Ui;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = (builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5012").TrimEnd('/');

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl + "/")
});

builder.Services.AddScoped<IAtlasApiClient>(sp =>
{
    HttpClient http = sp.GetRequiredService<HttpClient>();
    return new AtlasApiClient(apiBaseUrl, http);
});

builder.Services.AddScoped<SelectionState>();
builder.Services.AddScoped<LocalSettings>();
builder.Services.AddScoped<BrowserDialogs>();
builder.Services.AddScoped<AppCacheService>();

WebAssemblyHost host = builder.Build();

// Kick off hydration topology (settings/projects/productOwners/team → risks → tasks).
_ = host.Services.GetRequiredService<AppCacheService>().EnsureHydratedAsync();

await host.RunAsync();
