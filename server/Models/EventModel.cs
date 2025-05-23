namespace InkyDesk.Server.Models;

public class EventModel
{
    public string CalendarName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public bool IsAllDay { get; set; }
    public DateTime Start { get; set; }

    public string GetTitleDisplay()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return string.Empty;

        return Title
            .Replace("FW:", string.Empty, StringComparison.CurrentCultureIgnoreCase)
            .Replace("RE:", string.Empty, StringComparison.CurrentCultureIgnoreCase)
            .Trim();
    }

    public string GetLocationDisplay()
    {
        if (string.IsNullOrWhiteSpace(Location))
            return string.Empty;

        var map = new Dictionary<string, string>
        {
            { "zoom.us", "Zoom" },
            { "teams.microsoft.com", "Teams" },
            { "Microsoft Teams", "Teams" }
        };

        foreach (var key in map.Keys)
        {
            if (Location.Contains(key) || Notes.Contains(key))
                return map[key];
        }

        // Replace new lines with spaces and return
        return Location
            .Replace("\r\n", " ", StringComparison.CurrentCultureIgnoreCase)
            .Replace("\r", " ", StringComparison.CurrentCultureIgnoreCase)
            .Replace("\n", " ", StringComparison.CurrentCultureIgnoreCase)
            .Trim();
    }
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
               x.Start == y.Start;
    }

    public int GetHashCode(EventModel obj)
    {
        return HashCode.Combine(obj.CalendarName, obj.Title, obj.Location, obj.IsAllDay, obj.Start);
    }
}