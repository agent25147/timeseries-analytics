using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TimeSeriesAnalytics.Api.Models;
using TimeSeriesAnalytics.Api.Services;

namespace TimeSeriesAnalytics.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DataSeedController : ControllerBase
{
    private readonly IDataGeneratorService _dataGeneratorService;
    private readonly IClickHouseService _clickHouseService;
    private readonly ILogger<DataSeedController> _logger;

    public DataSeedController(
        IDataGeneratorService dataGeneratorService,
        IClickHouseService clickHouseService,
        ILogger<DataSeedController> logger)
    {
        _dataGeneratorService = dataGeneratorService;
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<SeedResponse>> GenerateData([FromBody] SeedRequest request)
    {
        if (request.RecordCount <= 0 || request.RecordCount > 100_000_000)
        {
            return BadRequest(new SeedResponse
            {
                Success = false,
                Message = "Record count must be between 1 and 100,000,000"
            });
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting data generation for {Count} records", request.RecordCount);

            var recordsGenerated = await _dataGeneratorService.GenerateAndInsertDataAsync(request.RecordCount);

            stopwatch.Stop();

            return Ok(new SeedResponse
            {
                Success = true,
                RecordsGenerated = recordsGenerated,
                TimeTaken = $"{stopwatch.Elapsed.TotalSeconds:F2}s",
                Message = $"Successfully generated {recordsGenerated:N0} records"
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating data");

            return StatusCode(500, new SeedResponse
            {
                Success = false,
                TimeTaken = $"{stopwatch.Elapsed.TotalSeconds:F2}s",
                Message = $"Error: {ex.Message}"
            });
        }
    }

    [HttpGet("count")]
    public async Task<ActionResult<object>> GetRecordCount()
    {
        try
        {
            var count = await _clickHouseService.ExecuteScalarAsync<long>(
                "SELECT count() FROM analytics.timeseries_data");

            return Ok(new { totalRecords = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting record count");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("clear")]
    public async Task<ActionResult<object>> ClearData()
    {
        try
        {
            await _clickHouseService.ExecuteNonQueryAsync(
                "TRUNCATE TABLE analytics.timeseries_data");

            return Ok(new { success = true, message = "All records cleared" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing data");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
