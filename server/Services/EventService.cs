using System.Text.Json;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using InkyDesk.Server.Models;

namespace InkyDesk.Server.Services;

public class EventService(
    CalendarService calendarService,
    ReplacementsService replacementsService,
    IHttpClientFactory httpClientFactory
)
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("calendar");

    private List<ReplacementModel> replacements = [];

    public async Task<List<EventModel>> GetEventsAsync()
    {
        replacements = await replacementsService.GetReplacementsAsync();

        var models = new List<EventModel>();
        var calendars = await calendarService.GetCalendarsAsync();

        foreach (var calendar in calendars)
        {
            var calendarText = await GetCalendarTextAsync(calendar.Url);

            if (string.IsNullOrWhiteSpace(calendarText))
                continue;

            var cal = Calendar.Load(calendarText);

            if (cal == null)
                continue;

            models.AddRange(GetEvents(cal, calendar.Name, calendar.Offset ?? 0));
        }

        return [.. models
            .Distinct(new EventModelComparer())
            .OrderByDescending(x => x.IsAllDay)
            .ThenBy(x => x.Start)
            .Take(4)];
    }

    private async Task<string> GetCalendarTextAsync(string url)
    {
        var response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();

        return string.Empty;
    }

    private List<EventModel> GetEvents(Calendar calendar, string calendarName, int offset)
    {
        var calEvents = new List<EventModel>();
        var now = DateTime.Now.AddDays(offset);

        foreach (var evt in calendar.Events)
        {
            var regularEvent = GetRegularEvent(evt, calendarName, now);

            if (regularEvent != null)
            {
                calEvents.Add(regularEvent);
                continue;
            }

            var occurrenceEvent = GetOccurrenceEvent(evt, calendarName, now);

            if (occurrenceEvent != null)
            {
                calEvents.Add(occurrenceEvent);
                continue;
            }
        }

        return calEvents;
    }

    private EventModel? GetRegularEvent(CalendarEvent evt, string calendarName, DateTime now)
    {
        if (evt.RecurrenceRule != null || evt.Start == null)
            return null;

        var evtStart = GetLocalStart(evt.Start);

        if (evtStart.Date != now.Date)
            return null;

        if (!evt.IsAllDay && evtStart < now)
            return null;

        return new EventModel
        {
            CalendarName = calendarName,
            Title = GetTitle(evt.Summary ?? string.Empty),
            Location = evt.Location ?? string.Empty,
            Notes = evt.Description ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = evtStart
        };
    }

    private EventModel? GetOccurrenceEvent(CalendarEvent evt, string calendarName, DateTime now)
    {
        if (evt.RecurrenceRule == null)
            return null;

        // Expand over the local day, not the event's own zone: `now` is local and the day we
        // filter to is local, so a zone-relative window can straddle the wrong local date.
        var dayStart = new CalDateTime(now.Date, TimeZoneInfo.Local.Id);
        var dayEnd = new CalDateTime(now.Date.AddDays(1), TimeZoneInfo.Local.Id);

        // GetOccurrences is lazy and unbounded for an open-ended rule, so stop at the day
        // boundary rather than taking a fixed count -- the first occurrence back can start
        // before today (a multi-day span) or already be over, and would hide today's.
        var evtStart = evt
            .GetOccurrences(dayStart)
            .TakeWhile(o => o.Period.StartTime < dayEnd)
            .Select(o => GetLocalStart(o.Period.StartTime))
            .Where(start => start.Date == now.Date && (evt.IsAllDay || start >= now))
            .Cast<DateTime?>()
            .FirstOrDefault();

        if (evtStart == null)
            return null;

        return new EventModel
        {
            CalendarName = calendarName,
            Title = GetTitle(evt.Summary ?? string.Empty),
            Location = evt.Location ?? string.Empty,
            Notes = evt.Description ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = evtStart.Value
        };
    }

    // A null TZID means a floating time, which is already local by definition.
    private static DateTime GetLocalStart(CalDateTime dateTime)
        => dateTime.TimeZoneName == null
            ? dateTime.Value
            : dateTime.ToTimeZone(TimeZoneInfo.Local.Id).Value;

    private string GetTitle(string title)
    {
        var newTitle = title;

        foreach (var replacement in replacements)
            newTitle = newTitle.Replace(replacement.Find, replacement.Replace, StringComparison.OrdinalIgnoreCase);

        return newTitle;
    }
}