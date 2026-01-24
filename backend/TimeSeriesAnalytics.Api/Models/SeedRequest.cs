namespace TimeSeriesAnalytics.Api.Models;

public class SeedRequest
{
    public int RecordCount { get; set; } = 100;
}

public class SeedResponse
{
    public bool Success { get; set; }
    public int RecordsGenerated { get; set; }
    public string TimeTaken { get; set; } = string.Empty;
    public string? Message { get; set; }
}