﻿using Refit;
using FRELODYUI.Services;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using Microsoft.Extensions.Logging;

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

            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<IApiResponseHandler, ApiResponseHandler>();

            var baseAddressApi = new Uri("https://localhost:7077");

            builder.Services.AddRefitClient<ISongsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);
            builder.Services.AddRefitClient<ISongBooksApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);
            builder.Services.AddRefitClient<ICategoriesApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);
            builder.Services.AddRefitClient<ISongCollectionsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);
            builder.Services.AddRefitClient<IChordsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);
            builder.Services.AddRefitClient<IChordChartsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);


            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
