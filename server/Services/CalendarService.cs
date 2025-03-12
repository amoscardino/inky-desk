using System.Text.Json;
using InkyDesk.Server.Models;

namespace InkyDesk.Server.Services;

public class CalendarService(IConfiguration config)
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<CalendarModel>> GetCalendarsAsync()
    {
        var configPath = config.GetValue<string>("ConfigPath")!;
        var fullPath = Path.Combine(configPath, "calendars.json");

        if (!File.Exists(fullPath))
            throw new Exception("Calendars file not found");

        var json = await File.ReadAllTextAsync(fullPath);

        var calendars = JsonSerializer.Deserialize<List<CalendarModel>>(json, jsonSerializerOptions) ?? [];

        return [.. calendars.Where(c => c.IsEnabled)];
    }
}