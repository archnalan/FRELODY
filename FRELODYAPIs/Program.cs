using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Areas.Admin.LogicData;
using FRELODYAPP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Data;
using FRELODYAPP.Models.SubModels;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Mapster;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Scalar.AspNetCore;
using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Areas.Admin.LogicData;
using System.Text.Json;
using FRELODYAPP.Data.Extensions;
using FRELODYAPP.Profiles;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using FRELODYSHRD.Services;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.Interfaces;
using FRELODYAPIs.Authorization;
using FRELODYAPIs.Seeding;
using Microsoft.AspNetCore.Authorization;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("SongData") ?? throw new InvalidOperationException("Connection string 'SongData' not found.");
builder.Services.AddDbContext<SongDbContext>(options =>
	options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();
builder.Services.AddDatabaseSeeder();
builder.Services.AddControllersWithViews();

// Register Mapster mappings
MappingConfig.RegisterMappings();
builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
// Register services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IPesaPalService, PesaPalService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<ICurrencyConverter, CurrencyConverter>();
builder.Services.AddTransient<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IPlaylistService,PlaylistService>();
builder.Services.AddScoped<ISongBookService,SongBookService>();
builder.Services.AddScoped<IArtistService,ArtistService>();
builder.Services.AddScoped<ICategoryService,CategoryService>();
builder.Services.AddScoped<IAlbumService,AlbumService>();
builder.Services.AddScoped<ISongService,SongService>();
builder.Services.AddScoped<IChordChartService, ChordChartService>();
builder.Services.AddScoped<IChordService, ChordService>();
builder.Services.AddScoped<ILyricSegment, LyricSegmentService>();
builder.Services.AddScoped<ILyricLineService, LyricLineService>();
builder.Services.AddScoped<ISongPartService, SongPartService>();
if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IEmailService, DevEmailService>();
else
    builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ISongPlayHistoryService, SongPlayHistoryService>();
builder.Services.AddScoped<IAuthService, AuthorizationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ISmtpSenderService,SmtpSenderService>();
builder.Services.AddScoped<IShareLinkService, ShareLinkService>();
builder.Services.AddScoped<FRELODYAPIs.Services.OgCard.IOgCardService, FRELODYAPIs.Services.OgCard.OgCardService>();
builder.Services.Configure<FRELODYAPIs.Controllers.ShareLandingOptions>(
    builder.Configuration.GetSection(FRELODYAPIs.Controllers.ShareLandingOptions.SectionName));
builder.Services.AddScoped<FileValidationService>();
builder.Services.AddScoped<SecurityUtilityService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ContentChangeTrackingService>();
builder.Services.AddScoped<ISongAiService, SongAiService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddHttpClient("NvidiaAI", client =>
{
    client.BaseAddress = new Uri("https://integrate.api.nvidia.com/v1/");
    var nvidiaKey = builder.Configuration["API_KEYS:nvidiaApiKey"];
    if (!string.IsNullOrEmpty(nvidiaKey))
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", nvidiaKey);
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddIdentity<User, IdentityRole>
            (options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<SongDbContext>()
            .AddDefaultTokenProviders();

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Query.TryGetValue("access_key_value_temp_refresh", out var token))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validation failed : {headers}", context.Request.Headers);

                logger.LogError(context.Exception, "JWT validation failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                //log token
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated: {token}", context.SecurityToken);

                var TenantId = context.Request.Headers["TenantId"].FirstOrDefault();
                //if (TenantId == null)
                //{

                //    context.Fail("TenantId header missing");
                //    context.Response.StatusCode = 401;
                //    context.Response.ContentType = "application/json";
                //    var result = System.Text.Json.JsonSerializer.Serialize(new { message = $"Missing TenantId header" });
                //    return context.Response.WriteAsync(result);

                //}
                var token = context.SecurityToken as JsonWebToken;
                var tenantIdFromToken = token.GetPayloadValue<string>("TenantId");


                if (!string.IsNullOrEmpty(tenantIdFromToken))
                {
                    //Logger.LogInformation("Here is the client id from the request {clientId}", clientId);
                    //if (tenantIdFromToken != TenantId)
                    //{
                    //    context.Fail("Invalid TenantId");
                    //    context.Response.StatusCode = 401;
                    //    context.Response.ContentType = "application/json";
                    //    var result = System.Text.Json.JsonSerializer.Serialize(new { message = $"Invalid TenantId header or Token" });
                    //    return context.Response.WriteAsync(result);
                    //}
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddRazorComponents();
builder.Services.AddRazorPages();

// Org-tier role authorization (custom [OrgRole(...)] attribute).
builder.Services.AddSingleton<IAuthorizationPolicyProvider, OrgRolePolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, OrgRoleAuthorizationHandler>();
builder.Services.AddAuthorization();

// Startup seeders for Identity roles + SuperAdmin.
builder.Services.AddScoped<RoleSeeder>();
builder.Services.AddScoped<SuperAdminSeeder>();
// Register the global exception handler
//builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		//options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddLogging();
//builder.Services.AddScoped<TextFileUploadService>();

builder.Services.AddCors(options =>
					options.AddPolicy("AllowAll", builder => 
					builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Configure controllers to properly handle areas for API documentation
builder.Services.AddControllers(options => {
    // Add convention to include area name in the route for Swagger
    options.Conventions.Add(new RouteTokenTransformerConvention(
        new SlugifyParameterTransformer()));
});

// Add OpenAPI services
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "FRELODYAPP API";
        document.Info.Version = "v1";
        document.Info.Description = "API for managing songs with chords and lyrics";
        return Task.CompletedTask;
    });
});

// ── OpenTelemetry observability ─────────────────────────────────────────────
//
//  Local development  → console exporter only  (OpenTelemetry:EnableConsole=true)
//  Local Docker       → OTLP→otel-collector    (OpenTelemetry:Endpoint set via env vars)
//  Docker Production  → OTLP→logs.frelody.com  (OpenTelemetry:Endpoint set via env vars)
//
var otelEndpoint    = builder.Configuration["OpenTelemetry:Endpoint"];
var otelServiceName = builder.Configuration["OpenTelemetry:ServiceName"]    ?? "frelody-api";
var otelServiceVer  = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
var otelHeaders     = builder.Configuration["OpenTelemetry:Headers"];
var enableConsole   = builder.Configuration.GetValue<bool>("OpenTelemetry:EnableConsole");
var hasOtlp         = !string.IsNullOrWhiteSpace(otelEndpoint);

// Auto-compute Basic auth header from superuser credentials when no explicit
// header is supplied but an OTLP endpoint IS configured (used in production).
if (hasOtlp && string.IsNullOrEmpty(otelHeaders))
{
    var otelUser = builder.Configuration["UserSettings:UserName"]     ?? "superuser";
    var otelPass = builder.Configuration["UserSettings:UserPassword"] ?? string.Empty;
    otelHeaders  = "Authorization=Basic " +
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{otelUser}:{otelPass}"));
}

builder.Services.AddOpenTelemetry()
    .ConfigureResource(res => res
        .AddService(
            serviceName:       otelServiceName,
            serviceVersion:    otelServiceVer,
            serviceInstanceId: Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"]              = Environment.MachineName,
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                // Exclude health / metrics / framework paths from trace noise
                opts.Filter = ctx =>
                    !ctx.Request.Path.StartsWithSegments("/health") &&
                    !ctx.Request.Path.StartsWithSegments("/metrics") &&
                    !ctx.Request.Path.StartsWithSegments("/_");
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();

        // Local development: human-readable console output
        if (enableConsole)
            tracing.AddConsoleExporter();

        // Docker (local or production): push to OTLP collector / backend
        if (hasOtlp)
            tracing.AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri($"{otelEndpoint!.TrimEnd('/')}/v1/traces");
                opts.Protocol = OtlpExportProtocol.HttpProtobuf;
                if (!string.IsNullOrEmpty(otelHeaders))
                    opts.Headers = otelHeaders;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (enableConsole)
            metrics.AddConsoleExporter();

        if (hasOtlp)
            metrics.AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri($"{otelEndpoint!.TrimEnd('/')}/v1/metrics");
                opts.Protocol = OtlpExportProtocol.HttpProtobuf;
                if (!string.IsNullOrEmpty(otelHeaders))
                    opts.Headers = otelHeaders;
            });
    });

builder.Logging.AddOpenTelemetry(otelLogging =>
{
    otelLogging.IncludeFormattedMessage = true;
    otelLogging.IncludeScopes           = true;
    otelLogging.ParseStateValues        = true;

    if (enableConsole)
        otelLogging.AddConsoleExporter();

    if (hasOtlp)
        otelLogging.AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri($"{otelEndpoint!.TrimEnd('/')}/v1/logs");
            opts.Protocol = OtlpExportProtocol.HttpProtobuf;
            if (!string.IsNullOrEmpty(otelHeaders))
                opts.Headers = otelHeaders;
        });
});
// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<SongDbContext>();
		context.Database.Migrate(); //Ensures migrations are applied
	}
	catch(Exception ex)
	{		
		logger.LogError(ex, "An Error occured when while seeding data in the Database");
	}
}

	// Configure the HTTP request pipeline.
	if (app.Environment.IsDevelopment())
	{
		app.UseMigrationsEndPoint();
    
}
	else
	{
		// Use the global exception handler in production
		app.UseExceptionHandler();
		// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
		app.UseHsts();
	}
