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

public class EventModelComparer : IEqualityComparer<EventModel>
{
    public bool Equals(EventModel? x, EventModel? y)
    {
        if (x is null || y is null)
            return false;

        return x.CalendarName == y.CalendarName &&
               x.Title == y.Title &&
               x.Location == y.Location &&
               x.IsAllDay == y.IsAllDay &&
               x.Start == y.Start &&
               x.End == y.End;
    }

    public int GetHashCode(EventModel obj)
    {
        return HashCode.Combine(obj.CalendarName, obj.Title, obj.Location, obj.IsAllDay, obj.Start, obj.End);
    }
}