using System.Text;
using TimeSeriesAnalytics.Api.Models;

namespace TimeSeriesAnalytics.Api.Services;

public interface IQueryBuilderService
{
    QueryResult BuildQuery(AgGridRequest request);
    QueryResult BuildCountQuery(AgGridRequest request);
}
public class QueryBuilderService: IQueryBuilderService
{
    private readonly ILogger<QueryBuilderService> _logger;

    // Map AG-Grid column names to database column names
    private static readonly Dictionary<string, string> ColumnMap = new()
        {
            { "id", "id" },
            { "transactionId", "transaction_id" },
            { "timestamp", "timestamp" },
            { "userId", "user_id" },
            { "region", "region" },
            { "category", "category" },
            { "productName", "product_name" },
            { "amount", "amount" },
            { "quantity", "quantity" },
            { "status", "status" },
            { "paymentMethod", "payment_method" },
            { "address", "address" },
            { "shippingLane", "shipping_lane" },
            { "scacCode", "scac_code" },
            { "billToName", "bill_to_name" },
            { "invoiceNumber", "invoice_number" },
            { "currencyCode", "currency_code" },
            { "weight", "weight" }
        };

    public QueryBuilderService(ILogger<QueryBuilderService> logger)
    {
        _logger = logger;
    }

    public QueryResult BuildQuery(AgGridRequest request)
    {
        var sql = new StringBuilder();
        var parameters = new Dictionary<string, object>();

        // SELECT clause
        sql.Append("SELECT ");

        if (request.RowGroupCols?.Any() == true)
        {
            // Grouping query
            BuildGroupingSelect(sql, request);
        }
        else
        {
            // Regular query - select all columns
            sql.Append("* ");
        }

        // FROM clause
        sql.Append("FROM analytics.timeseries_data ");

        // WHERE clause - combine filters AND group keys
        var hasWhere = false;

        // Add group key filters for drill-down
        if (request.GroupKeys?.Any() == true && request.RowGroupCols?.Any() == true)
        {
            sql.Append("WHERE ");
            hasWhere = true;

            for (int i = 0; i < request.GroupKeys.Count; i++)
            {
                var groupCol = request.RowGroupCols[i];
                var groupKey = request.GroupKeys[i];
                var dbColumn = GetDbColumnName(groupCol);

                sql.Append($"{dbColumn} = '{groupKey}' ");

                if (i < request.GroupKeys.Count - 1)
                {
                    sql.Append("AND ");
                }
            }
        }

        // Add regular filters
        if (request.FilterModel?.Any() == true)
        {
            if (hasWhere)
            {
                sql.Append("AND ");
            }
            else
            {
                sql.Append("WHERE ");
                hasWhere = true;
            }

            BuildFilterClauses(sql, request.FilterModel, parameters);
        }

        // GROUP BY clause
        if (request.RowGroupCols?.Any() == true)
        {
            BuildGroupByClause(sql, request);
        }

        // ORDER BY clause
        if (request.SortModel?.Any() == true)
        {
            BuildOrderByClause(sql, request.SortModel);
        }
        else if (request.RowGroupCols?.Any() == true)
        {
            // Default sort for grouped data
            var groupLevel = request.GroupKeys?.Count ?? 0;
            var currentGroupCol = request.RowGroupCols[groupLevel];
            var dbColumn = GetDbColumnName(currentGroupCol);
            sql.Append($"ORDER BY {dbColumn} ASC ");
        }
        else
        {
            sql.Append("ORDER BY timestamp DESC ");
        }

        // LIMIT and OFFSET
        var limit = request.EndRow - request.StartRow;
        sql.Append($"LIMIT {limit} OFFSET {request.StartRow}");

        var finalSql = sql.ToString();
        _logger.LogInformation("Generated SQL: {Sql}", finalSql);

        return new QueryResult
        {
            Sql = finalSql,
            Parameters = parameters
        };
    }

    public QueryResult BuildCountQuery(AgGridRequest request)
    {
        var sql = new StringBuilder();
        var parameters = new Dictionary<string, object>();

        sql.Append("SELECT COUNT(*) FROM analytics.timeseries_data ");

        var hasWhere = false;

        // Add group key filters
        if (request.GroupKeys?.Any() == true && request.RowGroupCols?.Any() == true)
        {
            sql.Append("WHERE ");
            hasWhere = true;

            for (int i = 0; i < request.GroupKeys.Count; i++)
            {
                var groupCol = request.RowGroupCols[i];
                var groupKey = request.GroupKeys[i];
                var dbColumn = GetDbColumnName(groupCol);

                sql.Append($"{dbColumn} = '{groupKey}' ");

                if (i < request.GroupKeys.Count - 1)
                {
                    sql.Append("AND ");
                }
            }
        }

        // Add regular filters
        if (request.FilterModel?.Any() == true)
        {
            if (hasWhere)
            {
                sql.Append("AND ");
            }
            else
            {
                sql.Append("WHERE ");
            }

            BuildFilterClauses(sql, request.FilterModel, parameters);
        }

        return new QueryResult
        {
            Sql = sql.ToString(),
            Parameters = parameters
        };
    }

