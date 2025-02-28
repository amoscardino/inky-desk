using System.Reflection;
using InkyDesk.Server.Data;
using InkyDesk.Server.Models;
using InkyDesk.Server.Services;
using Microsoft.EntityFrameworkCore;

// Get assembly information for the user agent string
var assembly = Assembly.GetEntryAssembly()!;
var name = assembly.GetName().Name;
var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
var userAgent = $"{name} v{version}";

// Configure QuestPDF settings
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.EnableDebugging = true;
QuestPDF.Settings.FontDiscoveryPaths.Add("Fonts");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<InkyDeskDataContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("InkyDeskData"));
});

builder.Services.AddHttpClient("calendar");
builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov");
    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
});
builder.Services.AddTransient<CalendarService>();
builder.Services.AddTransient<WeatherService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InkyDeskDataContext>();
    await db.Database.MigrateAsync();
}

app.UseDeveloperExceptionPage();

app.MapGet("/", () =>
{
    return userAgent;
});

app.MapGet("/events", async (CalendarService calendarService) =>
{
    return await calendarService.GetEventsAsync();
});

app.MapGet("/weather", async (WeatherService weatherService) =>
{
    return await weatherService.GetWeatherAsync();
});

app.MapGet("/image", async (CalendarService calendarService, WeatherService weatherService) =>
{
    var events = await calendarService.GetEventsAsync();
    var weather = await weatherService.GetWeatherAsync();
    var document = new CalendarDocument(events, weather);
    var imageBytes = document.ToImage();

    return Results.File(imageBytes, "image/png");
});

app.Run();
