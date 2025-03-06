using InkyDesk.Server.Models;
using NodaTime;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InkyDesk.Server.Services;

public class ImageService
{
    private const float Unit = 1;

    private const float Margin = 8 * Unit;
    private const float MarginXs = Margin / 4f;
    private const float MarginSm = Margin / 2f;
    private const float MarginLg = Margin * 1.5f;
    private const float MarginXl = Margin * 2;

    private const float Width = 400 * Unit;
    private const float Height = 300 * Unit;

    private const float DateWidth = 110 * Unit;
    private const float EventsWidth = Width - DateWidth - Margin;
    private const float EventsTextWidth = EventsWidth - MarginLg - MarginLg;

    private readonly EventService _eventService;
    private readonly WeatherService _weatherService;

    private readonly FontCollection _fontCollection;
    private readonly Brush _brushBlack;
    private readonly Brush _brushRed;
    private readonly Brush _brushWhite;
    private readonly DrawingOptions _lineDrawingOptions;

    public ImageService(EventService eventService, WeatherService weatherService)
    {
        _eventService = eventService;
        _weatherService = weatherService;

        _fontCollection = new FontCollection();
        _fontCollection.Add("Fonts/NotoSans-Regular.ttf");
        _fontCollection.Add("Fonts/NotoSans-Bold.ttf");
        _fontCollection.Add("Fonts/NotoSans-Italic.ttf");
        _fontCollection.Add("Fonts/NotoSans-BoldItalic.ttf");
        _fontCollection.Add("Fonts/NotoEmoji-Regular.ttf");
        _fontCollection.Add("Fonts/NotoEmoji-Bold.ttf");

        _brushBlack = Brushes.Solid(Color.Black);
        _brushRed = Brushes.Solid(Color.Red);
        _brushWhite = Brushes.Solid(Color.White);

        _lineDrawingOptions = new DrawingOptions
        {
            GraphicsOptions = new GraphicsOptions
            {
                Antialias = false
            }
        };
    }

    public async Task<byte[]> GetImageAsync()
    {
        var events = await _eventService.GetEventsAsync();
        var weather = await _weatherService.GetWeatherAsync();

        using var image = new Image<Rgb24>((int)Width, (int)Height, Color.White.ToPixel<Rgb24>());

        image.Mutate(DrawDate);
        image.Mutate(imageContext => DrawWeather(imageContext, weather));

        if (events.Count != 0)
            image.Mutate(imageContext => DrawEvents(imageContext, events));
        else
            image.Mutate(DrawNoEvents);

        using var memoryStream = new MemoryStream();
        await image.SaveAsPngAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream.ToArray();
    }

    private void DrawDate(IImageProcessingContext imageContext)
    {
        // Background box
        imageContext.FillPolygon(_brushRed, new PointF(0, 0), new PointF(DateWidth, 0), new PointF(DateWidth, Height), new PointF(0, Height));

        // Strings
        var now = DateTime.Now;
        var date = now.ToString("%d");
        var month = now.ToString("MMM");
        var dayOfWeek = now.ToString("ddd");

        // Fonts
        var dateFont = _fontCollection.Get("Noto Sans").CreateFont(92 * Unit, FontStyle.Bold);
        var otherFont = _fontCollection.Get("Noto Sans").CreateFont(36 * Unit, FontStyle.Bold);

        // Options
        var dateOptions = new RichTextOptions(dateFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(DateWidth / 2f, (Height / 2f) - MarginXl)
        };

        // Needed for placement of other text
        var dateRect = TextMeasurer.MeasureAdvance(date, dateOptions);

        var monthOptions = new RichTextOptions(otherFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Origin = new PointF(DateWidth / 2, (Height / 2f) - (dateRect.Height / 2f) - MarginLg)
        };

        var dayOfWeekOptions = new RichTextOptions(otherFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(DateWidth / 2, (Height / 2f) + (dateRect.Height / 2f) - MarginXl + MarginXs)
        };

        // Draw text
        imageContext.DrawText(_lineDrawingOptions, dateOptions, date, _brushWhite, null);
        imageContext.DrawText(_lineDrawingOptions, monthOptions, month, _brushWhite, null);
        imageContext.DrawText(_lineDrawingOptions, dayOfWeekOptions, dayOfWeek, _brushWhite, null);
    }

