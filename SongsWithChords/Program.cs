using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using SongsWithChords.Areas.Admin.Interfaces;
using SongsWithChords.Areas.Admin.LogicData;
using SongsWithChords.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SongsWithChords.Data.Infrastructure;
using SongsWithChords.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("SongData") ?? throw new InvalidOperationException("Connection string 'SongData' not found.");
builder.Services.AddDbContext<SongDbContext>(options =>
	options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
	.AddEntityFrameworkStores<SongDbContext>();
builder.Services.AddControllersWithViews();

//Register Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpContextAccessor();

// Register chart service
builder.Services.AddScoped<IChordChartService, ChordChartService>();
builder.Services.AddScoped<IChordService, ChordService>();
builder.Services.AddScoped<ILyricSegment, LyricSegmentService>();
builder.Services.AddScoped<ILyricLineService, LyricLineService>();
builder.Services.AddScoped<IVerseService, VerseService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<SmtpSenderService>();
builder.Services.AddScoped<FileValidationService>();
builder.Services.AddScoped<SecurityUtilityService>();
builder.Services.AddScoped<TokenService>();

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
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

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<SongDbContext>();
		context.Database.Migrate(); //Ensures migrations are applied
		CategoryData.Initialize(services);
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
		app.UseExceptionHandler("/Home/Error");
		// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
		app.UseHsts();
	}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
