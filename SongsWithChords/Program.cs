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
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Swashbuckle.AspNetCore.SwaggerGen;
using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Areas.Admin.LogicData;
using System.Text.Json;
using FRELODYAPP.Data.Extensions;
using SongsWithChords.Middleware;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("SongData") ?? throw new InvalidOperationException("Connection string 'SongData' not found.");
builder.Services.AddDbContext<SongDbContext>(options =>
	options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.AddDatabaseSeeder();
builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
	.AddEntityFrameworkStores<SongDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ISongCollectionService,SongCollectionService>();
builder.Services.AddScoped<ISongBookService,SongBookService>();
builder.Services.AddScoped<ICategoryService,CategoryService>();
builder.Services.AddScoped<ISongService,SongService>();
builder.Services.AddScoped<IChordChartService, ChordChartService>();
builder.Services.AddScoped<IChordService, ChordService>();
builder.Services.AddScoped<ILyricSegment, LyricSegmentService>();
builder.Services.AddScoped<ILyricLineService, LyricLineService>();
builder.Services.AddScoped<ISongPartService, SongPartService>();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ISongPlayHistoryService, SongPlayHistoryService>();
builder.Services.AddScoped<SmtpSenderService>();
builder.Services.AddScoped<FileValidationService>();
builder.Services.AddScoped<SecurityUtilityService>();
builder.Services.AddScoped<TokenService>();

// Register the global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
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
builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddCors(options =>
					options.AddPolicy("AllowAll", builder => 
					builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Configure controllers to properly handle areas for API documentation
builder.Services.AddControllers(options => {
    // Add convention to include area name in the route for Swagger
    options.Conventions.Add(new RouteTokenTransformerConvention(
        new SlugifyParameterTransformer()));
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FRELODYAPP API",
        Version = "v1",
        Description = "API for managing songs with chords and lyrics",
        Contact = new OpenApiContact
        {
            Name = "FRELODY",
            Email = "support@example.com"
        }
    });

    // Enable JWT Authentication in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Properly handle area-prefixed routes
    options.DocumentFilter<AddAreaRouteDocumentFilter>();

    // Configure operation IDs to avoid duplicates
    options.CustomOperationIds(apiDesc =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
        var areaName = apiDesc.ActionDescriptor.RouteValues.ContainsKey("area") ?
            apiDesc.ActionDescriptor.RouteValues["area"] : string.Empty;

        if (!string.IsNullOrEmpty(areaName))
        {
            return $"{areaName}_{controllerName}_{actionName}";
        }

        return $"{controllerName}_{actionName}";
    });

    // Include XML comments if you have them
    /*
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
    */
});

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
    // Add Swagger middleware only in development
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FRELODYAPP API v1");
        options.RoutePrefix = string.Empty;
        options.DocumentTitle = "FRELODYAPP API Documentation";
        options.DefaultModelsExpandDepth(-1); // Hide models section by default
        options.EnableDeepLinking(); // Enable direct linking to operations
        options.DisplayRequestDuration(); // Display request duration
    });
}
	else
	{
		// Use the global exception handler in production
		app.UseExceptionHandler();
		// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
		app.UseHsts();
	}

// Add global exception handling middleware
app.UseExceptionHandler();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

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
    }
    catch (Exception ex)
    {
        var dbLogger = services.GetRequiredService<ILogger<Program>>();
        dbLogger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Helper class for handling Area routes in Swagger
public class AddAreaRouteDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new OpenApiPaths();

        foreach (var path in swaggerDoc.Paths)
        {
            paths.Add(path.Key, path.Value);
        }

        swaggerDoc.Paths = paths;
    }
}

// Helper class for route transformations
public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string TransformOutbound(object value)
    {
        return value == null ? null : Regex.Replace(value.ToString(), "([a-z])([A-Z])", "$1-$2").ToLower();
    }
}
