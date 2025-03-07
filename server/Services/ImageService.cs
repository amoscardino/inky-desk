using InkyDesk.Server.Models;
using SixLabors.Fonts;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InkyDesk.Server.Services;

public class ImageService
{
    private const float Margin = 8f;
    private const float MarginXs = Margin / 4f;
    private const float MarginSm = Margin / 2f;
    private const float MarginLg = Margin * 1.5f;
    private const float MarginXl = Margin * 2;

    private const float FontSize = 24f;
    private const float FontSizeXs = 16f;
    private const float FontSizeSm = 18f;
    private const float FontSizeLg = 36f;
    private const float FontSizeXl = 92f;

    private const float Width = 400f;
    private const float Height = 300f;

    private const float DateWidth = 120f;
    private const float EventsWidth = Width - DateWidth;
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
        _fontCollection.Add("Fonts/IBMPlexMono-Regular.ttf");
        _fontCollection.Add("Fonts/IBMPlexMono-Bold.ttf");
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
        var dateFont = _fontCollection.Get("IBM Plex Mono").CreateFont(FontSizeXl, FontStyle.Bold);
        var otherFont = _fontCollection.Get("IBM Plex Mono").CreateFont(FontSizeLg, FontStyle.Bold);

        // Options
        var monthOptions = new RichTextOptions(otherFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(DateWidth / 2f, MarginLg)
        };
        var dateOptions = new RichTextOptions(dateFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(DateWidth / 2f, MarginLg + FontSizeLg)
        };
        var dayOfWeekOptions = new RichTextOptions(otherFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Origin = new PointF(DateWidth / 2f, MarginLg + FontSizeLg + FontSizeXl)
        };

        // Draw text
        imageContext.DrawText(_lineDrawingOptions, monthOptions, month, _brushWhite, null);
        imageContext.DrawText(_lineDrawingOptions, dateOptions, date, _brushWhite, null);
        imageContext.DrawText(_lineDrawingOptions, dayOfWeekOptions, dayOfWeek, _brushWhite, null);
    }

    private void DrawEvents(IImageProcessingContext imageContext, List<EventModel> events)
    {
        var today = DateTime.Today;
        var eventX = DateWidth + MarginLg;
        var eventY = MarginXl; // This will be updated as we draw events

        var titleFont = _fontCollection.Get("Noto Sans").CreateFont(FontSize, FontStyle.Bold);
        var altTitleFont = _fontCollection.Get("Noto Sans").CreateFont(FontSize, FontStyle.BoldItalic);
        var timeFont = _fontCollection.Get("Noto Sans").CreateFont(FontSizeSm, FontStyle.Regular);
        var locationFont = _fontCollection.Get("Noto Sans").CreateFont(FontSizeSm, FontStyle.Italic);
        var emojiFont = _fontCollection.Get("Noto Emoji");

        var normalTitleOptions = new RichTextOptions(titleFont) { FallbackFontFamilies = [emojiFont] };
        var altTitleOptions = new RichTextOptions(altTitleFont) { FallbackFontFamilies = [emojiFont] };
        var timeOptions = new RichTextOptions(timeFont);
        var locationOptions = new RichTextOptions(locationFont) { HorizontalAlignment = HorizontalAlignment.Right };

        for (int i = 0; i < events.Count; i++)
        {
            var evt = events[i];

            // Event Title
            var titleOptions = !evt.IsAllDay || evt.Start.Date == today ? normalTitleOptions : altTitleOptions;
            var title = TruncateText(evt.Title, EventsTextWidth, titleOptions);
            var titleBrush = evt.IsAllDay ? _brushRed : _brushBlack;

            titleOptions.Origin = new PointF(eventX, eventY);

            imageContext.DrawText(_lineDrawingOptions, titleOptions, title, titleBrush, null);

            eventY += FontSize;

            // For non-all day events, add time and location line
            if (!evt.IsAllDay)
            {
                eventY += MarginSm;

                var time = $"{evt.Start:h:mm tt}".ToLower();
                var timeWidth = TextMeasurer.MeasureBounds(time, timeOptions).Width;
                var location = TruncateText(evt.Location, EventsTextWidth - timeWidth, locationOptions);

                timeOptions.Origin = new PointF(eventX, eventY);
                locationOptions.Origin = new PointF(Width - MarginSm, eventY);

                imageContext.DrawText(_lineDrawingOptions, timeOptions, time, _brushBlack, null);
                imageContext.DrawText(_lineDrawingOptions, locationOptions, location, _brushBlack, null);

                eventY += FontSizeSm;
            }

            // Separator
            if (i < events.Count - 1)
            {
                eventY += MarginLg;

                imageContext.DrawLine(_lineDrawingOptions, _brushBlack, 1f, new PointF(eventX, eventY), new PointF(Width, eventY));

                eventY += 1f + MarginLg;
            }
        }
    }

    private void DrawNoEvents(IImageProcessingContext imageContext)
    {
        var text = "Nothing!";
        var font = _fontCollection.Get("Noto Sans").CreateFont(FontSize, FontStyle.Italic);
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
        var font = _fontCollection.Get("Noto Sans").CreateFont(FontSizeXs, FontStyle.Bold);
        var options = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var line1 = TruncateText(weather.Item1, DateWidth - MarginSm, options);
        var line2 = TruncateText(weather.Item2, DateWidth - MarginSm, options);

        options.Origin = new PointF(DateWidth / 2, Height - Margin - FontSizeXs - MarginSm);
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