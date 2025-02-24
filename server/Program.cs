using InkyDesk.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddTransient<CalendarService>();
builder.Services.AddTransient<ImageService>();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.EnableDebugging = true;

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.MapGet("/", () =>
{
    return "Hello World!";
});

app.MapGet("/data", async (CalendarService calendarService) =>
{
    return await calendarService.GetEventsAsync();
});

app.MapGet("/image", async (CalendarService calendarService) =>
{
    var events = await calendarService.GetEventsAsync();
    var imageBytes = await ImageService.GenerateImageAsync(events);

    return Results.File(imageBytes, "image/png");
});

app.Run();
