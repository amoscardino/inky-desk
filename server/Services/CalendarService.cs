using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using InkyDesk.Server.Models;

namespace InkyDesk.Server.Services;

public class CalendarService(IHttpClientFactory httpClientFactory, IConfiguration config)
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient();

    public async Task<List<EventModel>> GetEventsAsync()
    {
        var models = new List<EventModel>();
        var urls = config.GetValue<string>("CalendarUrls")?.Split(",") ?? [];

        foreach (var url in urls)
        {
            var calendarText = await GetCalendarTextAsync(url);

            if (string.IsNullOrWhiteSpace(calendarText))
                continue;

            var calendar = Calendar.Load(calendarText);

            models.AddRange(GetEvents(calendar));
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

    private static List<EventModel> GetEvents(Calendar calendar)
    {
        var calEvents = new List<EventModel>();
        var now = DateTime.Now;

        foreach (var evt in calendar.Events)
        {
            var regularEvent = GetRegularEvent(evt, now);

            if (regularEvent != null)
            {
                calEvents.Add(regularEvent);
                continue;
            }

            var occurrenceEvent = GetOccurrenceEvent(evt, now);

            if (occurrenceEvent != null)
            {
                calEvents.Add(occurrenceEvent);
                continue;
            }
        }

        return calEvents;
    }

    private static EventModel? GetRegularEvent(CalendarEvent evt, DateTime now)
    {
        if (evt.RecurrenceRules.Any())
            return null;

        if (evt.Start.Value.Date != now.Date)
            return null;

        if (!evt.IsAllDay && evt.Start.Value < now)
            return null;

        return new EventModel
        {
            Title = evt.Summary,
            Location = evt.Location ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = evt.Start.Value,
            End = evt.End?.Value
        };
    }

    private static EventModel? GetOccurrenceEvent(CalendarEvent evt, DateTime now)
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
            Title = evt.Summary,
            Location = evt.Location ?? string.Empty,
            IsAllDay = evt.IsAllDay,
            Start = occurrence.Period.StartTime.Value,
            End = occurrence.Period.EndTime?.Value
        };
    }
}