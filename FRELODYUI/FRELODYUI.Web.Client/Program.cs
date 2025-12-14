using Blazored.LocalStorage;
using FRELODYSHRD.Interfaces;
using FRELODYSHRD.Services;
using FRELODYUI.Services;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using FRELODYUI.Web.Client.Services;
using FRELODYUI.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseAddressApi = new Uri("https://localhost:7077");

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddTransient<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISongsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ISongBooksApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IChordsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IChordChartsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<IPlaylistsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi)
                .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddRefitClient<ICategoriesApi>()
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
builder.Services.AddScoped<HeroDataService>();
builder.Services.AddScoped<UserSettingsService>();
builder.Services.AddScoped<GlobalAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
                provider.GetRequiredService<GlobalAuthStateProvider>());
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITimeHelper, TimeHelper>();
builder.Services.AddScoped<ICurrencyConverter, CurrencyConverter>();
builder.Services.AddScoped<ICurrencyDisplayService, CurrencyDisplayService>();
await builder.Build().RunAsync();