    private void DrawEvents(IImageProcessingContext imageContext, List<EventModel> events)
    {
        var today = DateTime.Today;
        var eventX = DateWidth + MarginLg;
        var eventY = MarginXl; // This will be updated as we draw events

        var titleFont = _fontCollection.Get("Noto Sans").CreateFont(24 * Unit, FontStyle.Bold);
        var titleAltFont = _fontCollection.Get("Noto Sans").CreateFont(24 * Unit, FontStyle.BoldItalic);
        var timeLocationFont = _fontCollection.Get("Noto Sans").CreateFont(18 * Unit, FontStyle.Regular);
        var emojiFont = _fontCollection.Get("Noto Emoji");

        var titleOptions = new RichTextOptions(titleFont) { FallbackFontFamilies = [emojiFont] };
        var titleAltOptions = new RichTextOptions(titleAltFont) { FallbackFontFamilies = [emojiFont] };
        var timeLocationOptions = new RichTextOptions(timeLocationFont) { FallbackFontFamilies = [emojiFont] };

        for (int i = 0; i < events.Count; i++)
        {
            var evt = events[i];

            // Event Title
            var evtTitleOptions = evt.IsAllDay && evt.Start.Date != today ? titleAltOptions : titleOptions;
            var title = TruncateText(evt.Title, EventsTextWidth, evtTitleOptions);

            evtTitleOptions.Origin = new PointF(eventX, eventY);

            imageContext.DrawText(_lineDrawingOptions, evtTitleOptions, title, _brushBlack, null);

            eventY += (float)Math.Ceiling(TextMeasurer.MeasureBounds(title, evtTitleOptions).Height);

            // For non-all day events, add time and location line
            if (!evt.IsAllDay)
            {
                eventY += Margin;

                var timeLocation = $"{evt.Start:h:mm tt}".ToLower();

                if (!string.IsNullOrWhiteSpace(evt.Location))
                    timeLocation += $" — {evt.Location}";

                timeLocation = TruncateText(timeLocation, EventsTextWidth, timeLocationOptions);

                timeLocationOptions.Origin = new PointF(eventX, eventY);

                imageContext.DrawText(_lineDrawingOptions, timeLocationOptions, timeLocation, _brushBlack, null);

                eventY += (float)Math.Ceiling(TextMeasurer.MeasureBounds(timeLocation, timeLocationOptions).Height);
            }

            // Separator
            if (i < events.Count - 1)
            {
                eventY += MarginLg;

                imageContext.DrawLine(_lineDrawingOptions, _brushBlack, 1f, new PointF(eventX, eventY), new PointF(Width, eventY));

                eventY += MarginLg;
            }
        }
    }

    private void DrawNoEvents(IImageProcessingContext imageContext)
    {
        var text = "Nothing!";
        var font = _fontCollection.Get("Noto Sans").CreateFont(24 * Unit, FontStyle.Italic);
        var options = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(DateWidth + (EventsWidth / 2f), Height / 2f)
        };

        imageContext.DrawText(_lineDrawingOptions, options, text, _brushBlack, null);
    }

    private void DrawWeather(IImageProcessingContext imageContext, (string, string) weather)
    {
        var font = _fontCollection.Get("Noto Sans").CreateFont(16 * Unit, FontStyle.Bold);
        var options = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var line1 = TruncateText(weather.Item1, DateWidth - MarginLg, options);
        var line2 = TruncateText(weather.Item2, DateWidth - MarginLg, options);
        var line1Rect = TextMeasurer.MeasureBounds(line1, options);

        options.Origin = new PointF(DateWidth / 2, Height - Margin - line1Rect.Height - Margin);
        imageContext.DrawText(_lineDrawingOptions, options, line1, _brushWhite, null);

        options.Origin = new PointF(DateWidth / 2, Height - Margin);
        imageContext.DrawText(_lineDrawingOptions, options, line2, _brushWhite, null);
    }

    private static string TruncateText(string text, float width, RichTextOptions options)
    {
        var rect = TextMeasurer.MeasureBounds(text, options);

        while (rect.Width >= width)
        {
            text = text.TrimEnd('…')[..^1] + "…";
            rect = TextMeasurer.MeasureBounds(text, options);
        }

        return text;
    }
}