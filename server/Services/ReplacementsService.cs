using System.Text.Json;
using InkyDesk.Server.Models;

namespace InkyDesk.Server.Services;

public class ReplacementsService(IConfiguration config)
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<ReplacementModel>> GetReplacementsAsync()
    {
        var configPath = config.GetValue<string>("ConfigPath")!;
        var fullPath = Path.Combine(configPath, "replacements.json");

        if (!File.Exists(fullPath))
            return [];

        var json = await File.ReadAllTextAsync(fullPath);

        var replacements = JsonSerializer.Deserialize<List<ReplacementModel>>(json, jsonSerializerOptions) ?? [];

        return [.. replacements.Where(c => c.IsEnabled)];
    }
}