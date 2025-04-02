# InkyDesk Server

An ASP.NET Core API to provided data and an image to the client.

## Prerequisites

- .NET 8 SDK
- Docker (for deployment only)

## Configuration

There's two config values you should be aware of in appsettings.json:

- `WeatherStationId`: Corresponds to a station ID to use with the [weather.gov API](https://www.weather.gov/documentation/services-web-api). The default value `KLCK` is Rickenbacker airport.
- `ConfigPath` specifies a path to a folder that should contain the `calendars.json` file detailed below.

### `calendars.json`

Calendars to be included need to be defined in a file called `calendars.json`. This file needs to be somewhere and the path to it should be in the `ConfigPath` app setting (or environment variable). The format looks like this:

```json
[
    {
        "name": "Races: F1",
        "url": "https://files-f1.motorsportcalendars.com/f1-calendar_p1_p2_p3_qualifying_sprint_gp.ics",
        "offset": 0,
        "isEnabled": true
    }
]
```

The `name` property is just for keeping the file organized. `url` and `isEnabled` should be pretty self-explanatory. `offset` allows for bumping events some amount of days forward or backwards. My use case is for my city's trash pickup calendar. I want to know about the pickup the day before it happens, so I set the `offset` to 1. Note that the offset is applied to the current date, not the event.

### `replacements.json`

Titles can be modified via rules to make them shorter (and thus fit better on the small screen). These replacements are done via the `replacements.json` file which should be in the same folder as `calendars.json`. The format looks like this:

```json
[
    {
        "name": "Example",
        "find": "Example: ",
        "replace": "Ex: ",
        "isEnabled": true
    }
]
```

## Running via Docker

I publish the server app as a Docker container and it can be run like so:

`docker run amoscardino/inkydesk-server -p 5229:5161`

You will probably want to set some environment variables. You can use `TZ` to set your time zone (`America/Detroit` is the default). The `ConfigPath` setting defaults to `/etc/inkydesk` when running in Docker, so using a volume to map it somewhere will let you set your own `calendars.json` file. You can also set `WeatherStationId` as an environment variable.
