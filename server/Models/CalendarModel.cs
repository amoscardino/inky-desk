namespace InkyDesk.Server.Models;

public class CalendarModel
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int? Offset { get; set; }

    public bool IsEnabled { get; set; }
}