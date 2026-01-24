namespace TimeSeriesAnalytics.Api.Models;

public class QueryResult
{
    public string Sql { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}