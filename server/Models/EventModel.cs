namespace InkyDesk.Server.Models;

public class EventModel
{
    public string CalendarName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public bool IsAllDay { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
}