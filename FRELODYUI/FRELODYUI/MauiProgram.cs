using FRELODYLIB.Interfaces;
using FRELODYSHRD.Interfaces;
using FRELODYSHRD.Services;
using FRELODYUI.Services;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Refit;

namespace FRELODYUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddAuthenticationCore();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddTransient<AuthHeaderHandler>();
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<IApiResponseHandler, ApiResponseHandler>();
            builder.Services.AddSingleton<IPrintService, MauiPrintService>();
            builder.Services.AddSingleton<IClipboardService, MauiClipboardService>();
            builder.Services.AddScoped<IShareService, ShareService>();
            builder.Services.AddScoped<IModalService, ModalService>();
            builder.Services.AddScoped<IStorageService, MauiStorageService>();
            builder.Services.AddScoped<ChordLyricExtrator>();
            builder.Services.AddScoped<TabManagementService>();
            builder.Services.AddScoped<HeroDataService>();
            builder.Services.AddScoped<GlobalAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
                provider.GetRequiredService<GlobalAuthStateProvider>());
            builder.Services.AddSingleton<ITimeHelper, TimeHelper>(); 
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<ICurrencyConverter, CurrencyConverter>();
            builder.Services.AddScoped<ICurrencyDisplayService, CurrencyDisplayService>();
            var baseAddressApi = new Uri("https://localhost:7077");

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

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