// Add OpenAPI Enabled in ALL environments
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("FRELODYAPP API Documentation")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});
// Add global exception handling middleware
app.UseExceptionHandler();

app.UseCors("AllowAll");

// Redirect to HTTPS only when an HTTPS port is configured.
// In Docker the container runs plain HTTP behind Nginx, so skip this
// to prevent redirect loops.
if (!string.IsNullOrEmpty(app.Configuration["ASPNETCORE_HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}

app.MapStaticAssets();

// Serve runtime-generated assets (e.g. Open Graph preview PNGs written to
// wwwroot/share-og/{token}.png) which aren't part of the build-time manifest.
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedDataAsync();

        // Ensure all Identity roles exist (platform + org tiers).
        var roleSeeder = services.GetRequiredService<RoleSeeder>();
        await roleSeeder.SeedAsync();

        // Idempotently elevate SUPERADMIN_SEED_EMAIL to SuperAdmin.
        var superAdminSeeder = services.GetRequiredService<SuperAdminSeeder>();
        await superAdminSeeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var dbLogger = services.GetRequiredService<ILogger<Program>>();
        dbLogger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Helper class for route transformations
public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string TransformOutbound(object value)
    {
        if (value == null) return null;
        var str = value.ToString()!;
        // Pass 1: split runs of uppercase before an uppercase+lowercase pair
        //   e.g. "OAuthConfig" → "O-AuthConfig"  (catches OAuth, HTML, etc.)
        str = Regex.Replace(str, "([A-Z]+)([A-Z][a-z])", "$1-$2");
        // Pass 2: split lowercase→uppercase boundaries
        //   e.g. "GetGoogle" → "Get-Google"
        str = Regex.Replace(str, "([a-z])([A-Z])", "$1-$2");
        return str.ToLower();
    }
}