    private void BuildGroupingSelect(StringBuilder sql, AgGridRequest request)
    {
        var groupLevel = request.GroupKeys?.Count ?? 0;
        var currentGroupCol = request.RowGroupCols![groupLevel];
        var dbColumn = GetDbColumnName(currentGroupCol);

        sql.Append($"{dbColumn} AS {currentGroupCol}, ");

        // Add aggregations for value columns
        if (request.ValueCols?.Any() == true)
        {
            foreach (var valueCol in request.ValueCols)
            {
                var dbValueCol = GetDbColumnName(valueCol);
                sql.Append($"SUM({dbValueCol}) AS {valueCol}_sum, ");
                sql.Append($"AVG({dbValueCol}) AS {valueCol}_avg, ");
                sql.Append($"COUNT(*) AS {valueCol}_count, ");
            }
        }
        else
        {
            sql.Append("COUNT(*) AS count ");
        }

        // Remove trailing comma
        if (sql[sql.Length - 2] == ',')
        {
            sql.Length -= 2;
            sql.Append(' ');
        }
    }

    private void BuildFilterClauses(StringBuilder sql, List<ColumnFilter> filters, Dictionary<string, object> parameters)
    {
        sql.Append("WHERE ");
        var filterClauses = new List<string>();

        foreach (var filter in filters)
        {
            var dbColumn = GetDbColumnName(filter.ColumnName);
            var paramName = $"param_{filter.ColumnName}";

            switch (filter.FilterType?.ToLower())
            {
                case "text":
                    if (filter.Type == "contains" && !string.IsNullOrEmpty(filter.Filter))
                    {
                        filterClauses.Add($"{dbColumn} ILIKE '%{filter.Filter}%'");
                    }
                    else if (filter.Type == "equals" && !string.IsNullOrEmpty(filter.Filter))
                    {
                        filterClauses.Add($"{dbColumn} = '{filter.Filter}'");
                    }
                    break;

                case "number":
                    if (filter.Type == "equals" && !string.IsNullOrEmpty(filter.Filter))
                    {
                        filterClauses.Add($"{dbColumn} = {filter.Filter}");
                    }
                    else if (filter.Type == "greaterThan" && !string.IsNullOrEmpty(filter.Filter))
                    {
                        filterClauses.Add($"{dbColumn} > {filter.Filter}");
                    }
                    else if (filter.Type == "lessThan" && !string.IsNullOrEmpty(filter.Filter))
                    {
                        filterClauses.Add($"{dbColumn} < {filter.Filter}");
                    }
                    break;

                case "date":
                    if (!string.IsNullOrEmpty(filter.DateFrom))
                    {
                        filterClauses.Add($"{dbColumn} >= '{filter.DateFrom}'");
                    }
                    if (!string.IsNullOrEmpty(filter.DateTo))
                    {
                        filterClauses.Add($"{dbColumn} <= '{filter.DateTo}'");
                    }
                    break;
            }
        }

        sql.Append(string.Join(" AND ", filterClauses));
        sql.Append(' ');
    }

    private void BuildGroupByClause(StringBuilder sql, AgGridRequest request)
    {
        var groupLevel = request.GroupKeys?.Count ?? 0;
        var currentGroupCol = request.RowGroupCols![groupLevel];
        var dbColumn = GetDbColumnName(currentGroupCol);

        sql.Append($"GROUP BY {dbColumn} ");
    }

    private void BuildOrderByClause(StringBuilder sql, List<SortModel> sortModels)
    {
        sql.Append("ORDER BY ");
        var sortClauses = sortModels.Select(s =>
        {
            var dbColumn = GetDbColumnName(s.ColId);
            var direction = s.Sort.ToUpper();
            return $"{dbColumn} {direction}";
        });

        sql.Append(string.Join(", ", sortClauses));
        sql.Append(' ');
    }

    private string GetDbColumnName(string agGridColumnName)
    {
        return ColumnMap.TryGetValue(agGridColumnName, out var dbName) ? dbName : agGridColumnName;
    }
}
