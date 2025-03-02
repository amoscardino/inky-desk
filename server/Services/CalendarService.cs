using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using InkyDesk.Server.Data;
using InkyDesk.Server.Models;
using Microsoft.EntityFrameworkCore;
using Calendar = Ical.Net.Calendar;

namespace InkyDesk.Server.Services;

public class CalendarService(
    InkyDeskDataContext dataContext,
    IHttpClientFactory httpClientFactory
)
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("calendar");

    public async Task<List<EventModel>> GetEventsAsync()
    {
        var models = new List<EventModel>();
        var calendars = await dataContext.Calendars
            .Where(c => c.IsEnabled)
            .ToListAsync();

        foreach (var calendar in calendars)
        {
            var calendarText = await GetCalendarTextAsync(calendar.Url);

            if (string.IsNullOrWhiteSpace(calendarText))
                continue;

            var cal = Calendar.Load(calendarText);

            models.AddRange(GetEvents(cal, calendar.Name, calendar.Offset ?? 0));
        }

        return models
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

        if (!evt.IsAllDay && evt.Start.Value.ToLocalTime() < now)
            return null;

        return new EventModel
        {
            CalendarName = calendarName,
            Title = evt.Summary,
            Location = evt.Location ?? string.Empty,
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

        return new EventModel
        {
            CalendarName = calendarName,
            Title = evt.Summary,
            Location = evt.Location ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = occurrence.Period.StartTime.Value,
            End = occurrence.Period.EndTime?.Value
        };
    }
}