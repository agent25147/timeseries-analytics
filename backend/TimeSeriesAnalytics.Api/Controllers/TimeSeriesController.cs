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

        var response = new
        {
            data = dataList,
            lastRow = (int)totalCount,
            secondaryColumns = pivotColumns
        };

        _logger.LogInformation("Returning {Count} pivot records with {Columns} secondary columns",
            dataList.Count, pivotColumns.Count);

        return Ok(response);
    }
}
