using Dapper;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesAnalytics.Api.Models;
using TimeSeriesAnalytics.Api.Services;

namespace TimeSeriesAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeSeriesController : ControllerBase
{
    private readonly IClickHouseService _clickHouseService;
    private readonly IQueryBuilderService _queryBuilderService;
    private readonly ILogger<TimeSeriesController> _logger;

    public TimeSeriesController(
        IClickHouseService clickHouseService,
        IQueryBuilderService queryBuilderService,
        ILogger<TimeSeriesController> logger)
    {
        _clickHouseService = clickHouseService;
        _queryBuilderService = queryBuilderService;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        try
        {
            // Test database connection
            var result = await _clickHouseService.ExecuteScalarAsync<int>("SELECT 1");
            
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                database = result == 1 ? "connected" : "error"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    [HttpPost("query")]
    public async Task<ActionResult<object>> Query([FromBody] AgGridRequest request)
    {
        try
        {
            _logger.LogInformation("Processing AG-Grid query request");

            if (request.PivotMode && request.PivotCols?.Any() == true)
            {
                return await HandlePivotQuery(request);
            }
            else
            {
                return await HandleRegularQuery(request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<ActionResult<object>> HandleRegularQuery(AgGridRequest request)
    {
        // Build and execute count query
        var countQuery = _queryBuilderService.BuildCountQuery(request);
        var totalCount = await _clickHouseService.ExecuteScalarAsync<long>(
            countQuery.Sql,
            countQuery.Parameters);

        // Build and execute data query
        var dataQuery = _queryBuilderService.BuildQuery(request);
        var data = await _clickHouseService.QueryAsync<TimeSeriesRecordDto>(
            dataQuery.Sql,
            dataQuery.Parameters);

        var response = new AgGridResponse<TimeSeriesRecordDto>
        {
            Data = data.ToList(),
            LastRow = (int)totalCount
        };

        _logger.LogInformation("Returning {Count} records out of {Total}",
            response.Data.Count, totalCount);

        return Ok(response);
    }

    private async Task<ActionResult<object>> HandlePivotQuery(AgGridRequest request)
    {
        // Validate: Pivot mode requires at least one row group
        if (request.RowGroupCols == null || !request.RowGroupCols.Any())
        {
            _logger.LogWarning("Pivot mode requested without row groups. Returning empty result.");
            return Ok(new
            {
                data = new List<object>(),
                lastRow = 0,
                pivotResultFields = new List<string>()
            });
        }

        // Get query for unique pivot column values
        var pivotColumnsQuery = _queryBuilderService.BuildPivotColumnsQuery(request);

        var pivotColumnsResult = await _clickHouseService.QueryAsync<string>(
            pivotColumnsQuery.Sql,
            pivotColumnsQuery.Parameters);

        var pivotColumns = pivotColumnsResult.Where(v => !string.IsNullOrEmpty(v)).ToList();

        // Build pivot query
        var pivotQuery = _queryBuilderService.BuildPivotQuery(request, pivotColumns);

        _logger.LogInformation("PIVOT SQL: {Sql}", pivotQuery.Sql);

        var pivotData = await _clickHouseService.QueryAsync<dynamic>(pivotQuery.Sql);

        var dataList = pivotData.AsList();
        // Count query
        var countQuery = _queryBuilderService.BuildCountQuery(request);
        var totalCount = await _clickHouseService.ExecuteScalarAsync<long>(
            countQuery.Sql,
            countQuery.Parameters);

        // Build the list of all pivot result field names
        var pivotResultFields = new List<string>();
        foreach (var pivotValue in pivotColumns)
        {
            var sanitizedPivotValue = SanitizeColumnName(pivotValue);
            foreach (var valueCol in request.ValueCols ?? new List<string>())
            {
                pivotResultFields.Add($"{sanitizedPivotValue}_{valueCol}");
            }
        }

        var response = new
        {
            data = dataList,
            lastRow = (int)totalCount,
            pivotResultFields = pivotResultFields
        };

        _logger.LogInformation("Returning {Count} pivot records with {Columns} pivot result fields",
            dataList.Count, pivotResultFields.Count);

        return Ok(response);
    }

    private string SanitizeColumnName(string columnName)
    {
        // Same sanitization logic as in QueryBuilderService
        return columnName
            .Replace(" ", "_")
            .Replace("&", "and")
            .Replace("-", "_")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace(".", "_")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("@", "at")
            .Replace("#", "num")
            .Replace("%", "pct")
            .Replace("$", "dollar")
            .Replace("*", "star")
            .Replace("+", "plus")
            .Replace("=", "eq");
    }
}
