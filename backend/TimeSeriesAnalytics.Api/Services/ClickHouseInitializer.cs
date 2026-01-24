namespace TimeSeriesAnalytics.Api.Services;

public interface IClickHouseInitializer
{
    Task InitializeDatabaseAsync();
}
public class ClickHouseInitializer : IClickHouseInitializer
{
    private readonly IClickHouseService _clickHouseService;
    private readonly ILogger<ClickHouseInitializer> _logger;

    public ClickHouseInitializer(
        IClickHouseService clickHouseService,
        ILogger<ClickHouseInitializer> logger)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Initializing ClickHouse database schema...");

        try
        {
            // Create database if not exists
            await CreateDatabaseAsync();

            // Create table if not exists
            await CreateTableAsync();

            _logger.LogInformation("ClickHouse database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ClickHouse database");
            throw;
        }
    }

    private async Task CreateDatabaseAsync()
    {
        var sql = "CREATE DATABASE IF NOT EXISTS analytics";
        await _clickHouseService.ExecuteNonQueryAsync(sql);
        _logger.LogInformation("Database 'analytics' ensured");
    }

    private async Task CreateTableAsync()
    {
        var sql = @"
        CREATE TABLE IF NOT EXISTS analytics.timeseries_data
        (
            id UUID DEFAULT generateUUIDv4(),
            transaction_id UUID,
            timestamp DateTime,
            user_id UInt32,
            region String,
            category String,
            product_name String,
            amount Decimal(10, 2),
            quantity UInt16,
            status String,
            payment_method String,
            address String,
            shipping_lane String,
            scac_code String,
            bill_to_name String,
            invoice_number UUID,
            currency_code String,
            weight Decimal(10, 2),
            created_at DateTime DEFAULT now()
        )
        ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (timestamp, region, category, user_id)
        SETTINGS index_granularity = 8192";

        await _clickHouseService.ExecuteNonQueryAsync(sql);
        _logger.LogInformation("Table 'timeseries_data' ensured");
    }
}