using Blazored.LocalStorage;
using FRELODYSHRD.Interfaces;
using FRELODYUI.Services;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using FRELODYUI.Web.Client.Services;
using FRELODYUI.Web.Components;
using FRELODYUI.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IShareService, ShareService>();
builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddScoped<IStorageService, WebStorageService>();
builder.Services.AddScoped<ChordLyricExtrator>();
builder.Services.AddScoped<TabManagementService>();
builder.Services.AddScoped<GlobalAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
                provider.GetRequiredService<GlobalAuthStateProvider>());
var baseAddressApi = new Uri("https://localhost:7018");

builder.Services.AddRefitClient<ISongsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISongBooksApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ICategoriesApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISongCollectionsApi>()
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


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(FRELODYUI.Shared._Imports).Assembly,
        typeof(FRELODYUI.Web.Client._Imports).Assembly);

app.Run();
