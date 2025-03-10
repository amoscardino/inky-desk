namespace InkyDesk.Server.Services;

public class WeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("weather");
    private readonly string _stationId = configuration["WeatherStationId"] ?? string.Empty;

    public async Task<(string, string)> GetWeatherAsync()
    {
        var response = await _httpClient.GetAsync($"/stations/{_stationId}/observations/latest");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<Observations>()
            ?? throw new Exception("Failed to parse weather data.");

        var weather = content.Properties;
        var temperature = Math.Round(weather.Temperature.Value.GetValueOrDefault() * 9 / 5 + 32, 0);

        return ($"{temperature}°F", weather.TextDescription);
    }

    private class Observations
    {
        public Properties Properties { get; set; } = new();
    }

    private class Properties
    {
        public ValueWithUnit Elevation { get; set; } = new();
        public string Station { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string RawMessage { get; set; } = string.Empty;
        public string TextDescription { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<CloudLayer> CloudLayers { get; set; } = [];
        public ValueWithUnit Temperature { get; set; } = new();
        public ValueWithUnit Dewpoint { get; set; } = new();
        public ValueWithUnit WindDirection { get; set; } = new();
        public ValueWithUnit WindSpeed { get; set; } = new();
        public ValueWithUnit WindGust { get; set; } = new();
        public ValueWithUnit BarometricPressure { get; set; } = new();
        public ValueWithUnit SeaLevelPressure { get; set; } = new();
        public ValueWithUnit Visibility { get; set; } = new();
        public ValueWithUnit MaxTemperatureLast24Hours { get; set; } = new();
        public ValueWithUnit MinTemperatureLast24Hours { get; set; } = new();
        public ValueWithUnit PrecipitationLastHour { get; set; } = new();
        public ValueWithUnit PrecipitationLast3Hours { get; set; } = new();
        public ValueWithUnit PrecipitationLast6Hours { get; set; } = new();
        public ValueWithUnit RelativeHumidity { get; set; } = new();
        public ValueWithUnit WindChill { get; set; } = new();
        public ValueWithUnit HeatIndex { get; set; } = new();
    }

    private class ValueWithUnit
    {
        public string UnitCode { get; set; } = string.Empty;
        public double? Value { get; set; }
        public char? QualityControl { get; set; }
    }

    private class CloudLayer
    {
        public ValueWithUnit Base { get; set; } = new();
        public string Amount { get; set; } = string.Empty;
    }
}