using TimeSeriesAnalytics.Api.Models;
using Bogus;
using Dapper;

namespace TimeSeriesAnalytics.Api.Services;

public interface IDataGeneratorService
{
    Task<int> GenerateAndInsertDataAsync(int recordCount);
    IEnumerable<TimeSeriesRecord> GenerateRecords(int count);
}

public class DataGeneratorService : IDataGeneratorService
{
    private readonly IClickHouseService _clickHouseService;
    private readonly ILogger<DataGeneratorService> _logger;

    // Predefined sets for realistic data
    private static readonly string[] Regions = { "US-East", "US-West", "EU-West", "EU-Central", "Asia-Pacific", "South-America" };
    private static readonly string[] Categories = { "Electronics", "Clothing", "Food", "Books", "Home & Garden", "Sports", "Toys" };
    private static readonly string[] Statuses = { "Completed", "Pending", "Refunded", "Cancelled" };
    private static readonly string[] PaymentMethods = { "Credit Card", "PayPal", "Bank Transfer", "Cryptocurrency", "Cash on Delivery" };
    private static readonly string[] ShippingLanes = { "Domestic-Ground", "Domestic-Air", "International-Ocean", "International-Air", "Express", "Economy" };
    private static readonly string[] ScacCodes = { "FDEG", "UPSS", "RTWY", "ABFS", "SAIA", "ODFL", "RLCA", "CNWY" };  // Real SCAC codes
    private static readonly string[] CurrencyCodes = { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CNY" };

    public DataGeneratorService(
        IClickHouseService clickHouseService,
        ILogger<DataGeneratorService> logger)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    public IEnumerable<TimeSeriesRecord> GenerateRecords(int count)
    {
        var faker = new Faker<TimeSeriesRecord>()
            .RuleFor(r => r.Id, f => Guid.NewGuid())
            .RuleFor(r => r.TransactionId, f => Guid.NewGuid())
            .RuleFor(r => r.Timestamp, f => f.Date.Between(
                DateTime.UtcNow.AddYears(-2),
                DateTime.UtcNow))
            .RuleFor(r => r.UserId, f => f.Random.Int(1, 10000))
            .RuleFor(r => r.Region, f => f.PickRandom(Regions))
            .RuleFor(r => r.Category, f => f.PickRandom(Categories))
            .RuleFor(r => r.ProductName, f => f.Commerce.ProductName())
            .RuleFor(r => r.Amount, f => f.Finance.Amount(10, 5000))
            .RuleFor(r => r.Quantity, f => f.Random.Int(1, 10))
            .RuleFor(r => r.Status, f => f.PickRandom(Statuses))
            .RuleFor(r => r.PaymentMethod, f => f.PickRandom(PaymentMethods))
            .RuleFor(r => r.Address, f => f.Address.FullAddress())
            .RuleFor(r => r.ShippingLane, f => f.PickRandom(ShippingLanes))
            .RuleFor(r => r.ScacCode, f => f.PickRandom(ScacCodes))
            .RuleFor(r => r.BillToName, f => f.Company.CompanyName())
            .RuleFor(r => r.InvoiceNumber, f => Guid.NewGuid())
            .RuleFor(r => r.CurrencyCode, f => f.PickRandom(CurrencyCodes))
            .RuleFor(r => r.Weight, f => f.Random.Decimal(0.5m, 500m))
            .RuleFor(r => r.CreatedAt, f => DateTime.UtcNow);

        return faker.Generate(count);
    }

    public async Task<int> GenerateAndInsertDataAsync(int recordCount)
    {
        _logger.LogInformation("Generating {Count} records...", recordCount);

        const int batchSize = 10000;
        int totalInserted = 0;

        for (int i = 0; i < recordCount; i += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, recordCount - i);
            var records = GenerateRecords(currentBatchSize);

            await InsertBatchAsync(records);
            totalInserted += currentBatchSize;

            _logger.LogInformation("Inserted {Inserted}/{Total} records", totalInserted, recordCount);
        }

        return totalInserted;
    }

    private async Task InsertBatchAsync(IEnumerable<TimeSeriesRecord> records)
    {
        var sql = @"
                INSERT INTO analytics.timeseries_data 
                (id, transaction_id, timestamp, user_id, region, category, product_name, amount, quantity, 
                 status, payment_method, address, shipping_lane, scac_code, bill_to_name, 
                 invoice_number, currency_code, weight)
                VALUES 
                (@Id, @TransactionId, @Timestamp, @UserId, @Region, @Category, @ProductName, @Amount, @Quantity, 
                 @Status, @PaymentMethod, @Address, @ShippingLane, @ScacCode, @BillToName, 
                 @InvoiceNumber, @CurrencyCode, @Weight)";

        await using var connection = await _clickHouseService.GetConnectionAsync();

        foreach (var record in records)
        {
            await connection.ExecuteAsync(sql, new
            {
                record.Id,
                record.TransactionId,
                record.Timestamp,
                record.UserId,
                record.Region,
                record.Category,
                record.ProductName,
                record.Amount,
                record.Quantity,
                record.Status,
                record.PaymentMethod,
                record.Address,
                record.ShippingLane,
                record.ScacCode,
                record.BillToName,
                record.InvoiceNumber,
                record.CurrencyCode,
                record.Weight
            });
        }
    }
}
