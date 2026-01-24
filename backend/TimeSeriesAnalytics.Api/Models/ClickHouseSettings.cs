namespace TimeSeriesAnalytics.Api.Models;

public class ClickHouseSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 9000;
    public string Database { get; set; } = "analytics";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseCompression { get; set; } = true;
}