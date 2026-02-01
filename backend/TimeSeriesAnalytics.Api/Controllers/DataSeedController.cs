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
    private readonly IDataSeedJobService _jobService;
    private readonly ILogger<DataSeedController> _logger;

    public DataSeedController(
        IDataGeneratorService dataGeneratorService,
        IClickHouseService clickHouseService,
        IDataSeedJobService jobService,
        ILogger<DataSeedController> logger)
    {
        _dataGeneratorService = dataGeneratorService;
        _clickHouseService = clickHouseService;
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// Start a background job to generate data. Returns immediately with a job ID.
    /// </summary>
    [HttpPost("start")]
    public ActionResult<StartSeedJobResponse> StartDataGeneration([FromBody] SeedJobRequest request)
    {
        if (request.RecordCount <= 0 || request.RecordCount > 1_000_000_000)
        {
            return BadRequest(new
            {
                success = false,
                message = "Record count must be between 1 and 1,000,000,000"
            });
        }

        var jobId = _jobService.StartJob(request.RecordCount, request.BatchSize);

        var response = new StartSeedJobResponse
        {
            JobId = jobId,
            Message = $"Job started to generate {request.RecordCount:N0} records",
            StatusUrl = $"/api/dataseed/status/{jobId}"
        };

        return Ok(response);
    }

    /// <summary>
    /// Get the status of a specific job
    /// </summary>
    [HttpGet("status/{jobId}")]
    public ActionResult<DataSeedJob> GetJobStatus(string jobId)
    {
        var job = _jobService.GetJobStatus(jobId);

        if (job == null)
        {
            return NotFound(new { message = $"Job {jobId} not found" });
        }

        return Ok(job);
    }

    /// <summary>
    /// Get all jobs (recent first)
    /// </summary>
    [HttpGet("jobs")]
    public ActionResult<IEnumerable<DataSeedJob>> GetAllJobs()
    {
        var jobs = _jobService.GetAllJobs();
        return Ok(jobs);
    }

    /// <summary>
    /// Cancel a running or queued job
    /// </summary>
    [HttpPost("cancel/{jobId}")]
    public ActionResult CancelJob(string jobId)
    {
        var cancelled = _jobService.CancelJob(jobId);

        if (!cancelled)
        {
            return NotFound(new { message = $"Job {jobId} not found or cannot be cancelled" });
        }

        return Ok(new { message = $"Job {jobId} cancelled", success = true });
    }

    /// <summary>
    /// Legacy synchronous endpoint - use /start for better experience
    /// </summary>
    [HttpPost("generate")]
    [Obsolete("Use /start endpoint for background processing")]
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
