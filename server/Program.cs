using System.Reflection;
using InkyDesk.Server.Services;

// Get assembly information for the user agent string
var assembly = Assembly.GetEntryAssembly()!;
var name = assembly.GetName().Name;
var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
var userAgent = $"{name} v{version}";

var builder = WebApplication.CreateBuilder(args);

// Named HTTP clients
builder.Services.AddHttpClient("calendar");
builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov");
    // weather.gov requires a user agent string
    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
});

// Custom services
builder.Services.AddTransient<CalendarService>();
builder.Services.AddTransient<EventService>();
builder.Services.AddTransient<WeatherService>();
builder.Services.AddTransient<ImageService>();

var app = builder.Build();

// Always return detailed errors
app.UseDeveloperExceptionPage();

// Default endpoint just returns the user agent string. Useful for debugging.
app.MapGet("/", () =>
{
    return userAgent;
});

// Returns the calendar data as JSON. Useful for debugging.
app.MapGet("/calendars", async (CalendarService calendarService) =>
{
    return await calendarService.GetCalendarsAsync();
});

// Returns event data as JSON. Useful for debugging.
app.MapGet("/events", async (EventService eventService) =>
{
    return await eventService.GetEventsAsync();
});

// Returns weather data as JSON. Useful for debugging.
app.MapGet("/weather", async (WeatherService weatherService) =>
{
    var weather = await weatherService.GetWeatherAsync();
    return $"{weather.Item1} {weather.Item2}";
});

// Returns an image as a PNG file. This is what is actually called by the client script.
app.MapGet("/image", async (ImageService imageService) =>
{
    var imageBytes = await imageService.GetImageAsync();

    return Results.File(imageBytes, "image/png");
});

app.Run();
