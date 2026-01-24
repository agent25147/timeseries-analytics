namespace TimeSeriesAnalytics.Api.Models;

public class AgGridResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int LastRow { get; set; }
    public List<string>? SecondaryColumns { get; set; }  // For pivot mode
}