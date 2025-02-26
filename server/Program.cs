using System.Reflection;
using InkyDesk.Server.Data;
using InkyDesk.Server.Models;
using InkyDesk.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<InkyDeskDataContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("InkyDeskData"));
});

builder.Services.AddHttpClient();
builder.Services.AddTransient<CalendarService>();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.EnableDebugging = true;
QuestPDF.Settings.FontDiscoveryPaths.Add("Fonts");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InkyDeskDataContext>();
    await db.Database.MigrateAsync();
}

app.UseDeveloperExceptionPage();

app.MapGet("/", () =>
{
    var assembly = Assembly.GetEntryAssembly()!;
    var name = assembly.GetName().Name;
    var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    return $"{name} v{version}";
});

app.MapGet("/data", async (CalendarService calendarService) =>
{
    return await calendarService.GetEventsAsync();
});

app.MapGet("/image", async (CalendarService calendarService) =>
{
    var events = await calendarService.GetEventsAsync();
    var document = new CalendarDocument(events);
    var imageBytes = document.ToImage();

    return Results.File(imageBytes, "image/png");
});

app.Run();
