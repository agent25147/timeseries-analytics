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
    public async Task<ActionResult<AgGridResponse<TimeSeriesRecord>>> Query([FromBody] AgGridRequest request)
    {
        try
        {
            _logger.LogInformation("Processing AG-Grid query request");

            // Build and execute count query
            var countQuery = _queryBuilderService.BuildCountQuery(request);
            var totalCount = await _clickHouseService.ExecuteScalarAsync<long>(
                countQuery.Sql,
                countQuery.Parameters);

            // Build and execute data query
            var dataQuery = _queryBuilderService.BuildQuery(request);
            var data = await _clickHouseService.QueryAsync<TimeSeriesRecord>(
                dataQuery.Sql,
                dataQuery.Parameters);

            var response = new AgGridResponse<TimeSeriesRecord>
            {
                Data = data.ToList(),
                LastRow = (int)totalCount
            };

            _logger.LogInformation("Returning {Count} records out of {Total}",
                response.Data.Count, totalCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
