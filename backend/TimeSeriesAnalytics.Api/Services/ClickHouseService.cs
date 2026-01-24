using ClickHouse.Client.ADO;
using Dapper;
using Microsoft.Extensions.Options;
using TimeSeriesAnalytics.Api.Models;

namespace TimeSeriesAnalytics.Api.Services;

public interface IClickHouseService
{
    Task<ClickHouseConnection> GetConnectionAsync();
    Task ExecuteNonQueryAsync(string sql, object? parameters = null);
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null);
}
public class ClickHouseService : IClickHouseService
{
    private readonly ClickHouseSettings _settings;
    private readonly ILogger<ClickHouseService> _logger;

    public ClickHouseService(
        IOptions<ClickHouseSettings> settings,
        ILogger<ClickHouseService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ClickHouseConnection> GetConnectionAsync()
    {
        var connectionString = new ClickHouseConnectionStringBuilder
        {
            Host = _settings.Host,
            Port = (ushort)_settings.Port,
            Database = _settings.Database,
            Username = _settings.Username,
            Password = _settings.Password,
            Compression = _settings.UseCompression
        }.ToString();

        var connection = new ClickHouseConnection(connectionString);
        await connection.OpenAsync();

        return connection;
    }

    public async Task ExecuteNonQueryAsync(string sql, object? parameters = null)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await connection.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing non-query: {Sql}", sql);
            throw;
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            return await connection.ExecuteScalarAsync<T>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scalar: {Sql}", sql);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            return await connection.QueryAsync<T>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Sql}", sql);
            throw;
        }
    }
}
