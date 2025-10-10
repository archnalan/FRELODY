using Blazored.LocalStorage;
using FRELODYSHRD.Interfaces;
using FRELODYUI.Services;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using FRELODYUI.Web.Client.Services;
using FRELODYUI.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseAddressApi = new Uri("https://localhost:7018");

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddRefitClient<ISongsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<ISongBooksApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<IChordsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<IChordChartsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<ISongCollectionsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<ICategoriesApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<IShareApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<ISongSectionsApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<IFeedbackApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<ISettingsApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<ISongPlayHistoryApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<IAuthApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

builder.Services.AddRefitClient<IUsersApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

// Add device-specific services used by the FRELODYUI.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
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
await builder.Build().RunAsync();
