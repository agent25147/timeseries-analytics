using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using TimeSeriesAnalytics.Api.Models;

namespace TimeSeriesAnalytics.Api.Services;

public interface IDataSeedJobService
{
    string StartJob(long recordCount, int batchSize);
    DataSeedJob? GetJobStatus(string jobId);
    IEnumerable<DataSeedJob> GetAllJobs();
    bool CancelJob(string jobId);
}

public class DataSeedJobService : BackgroundService, IDataSeedJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeedJobService> _logger;
    private readonly ConcurrentDictionary<string, DataSeedJob> _jobs = new();
    private readonly Channel<DataSeedJob> _jobChannel;

    public DataSeedJobService(
        IServiceProvider serviceProvider,
        ILogger<DataSeedJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Create unbounded channel for job queue
        // SingleReader = true because we have one background worker
        // SingleWriter = false because multiple API requests can add jobs
        _jobChannel = Channel.CreateUnbounded<DataSeedJob>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    }

    public string StartJob(long recordCount, int batchSize)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var job = new DataSeedJob
        {
            JobId = jobId,
            Status = DataSeedJobStatus.Queued,
            TotalRecords = recordCount,
            ProcessedRecords = 0,
            PercentComplete = 0,
            BatchSize = batchSize,
            StartTime = DateTime.UtcNow
        };

        _jobs[jobId] = job;
        
        // Write to channel - this will wake up the reader immediately
        _jobChannel.Writer.TryWrite(job);

        _logger.LogInformation("Created job {JobId} for {Records} records with batch size {BatchSize}", 
            jobId, recordCount, batchSize);

        return jobId;
    }

    public DataSeedJob? GetJobStatus(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public IEnumerable<DataSeedJob> GetAllJobs()
    {
        return _jobs.Values.OrderByDescending(j => j.StartTime);
    }

    public bool CancelJob(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            if (job.Status == DataSeedJobStatus.Queued || job.Status == DataSeedJobStatus.Running)
            {
                job.Status = DataSeedJobStatus.Cancelled;
                job.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Cancelled job {JobId}", jobId);
                return true;
            }
        }
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Seed Job Service started (using Channels - event-driven)");

        // ReadAllAsync waits for jobs to be written to the channel
        // No polling loop! This blocks until a job is available
        // Zero CPU usage when idle, instant wake-up when job arrives
        await foreach (var job in _jobChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job {JobId}", job.JobId);
                job.Status = DataSeedJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.EndTime = DateTime.UtcNow;
            }
        }

        _logger.LogInformation("Data Seed Job Service stopped");
    }

    private async Task ProcessJobAsync(DataSeedJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dataGeneratorService = scope.ServiceProvider.GetRequiredService<IDataGeneratorService>();

        job.Status = DataSeedJobStatus.Running;
        job.StartTime = DateTime.UtcNow;

        _logger.LogInformation("Starting job {JobId} - {Records} records with batch size {BatchSize}", 
            job.JobId, job.TotalRecords, job.BatchSize);

        var stopwatch = Stopwatch.StartNew();
        var batchSize = job.BatchSize;
        var totalBatches = (int)Math.Ceiling((double)job.TotalRecords / batchSize);

        try
        {
            for (int batch = 0; batch < totalBatches; batch++)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested || job.Status == DataSeedJobStatus.Cancelled)
                {
                    _logger.LogInformation("Job {JobId} was cancelled", job.JobId);
                    job.Status = DataSeedJobStatus.Cancelled;
                    job.EndTime = DateTime.UtcNow;
                    return;
                }

                var recordsInBatch = (int)Math.Min(batchSize, job.TotalRecords - job.ProcessedRecords);
                
                // Generate and insert batch
                await dataGeneratorService.GenerateAndInsertDataAsync(recordsInBatch);

                // Update progress
                job.ProcessedRecords += recordsInBatch;
                job.PercentComplete = (int)((double)job.ProcessedRecords / job.TotalRecords * 100);

                // Calculate speed and ETA
                var elapsed = stopwatch.Elapsed.TotalSeconds;
                if (elapsed > 0)
                {
                    job.RecordsPerSecond = job.ProcessedRecords / elapsed;
                    var remainingRecords = job.TotalRecords - job.ProcessedRecords;
                    var estimatedSeconds = remainingRecords / job.RecordsPerSecond.Value;
                    job.EstimatedTimeRemaining = FormatTimeSpan(TimeSpan.FromSeconds(estimatedSeconds));
                }

                _logger.LogInformation(
                    "Job {JobId}: {Processed}/{Total} ({Percent}%) - {Speed:F0} records/sec - Batch size: {BatchSize}",
                    job.JobId,
                    job.ProcessedRecords,
                    job.TotalRecords,
                    job.PercentComplete,
                    job.RecordsPerSecond ?? 0,
                    job.BatchSize);
            }

            stopwatch.Stop();
            job.Status = DataSeedJobStatus.Completed;
            job.EndTime = DateTime.UtcNow;
            job.PercentComplete = 100;

            _logger.LogInformation(
                "Job {JobId} completed - {Records} records in {Time:F2}s ({Speed:F0} records/sec)",
                job.JobId,
                job.ProcessedRecords,
                stopwatch.Elapsed.TotalSeconds,
                job.RecordsPerSecond ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", job.JobId);
            job.Status = DataSeedJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.EndTime = DateTime.UtcNow;
            throw;
        }
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        return $"{timeSpan.Seconds}s";
    }

    public override void Dispose()
    {
        // Complete the channel to signal no more jobs will be added
        _jobChannel.Writer.Complete();
        base.Dispose();
    }
}
