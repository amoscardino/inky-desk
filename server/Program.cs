using System.Reflection;
using InkyDesk.Server.Services;

// Get assembly information for the user agent string
var assembly = Assembly.GetEntryAssembly()!;
var name = assembly.GetName().Name;
var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
var userAgent = $"{name} v{version}";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("calendar");
builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov");
    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
});
builder.Services.AddTransient<CalendarService>();
builder.Services.AddTransient<EventService>();
builder.Services.AddTransient<WeatherService>();
builder.Services.AddTransient<ImageService>();

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.MapGet("/", () =>
{
    return userAgent;
});

app.MapGet("/calendars", async (CalendarService calendarService) =>
{
    return await calendarService.GetCalendarsAsync();
});

app.MapGet("/events", async (EventService eventService) =>
{
    return await eventService.GetEventsAsync();
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
