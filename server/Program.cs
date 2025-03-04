using System.Reflection;
using InkyDesk.Server.Data;
using InkyDesk.Server.Services;
using Microsoft.EntityFrameworkCore;

// Get assembly information for the user agent string
var assembly = Assembly.GetEntryAssembly()!;
var name = assembly.GetName().Name;
var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
var userAgent = $"{name} v{version}";

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
builder.Services.AddTransient<ImageService>();

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
    var weather = await weatherService.GetWeatherAsync();
    return $"{weather.Item1} {weather.Item2}";
});

app.MapGet("/image", async (ImageService imageService) =>
{
    var imageBytes = await imageService.GetImageAsync();

    return Results.File(imageBytes, "image/png");
});

app.Run();
