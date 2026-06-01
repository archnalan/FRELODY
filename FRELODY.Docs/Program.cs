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
