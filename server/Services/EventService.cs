using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using InkyDesk.Server.Models;

namespace InkyDesk.Server.Services;

public class EventService(
    CalendarService calendarService,
    IHttpClientFactory httpClientFactory
)
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("calendar");

    public async Task<List<EventModel>> GetEventsAsync()
    {
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
            .ThenBy(x => x.End)
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

    private static List<EventModel> GetEvents(Calendar calendar, string calendarName, int offset)
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

    private static EventModel? GetRegularEvent(CalendarEvent evt, string calendarName, DateTime now)
    {
        if (evt.RecurrenceRules.Any())
            return null;

        if (evt.Start.Value.Date != now.Date)
            return null;

        if (!evt.IsAllDay)
        {
            var evtTime = evt.Start.IsUtc ? evt.Start.Value.ToLocalTime() : evt.Start.Value;

            if (evtTime < now)
                return null;
        }

        return new EventModel
        {
            CalendarName = calendarName,
            Title = evt.Summary,
            Location = evt.Location ?? string.Empty,
            Notes = evt.Description ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = evt.Start.Value,
            End = evt.End?.Value
        };
    }

    private static EventModel? GetOccurrenceEvent(CalendarEvent evt, string calendarName, DateTime now)
    {
        if (!evt.RecurrenceRules.Any())
            return null;

        var startTime = new CalDateTime(now.Date, evt.Start.TimeZoneName);
        var endTime = new CalDateTime(now.Date.AddDays(1).AddMinutes(-1), evt.Start.TimeZoneName);
        var occurrences = evt.GetOccurrences(startTime, endTime).ToList();

        if (occurrences.Count == 0)
            return null;

        var occurrence = occurrences.First();

        if (!evt.IsAllDay)
        {
            var evtTime = occurrence.Period.StartTime.IsUtc ? occurrence.Period.StartTime.Value.ToLocalTime() : occurrence.Period.StartTime.Value;

            if (evtTime < now)
                return null;
        }

        return new EventModel
        {
            CalendarName = calendarName,
            Title = evt.Summary,
            Location = evt.Location ?? string.Empty,
            Notes = evt.Description ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = occurrence.Period.StartTime.Value,
            End = occurrence.Period.EndTime?.Value
        };
    }
}