using FRELODYSHRD.Interfaces;
using FRELODYUI.Services;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using FRELODYUI.Web.Components;
using FRELODYUI.Web.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add device-specific services used by the FRELODYUI.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IApiResponseHandler, ApiResponseHandler>();
builder.Services.AddScoped<IPrintService, WebPrintService>();
builder.Services.AddScoped<IClipboardService, WebClipboardService>();
builder.Services.AddScoped<IShareService, ShareService>();
builder.Services.AddScoped<IModalService, ModalService>();

var baseAddressApi = new Uri("https://localhost:7018");

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
builder.Services.AddRefitClient<IShareApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);
builder.Services.AddRefitClient<ISongSectionsApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);
builder.Services.AddRefitClient<IFeedbackApi>()
               .ConfigureHttpClient(c => c.BaseAddress = baseAddressApi);

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
