using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InkyDesk.Server.Models;

public class CalendarDocument(List<EventModel> events) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        var now = DateTime.Now;

        container.Page(page =>
        {
            page.Size(400, 300);
            page.DefaultTextStyle(TextStyle.Default.FontSize(18));
            page.Margin(4);

            page.Content()
                .Extend()
                .Row(row =>
                {
                    row.RelativeItem(0.3f)
                        .AlignMiddle()
                        .AlignCenter()
                        .Column(col =>
                        {
                            col.Item()
                                .AlignCenter()
                                .Text(now.ToString("MMM"))
                                .FontSize(36)
                                .LetterSpacing(0.05f)
                                .Bold();
                            col.Item()
                                .AlignCenter()
                                .Text(now.ToString("dd"))
                                .FontSize(92)
                                .ExtraBold()
                                .LineHeight(0.9f);
                            col.Item()
                                .AlignCenter()
                                .Text(now.ToString("ddd"))
                                .FontSize(36)
                                .LetterSpacing(0.05f)
                                .Bold();
                        });

                    row.AutoItem()
                        .Padding(8)
                        .LineVertical(1);

                    row.RelativeItem(0.7f)
                        .AlignTop()
                        .AlignCenter()
                        .Column(col =>
                        {
                            col.Spacing(8);

                            for (int i = 0; i < events.Count; i++)
                            {
                                var evt = events[i];

                                if (i > 0)
                                {
                                    col.Item().PaddingHorizontal(8).LineHorizontal(1);
                                }

                                col.Item()
                                    .Column(evtCol =>
                                    {
                                        evtCol.Item()
                                            .PaddingBottom(4)
                                            .Text(evt.Title)
                                            .Bold()
                                            .FontSize(24)
                                            .ClampLines(1);

                                        if (!evt.IsAllDay)
                                        {
                                            evtCol.Item()
                                                .Row(evtRow =>
                                                {
                                                    evtRow.RelativeItem(0.3f)
                                                        .AlignLeft()
                                                        .AlignMiddle()
                                                        .Text(evt.Start.ToString("h:mm tt").ToLower());

                                                    evtRow.RelativeItem(0.7f)
                                                        .AlignRight()
                                                        .AlignMiddle()
                                                        .Text(evt.Location)
                                                        .Light()
                                                        .Italic()
                                                        .ClampLines(1);
                                                });
                                        }
                                    });
                            }
                        });
                });
        });
    }

    public byte[] ToImage()
    {
        var imageSettings = new ImageGenerationSettings { RasterDpi = 72 };
        var imageBytes = this.GenerateImages(imageSettings).First();

        return imageBytes;
    }
}