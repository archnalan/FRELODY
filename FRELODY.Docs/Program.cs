using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FRELODY.Docs;
using FRELODY.Docs.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddSingleton<NavigationDataService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<ThemeService>();

// Documentation media (SuperAdmin-managed screenshots + YouTube embeds) is read from / written to
// the FRELODY API. This client targets the API origin (Api:BaseUrl), distinct from the default
// HttpClient above which serves the docs site's own static content.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl)) apiBaseUrl = builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new DocMediaService(
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) },
    sp.GetRequiredService<AuthService>(),
    apiBaseUrl));

// FRELODY's web app is the single source of truth for sign-in. The docs site
// redirects users to <Web:BaseUrl>/login when they need to authenticate, and
// receives the resulting session back via a URL fragment (#session=...).
var webBase = builder.Configuration["Web:BaseUrl"] ?? string.Empty;
builder.Services.AddScoped(sp => new AuthService(
    sp.GetRequiredService<IJSRuntime>(),
    sp.GetRequiredService<NavigationManager>(),
    webBase));

builder.Services.AddScoped<PdfExportService>();

var host = builder.Build();

// Note: AuthService.InitializeAsync() (session restore) runs from MainLayout
// on first render so it executes inside the same scope as the rest of the UI.

await host.RunAsync();
