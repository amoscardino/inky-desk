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
    private static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);
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

            models.AddRange(GetEvents(cal, calendar.Name, calendar.Offset ?? 0));
        }

        return models
            .Distinct(new EventModelComparer())
            .OrderByDescending(x => x.IsAllDay)
            .ThenBy(x => x.Start)
            .Take(4)
            .ToList();
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
        if (evt.RecurrenceRules.Any())
            return null;

        var evtStart = evt.Start.ToTimeZone(TimeZoneInfo.Local.StandardName).Value;

        if (evtStart.Date != now.Date)
            return null;

        if (!evt.IsAllDay && evtStart < now)
            return null;

        return new EventModel
        {
            CalendarName = calendarName,
            Title = GetTitle(evt.Summary),
            Location = evt.Location ?? string.Empty,
            Notes = evt.Description ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = evtStart
        };
    }

    private EventModel? GetOccurrenceEvent(CalendarEvent evt, string calendarName, DateTime now)
    {
        if (!evt.RecurrenceRules.Any())
            return null;

        var startTime = new CalDateTime(now.Date, evt.Start.TimeZoneName);
        var endTime = new CalDateTime(now.Date.AddDays(1).AddMinutes(-1), evt.Start.TimeZoneName);
        var occurrences = evt.GetOccurrences(startTime, endTime).ToList();

        if (occurrences.Count == 0)
            return null;

        var evtStart = occurrences.First().Period.StartTime.ToTimeZone(TimeZoneInfo.Local.StandardName).Value;

        if (evtStart.Date != now.Date)
            return null;

        if (!evt.IsAllDay && evtStart < now)
            return null;

        return new EventModel
        {
            CalendarName = calendarName,
            Title = GetTitle(evt.Summary),
            Location = evt.Location ?? string.Empty,
            Notes = evt.Description ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = evtStart
        };
    }

    private string GetTitle(string title)
    {
        var newTitle = title;

        foreach (var replacement in replacements)
            newTitle = newTitle.Replace(replacement.Find, replacement.Replace, StringComparison.OrdinalIgnoreCase);

        return newTitle;
    }
}