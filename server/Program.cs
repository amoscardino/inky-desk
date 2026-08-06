using System.Reflection;
using Ical.Net;
using InkyDesk.Server.Services;

// Some calendar clients emit a placeholder TZID (e.g. "Customized Time Zone") that no
// lookup can resolve. Ical.Net's default resolver throws ArgumentException for those, which
// blows up recurrence expansion deep inside GetOccurrences. Treat anything unresolvable as
// the machine's local zone instead, which is what an empty TZID already falls back to.
TimeZoneResolvers.TimeZoneResolver = tzId =>
{
    try
    {
        if (DefaultTimeZoneResolver.GetZone(tzId) is { } zone)
            return zone;
    }
    catch (ArgumentException)
    {
        // The default resolver matched a mapping key but couldn't finish resolving it.
    }

    return DefaultTimeZoneResolver.GetZone(string.Empty);
};

// Get assembly information for the user agent string
var assembly = Assembly.GetEntryAssembly()!;
var name = assembly.GetName().Name;
var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
var userAgent = $"{name} v{version}";

var builder = WebApplication.CreateBuilder(args);

// Named HTTP clients
builder.Services.AddHttpClient("calendar", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
});
builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov");
    // weather.gov requires a user agent string
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
});

// Custom services
builder.Services.AddTransient<CalendarService>();
builder.Services.AddTransient<EventService>();
builder.Services.AddSingleton<ImageService>();
builder.Services.AddTransient<ReplacementsService>();
builder.Services.AddTransient<WeatherService>();

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
app.MapGet("/image", async (ImageService imageService, EventService eventService, WeatherService weatherService) =>
{
    var imageBytes = await imageService.GetImageAsync(eventService, weatherService);

    return Results.File(imageBytes, "image/png");
});

app.Run();
