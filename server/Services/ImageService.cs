using InkyDesk.Server.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InkyDesk.Server.Services;

public class ImageService
{
    private const int Unit = 1;

    private const int Margin = 8 * Unit;
    private const int MarginHalf = (int)(Margin / 2d);
    private const int MarginDouble = Margin * 2;

    private const int Width = 400 * Unit;
    private const int Height = 300 * Unit;

    private const int DateWidth = 120 * Unit;
    private const int EventsWidth = Width - DateWidth - Margin;

    private readonly CalendarService _calendarService;
    private readonly WeatherService _weatherService;

    private readonly FontCollection _fontCollection;
    private readonly Brush _brush;
    private readonly Brush _brushAlt;
    private readonly DrawingOptions _lineDrawingOptions;

    public ImageService(CalendarService calendarService, WeatherService weatherService)
    {
        _calendarService = calendarService;
        _weatherService = weatherService;
        _fontCollection = new FontCollection();
        _fontCollection.Add("Fonts/ChareInk6SP-Regular.ttf");
        _fontCollection.Add("Fonts/ChareInk6SP-Bold.ttf");
        _fontCollection.Add("Fonts/ChareInk6SP-Italic.ttf");
        _fontCollection.Add("Fonts/ChareInk6SP-BoldItalic.ttf");
        _fontCollection.Add("Fonts/NotoEmoji-Regular.ttf");
        _fontCollection.Add("Fonts/NotoEmoji-Bold.ttf");

        _brush = Brushes.Solid(Color.Black);
        _brushAlt = Brushes.Solid(Color.Red);

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

        using var image = new Image<Rgb24>(Width, Height, Color.White.ToPixel<Rgb24>());

        image.Mutate(DrawDate);
        image.Mutate(imageContext => DrawEvents(imageContext, events));
        image.Mutate(imageContext => DrawWeather(imageContext, weather.Item1, weather.Item2));

        using var memoryStream = new MemoryStream();
        await image.SaveAsPngAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream.ToArray();
    }

    private void DrawDate(IImageProcessingContext imageContext)
    {
        var now = DateTime.Now;

        var date = now.ToString("%d");
        var dateFont = _fontCollection.Get("ChareInk6SP").CreateFont(92 * Unit, FontStyle.Bold);
        var dateOptions = new RichTextOptions(dateFont);
        var dateRect = TextMeasurer.MeasureAdvance(date, dateOptions);
        dateOptions.HorizontalAlignment = HorizontalAlignment.Center;
        dateOptions.Origin = new PointF(DateWidth / 2, (Height / 2f) - (dateRect.Height / 2f) - MarginDouble);
        imageContext.DrawText(dateOptions, date, _brushAlt);

        var month = now.ToString("MMM");
        var monthFont = _fontCollection.Get("ChareInk6SP").CreateFont(36 * Unit, FontStyle.Bold);
        var monthOptions = new RichTextOptions(monthFont);
        var monthRect = TextMeasurer.MeasureAdvance(month, monthOptions);
        monthOptions.HorizontalAlignment = HorizontalAlignment.Center;
        monthOptions.Origin = new PointF(DateWidth / 2, (Height / 2f) - (dateRect.Height / 2f) - monthRect.Height - MarginDouble);
        imageContext.DrawText(monthOptions, month, _brush);

        var dayOfWeek = now.ToString("ddd");
        var dayOfWeekFont = _fontCollection.Get("ChareInk6SP").CreateFont(36 * Unit, FontStyle.Bold);
        var dayOfWeekOptions = new RichTextOptions(dayOfWeekFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(DateWidth / 2, (Height / 2f) + (dateRect.Height / 2f) - MarginDouble)
        };
        imageContext.DrawText(dayOfWeekOptions, dayOfWeek, _brush);

        imageContext.DrawLine(_lineDrawingOptions, _brush, 2f, new PointF(DateWidth - 0.5f, Margin), new PointF(DateWidth - 0.5f, Height - Margin));
    }

    private void DrawEvents(IImageProcessingContext imageContext, List<EventModel> events)
    {
        var eventY = MarginDouble;

        // TODO: Emoji font as fallback?
        var titleFont = _fontCollection.Get("ChareInk6SP").CreateFont(24 * Unit, FontStyle.Bold);
        var timeLocationFont = _fontCollection.Get("ChareInk6SP").CreateFont(18 * Unit, FontStyle.Regular);

        var titleOptions = new RichTextOptions(titleFont) { WrappingLength = EventsWidth };
        var timeOptions = new RichTextOptions(timeLocationFont) { WrappingLength = EventsWidth / 2 };
        var locationOptions = new RichTextOptions(timeLocationFont) { WrappingLength = EventsWidth / 2 };

        for (int i = 0; i < events.Count; i++)
        {
            var evt = events[i];

            var titleRect = TextMeasurer.MeasureBounds(evt.Title, titleOptions);

            if (evt.IsAllDay)
            {

            }
        }
    }

    private void DrawWeather(IImageProcessingContext imageContext, string weatherLine1, string weatherLine2)
    {
        var weatherFont = _fontCollection.Get("ChareInk6SP").CreateFont(16 * Unit, FontStyle.Bold);
        var weatherOptions = new RichTextOptions(weatherFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var weatherRect1 = TextMeasurer.MeasureBounds(weatherLine1, weatherOptions);
        var weatherRect2 = TextMeasurer.MeasureBounds(weatherLine2, weatherOptions);

        weatherOptions.Origin = new PointF(DateWidth / 2, Height - Margin - weatherRect1.Height - MarginHalf - weatherRect2.Height);
        imageContext.DrawText(weatherOptions, weatherLine1, _brush);


        weatherOptions.Origin = new PointF(DateWidth / 2, Height - Margin - weatherRect1.Height);
        imageContext.DrawText(weatherOptions, weatherLine2, _brush);
    }
}