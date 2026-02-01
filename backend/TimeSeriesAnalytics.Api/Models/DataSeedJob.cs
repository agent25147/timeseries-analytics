namespace TimeSeriesAnalytics.Api.Models;

public class DataSeedJob
{
    public string JobId { get; set; } = string.Empty;
    public DataSeedJobStatus Status { get; set; }
    public long TotalRecords { get; set; }
    public long ProcessedRecords { get; set; }
    public int PercentComplete { get; set; }
    public int BatchSize { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public double? RecordsPerSecond { get; set; }
    public string? EstimatedTimeRemaining { get; set; }
}

public enum DataSeedJobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class StartSeedJobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StatusUrl { get; set; } = string.Empty;
}

public class SeedJobRequest
{
    public long RecordCount { get; set; }
    public int BatchSize { get; set; } = 10000;
}
