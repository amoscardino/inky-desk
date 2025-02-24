using InkyDesk.Server.Models;
using QuestPDF.Fluent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace InkyDesk.Server.Services;

public class ImageService
{
    public static async Task<byte[]> GenerateImageAsync(List<EventModel> events)
    {
        var document = new CalendarDocument(events);
        var imageBytes = document.GenerateImages().First();
        using var image = Image.Load(imageBytes);

        image.Mutate(x => x.Resize(400, 300));

        using var memoryStream = new MemoryStream();
        await image.SaveAsPngAsync(memoryStream);

        return memoryStream.ToArray();
    }
}