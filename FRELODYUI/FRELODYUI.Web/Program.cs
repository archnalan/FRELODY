using Blazored.LocalStorage;
using FRELODYSHRD.Interfaces;
using FRELODYSHRD.Services;
using FRELODYUI.Services;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using FRELODYUI.Web.Client.Services;
using FRELODYUI.Web.Components;
using FRELODYUI.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Persist Data Protection keys to a configurable directory.
// In Docker this is a named volume mount so keys survive container restarts.
// Without this, every restart rotates the key ring and existing browser
// antiforgery cookies / auth tokens become undecryptable → blank screen.
var dpKeysDir = builder.Configuration["ASPNETCORE_DataProtection__KeysDirectory"]
                ?? Path.Combine(builder.Environment.ContentRootPath, "dp-keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysDir));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddSingleton<IFormFactor, FRELODYUI.Web.Client.Services.FormFactor>();
builder.Services.AddSingleton<IApiResponseHandler, ApiResponseHandler>();
builder.Services.AddScoped<IPrintService, WebPrintService>();
builder.Services.AddScoped<IClipboardService, WebClipboardService>();
builder.Services.AddScoped<ICameraService, WebCameraService>();
builder.Services.AddScoped<IShareService, ShareService>();
builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IStorageService, WebStorageService>();
builder.Services.AddScoped<ChordLyricExtrator>();
builder.Services.AddScoped<ISongExtractionAiService, SongExtractionAiService>();
builder.Services.AddScoped<TabManagementService>();
builder.Services.AddScoped<HeroDataService>();
builder.Services.AddScoped<UserSettingsService>();
builder.Services.AddScoped<GlobalAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
                provider.GetRequiredService<GlobalAuthStateProvider>());
builder.Services.AddSingleton<ITimeHelper, TimeHelper>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrencyConverter, CurrencyConverter>();
builder.Services.AddScoped<ICurrencyDisplayService, CurrencyDisplayService>();
var baseAddressApi = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7077");

builder.Services.AddHttpClient("TokenRefresh", c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<ISongsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISongBooksApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ICategoriesApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IPlaylistsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IChordsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IChordChartsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IShareApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISongSectionsApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IFeedbackApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISettingsApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IArtistsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IAlbumsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISongPlayHistoryApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IAuthApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IUsersApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ITenantsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IOtpApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IChatsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IPesaPalApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IProductsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISmtpSenderApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IContentChangeTrackingApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISongAiApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IOcrApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Treat both "Development" (local VS debug) and "Docker" (local container)
// as dev-like environments so that debugging and error pages are available.
var isDevLike = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker");
if (isDevLike)
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Redirect to HTTPS only when an HTTPS port is actually configured.
// In Docker the container runs plain HTTP (ASPNETCORE_HTTPS_PORT=""),
// so this is skipped – preventing a blank page caused by a redirect loop.
if (!string.IsNullOrEmpty(app.Configuration["ASPNETCORE_HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(FRELODYUI.Shared._Imports).Assembly,
        typeof(FRELODYUI.Web.Client._Imports).Assembly);

app.Run();
