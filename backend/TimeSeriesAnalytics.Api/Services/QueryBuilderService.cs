using System.Text;
using TimeSeriesAnalytics.Api.Models;

namespace TimeSeriesAnalytics.Api.Services;

public interface IQueryBuilderService
{
    QueryResult BuildQuery(AgGridRequest request);
    QueryResult BuildCountQuery(AgGridRequest request);
    QueryResult BuildPivotQuery(AgGridRequest request, List<string> pivotValues);
    QueryResult BuildPivotColumnsQuery(AgGridRequest request);
}
public class QueryBuilderService: IQueryBuilderService
{
    private readonly ILogger<QueryBuilderService> _logger;

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

    public QueryResult BuildPivotColumnsQuery(AgGridRequest request)
    {
        if (request.PivotCols == null || !request.PivotCols.Any())
        {
            return new QueryResult
            {
                Sql = "",
                Parameters = new Dictionary<string, object>()
            };
        }

        var pivotCol = request.PivotCols.First(); // For now, support single pivot column
        var dbColumn = GetDbColumnName(pivotCol);

        var sql = new StringBuilder();
        sql.Append($"SELECT DISTINCT {dbColumn} FROM analytics.timeseries_data ");

        // Apply filters if any
        var parameters = new Dictionary<string, object>();
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
                var groupDbColumn = GetDbColumnName(groupCol);

                sql.Append($"{groupDbColumn} = '{groupKey}' ");

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

        sql.Append($"ORDER BY {dbColumn}");

        _logger.LogInformation("Getting pivot values with SQL: {Sql}", sql.ToString());

        return new QueryResult
        {
            Sql = sql.ToString(),
            Parameters = parameters
        };
    }

