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
    private const float MarginSm = Margin / 2f;
    private const float MarginLg = Margin * 1.5f;
    private const float MarginXl = Margin * 2;

    private const float Width = 400 * Unit;
    private const float Height = 300 * Unit;

    private const float DateWidth = 110 * Unit;
    private const float EventsWidth = Width - DateWidth - Margin;

    private readonly CalendarService _calendarService;
    private readonly WeatherService _weatherService;

    private readonly FontCollection _fontCollection;
    private readonly Brush _brushBlack;
    private readonly Brush _brushRed;
    private readonly Brush _brushWhite;
    private readonly DrawingOptions _lineDrawingOptions;

    public ImageService(CalendarService calendarService, WeatherService weatherService)
    {
        _calendarService = calendarService;
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
        var events = await _calendarService.GetEventsAsync();
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
        imageContext.FillPolygon(_brushRed, new PointF(0, 0), new PointF(DateWidth, 0), new PointF(DateWidth, Height), new PointF(0, Height));

        var now = DateTime.Now;

        var date = now.ToString("%d");
        var dateFont = _fontCollection.Get("Noto Sans").CreateFont(92 * Unit, FontStyle.Bold);
        var dateOptions = new RichTextOptions(dateFont);
        var dateRect = TextMeasurer.MeasureAdvance(date, dateOptions);
        dateOptions.HorizontalAlignment = HorizontalAlignment.Center;
        dateOptions.Origin = new PointF(DateWidth / 2, (Height / 2f) - (dateRect.Height / 2f) - MarginXl);
        imageContext.DrawText(_lineDrawingOptions, dateOptions, date, _brushWhite, null);

        var month = now.ToString("MMM");
        var monthFont = _fontCollection.Get("Noto Sans").CreateFont(36 * Unit, FontStyle.Bold);
        var monthOptions = new RichTextOptions(monthFont);
        var monthRect = TextMeasurer.MeasureAdvance(month, monthOptions);
        monthOptions.HorizontalAlignment = HorizontalAlignment.Center;
        monthOptions.Origin = new PointF(DateWidth / 2, (Height / 2f) - (dateRect.Height / 2f) - monthRect.Height - MarginXl - MarginSm);
        imageContext.DrawText(_lineDrawingOptions, monthOptions, month, _brushWhite, null);

        var dayOfWeek = now.ToString("ddd");
        var dayOfWeekFont = _fontCollection.Get("Noto Sans").CreateFont(36 * Unit, FontStyle.Bold);
        var dayOfWeekOptions = new RichTextOptions(dayOfWeekFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(DateWidth / 2, (Height / 2f) + (dateRect.Height / 2f) - MarginXl)
        };
        imageContext.DrawText(_lineDrawingOptions, dayOfWeekOptions, dayOfWeek, _brushWhite, null);
    }

    private void DrawEvents(IImageProcessingContext imageContext, List<EventModel> events)
    {
        var today = DateTime.Today;
        var eventY = MarginXl;

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
            var evtTitleOptions = evt.IsAllDay && evt.Start.Date != today ? titleAltOptions : titleOptions;
            var title = TruncateText(evt.Title, EventsWidth - MarginLg - MarginLg, evtTitleOptions);

            evtTitleOptions.Origin = new PointF(DateWidth + MarginLg, eventY);

            imageContext.DrawText(_lineDrawingOptions, evtTitleOptions, title, _brushBlack, null);

            eventY += (float)Math.Ceiling(TextMeasurer.MeasureBounds(title, evtTitleOptions).Height);

            if (!evt.IsAllDay)
            {
                eventY += Margin;

                var timeLocation = $"{evt.Start:h:mm tt}".ToLower();

                if (!string.IsNullOrWhiteSpace(evt.Location))
                    timeLocation += $" @ {evt.Location}";

                timeLocation = TruncateText(timeLocation, EventsWidth - MarginLg - MarginLg, timeLocationOptions);

                timeLocationOptions.Origin = new PointF(DateWidth + MarginLg, eventY);

                imageContext.DrawText(_lineDrawingOptions, timeLocationOptions, timeLocation, _brushBlack, null);

                eventY += (float)Math.Ceiling(TextMeasurer.MeasureBounds(timeLocation, timeLocationOptions).Height);
            }

            if (i < events.Count - 1)
            {
                eventY += MarginLg;

                imageContext.DrawLine(_lineDrawingOptions, _brushBlack, 1f, new PointF(DateWidth + MarginXl + MarginXl, eventY), new PointF(Width, eventY));

                eventY += MarginLg;
            }
        }
    }

    private void DrawNoEvents(IImageProcessingContext imageContext)
    {
        var text = "Nothing!";
        var font = _fontCollection.Get("Noto Sans").CreateFont(24 * Unit, FontStyle.Italic);
        var options = new RichTextOptions(font);
        var rect = TextMeasurer.MeasureBounds(text, options);

        options.Origin = new PointF(DateWidth + (EventsWidth / 2) - (rect.Width / 2), (Height / 2) - (rect.Height / 2));

        imageContext.DrawText(_lineDrawingOptions, options, text, _brushBlack, null);
    }

    private void DrawWeather(IImageProcessingContext imageContext, (string, string) weather)
    {
        var weatherFont = _fontCollection.Get("Noto Sans").CreateFont(16 * Unit, FontStyle.Bold);
        var weatherOptions = new RichTextOptions(weatherFont) { HorizontalAlignment = HorizontalAlignment.Center };

        var line1 = TruncateText(weather.Item1, DateWidth - MarginLg, weatherOptions);
        var line2 = TruncateText(weather.Item2, DateWidth - MarginLg, weatherOptions);

        var weatherRect1 = TextMeasurer.MeasureBounds(line1, weatherOptions);
        var weatherRect2 = TextMeasurer.MeasureBounds(line2, weatherOptions);

        weatherOptions.Origin = new PointF(DateWidth / 2, Height - Margin - weatherRect1.Height - MarginSm - weatherRect2.Height);
        imageContext.DrawText(_lineDrawingOptions, weatherOptions, line1, _brushWhite, null);

        weatherOptions.Origin = new PointF(DateWidth / 2, Height - Margin - weatherRect1.Height);
        imageContext.DrawText(_lineDrawingOptions, weatherOptions, line2, _brushWhite, null);
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