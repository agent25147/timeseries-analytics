namespace TimeSeriesAnalytics.Api.Models;

public class AgGridRequest
{
    public int StartRow { get; set; }
    public int EndRow { get; set; }
    public List<ColumnFilter>? FilterModel { get; set; }
    public List<SortModel>? SortModel { get; set; }
    public List<string>? RowGroupCols { get; set; }
    public List<string>? GroupKeys { get; set; }
    public List<string>? ValueCols { get; set; }
    public List<string>? PivotCols { get; set; }
    public bool PivotMode { get; set; }
}

public class ColumnFilter
{
    public string ColumnName { get; set; } = string.Empty;
    public string FilterType { get; set; } = string.Empty;  // text, number, date
    public string? Type { get; set; }  // equals, contains, greaterThan, etc.
    public string? Filter { get; set; }
    public string? FilterTo { get; set; }  // For range filters
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}

public class SortModel
{
    public string ColId { get; set; } = string.Empty;
    public string Sort { get; set; } = string.Empty;  // asc or desc
}