    public QueryResult BuildPivotQuery(AgGridRequest request, List<string> pivotValues)
    {
        var sql = new StringBuilder();
        var parameters = new Dictionary<string, object>();

        var pivotCol = request.PivotCols!.First();
        var pivotDbCol = GetDbColumnName(pivotCol);
        var rowGroupCols = request.RowGroupCols ?? new List<string>();

        // Gracefully handle missing valueCols - return row groups only
        if (request.ValueCols == null || !request.ValueCols.Any())
        {
            _logger.LogWarning("Pivot mode requested but no ValueCols specified. Returning row groups only.");

            // Just return row group columns without pivot aggregations
            sql.Append("SELECT ");

            if (rowGroupCols.Any())
            {
                foreach (var col in rowGroupCols)
                {
                    var dbCol = GetDbColumnName(col);
                    sql.Append($"{dbCol}, ");
                }

                // Remove trailing comma
                sql.Length -= 2;
                sql.Append(' ');

                sql.Append("FROM analytics.timeseries_data ");

                // Add WHERE clauses if any
                var hasWhere = false;
                if (request.GroupKeys?.Any() == true)
                {
                    sql.Append("WHERE ");
                    hasWhere = true;

                    for (int i = 0; i < request.GroupKeys.Count; i++)
                    {
                        var groupCol = request.RowGroupCols![i];
                        var groupKey = request.GroupKeys[i];
                        var dbColumn = GetDbColumnName(groupCol);

                        sql.Append($"{dbColumn} = '{groupKey}' ");

                        if (i < request.GroupKeys.Count - 1)
                        {
                            sql.Append("AND ");
                        }
                    }
                }

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

                sql.Append($"GROUP BY {string.Join(", ", rowGroupCols.Select(GetDbColumnName))} ");
                sql.Append($"ORDER BY {GetDbColumnName(rowGroupCols.First())} ASC ");

                var limit_i = request.EndRow - request.StartRow;
                sql.Append($"LIMIT {limit_i} OFFSET {request.StartRow}");
            }
            else
            {
                // No row groups either - return empty
                sql.Append("1 WHERE 1=0");
            }

            return new QueryResult
            {
                Sql = sql.ToString(),
                Parameters = parameters
            };
        }

        var valueCols = request.ValueCols;

        // SELECT clause - row group columns
        sql.Append("SELECT ");
        foreach (var col in rowGroupCols)
        {
            var dbCol = GetDbColumnName(col);
            sql.Append($"{dbCol}, ");
        }

        // Add pivoted value columns using sumIf/avgIf/countIf
        foreach (var pivotValue in pivotValues)
        {
            var safePivotValue = pivotValue.Replace("'", "''");
            var sanitizedPivotValue = SanitizeColumnName(pivotValue);

            foreach (var valueCol in valueCols)
            {
                var dbValueCol = GetDbColumnName(valueCol);

                sql.Append($"sumIf({dbValueCol}, {pivotDbCol} = '{safePivotValue}') AS {sanitizedPivotValue}_{valueCol}_sum, ");
                sql.Append($"avgIf({dbValueCol}, {pivotDbCol} = '{safePivotValue}') AS {sanitizedPivotValue}_{valueCol}_avg, ");
                sql.Append($"countIf({pivotDbCol} = '{safePivotValue}') AS {sanitizedPivotValue}_{valueCol}_count, ");
            }
        }

        // Remove trailing comma and space
        sql.Length -= 2;
        sql.Append(' ');

        // FROM clause
        sql.Append("FROM analytics.timeseries_data ");

        // WHERE clause
        var hasWhereClause = false;

        // Add group key filters
        if (request.GroupKeys?.Any() == true && request.RowGroupCols?.Any() == true)
        {
            sql.Append("WHERE ");
            hasWhereClause = true;

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
            if (hasWhereClause)
            {
                sql.Append("AND ");
            }
            else
            {
                sql.Append("WHERE ");
            }

            BuildFilterClauses(sql, request.FilterModel, parameters);
        }

        // GROUP BY clause
        if (rowGroupCols.Any())
        {
            sql.Append("GROUP BY ");
            sql.Append(string.Join(", ", rowGroupCols.Select(GetDbColumnName)));
            sql.Append(' ');
        }

        // ORDER BY clause
        if (request.SortModel?.Any() == true)
        {
            BuildOrderByClause(sql, request.SortModel);
        }
        else if (rowGroupCols.Any())
        {
            sql.Append($"ORDER BY {GetDbColumnName(rowGroupCols.First())} ASC ");
        }

        // LIMIT and OFFSET
        var limit = request.EndRow - request.StartRow;
        sql.Append($"LIMIT {limit} OFFSET {request.StartRow}");

        var finalSql = sql.ToString();
        _logger.LogInformation("Generated Pivot SQL: {Sql}", finalSql);

        return new QueryResult
        {
            Sql = finalSql,
            Parameters = parameters
        };
    }

    private string SanitizeColumnName(string columnName)
    {
        // Replace spaces, ampersands, and special chars with underscores or alternatives
        return columnName
            .Replace(" ", "_")           // Spaces to underscores
            .Replace("&", "and")          // Ampersand to "and"
            .Replace("-", "_")            // Hyphens to underscores
            .Replace("'", "")             // Remove single quotes
            .Replace("\"", "")            // Remove double quotes
            .Replace("(", "")             // Remove parentheses
            .Replace(")", "")
            .Replace("/", "_")            // Slash to underscore
            .Replace("\\", "_")           // Backslash to underscore
            .Replace(".", "_")            // Dot to underscore
            .Replace(",", "")             // Remove commas
            .Replace("!", "")             // Remove exclamation
            .Replace("?", "")             // Remove question mark
            .Replace("@", "at")           // @ to "at"
            .Replace("#", "num")          // # to "num"
            .Replace("%", "pct")          // % to "pct"
            .Replace("$", "dollar")       // $ to "dollar"
            .Replace("*", "star")         // * to "star"
            .Replace("+", "plus")         // + to "plus"
            .Replace("=", "eq");          // = to "eq"
    }
    private string GetDbColumnName(string agGridColumnName)
    {
        return agGridColumnName;
       // return ColumnMap.TryGetValue(agGridColumnName, out var dbName) ? dbName : agGridColumnName;
    }
  
}
