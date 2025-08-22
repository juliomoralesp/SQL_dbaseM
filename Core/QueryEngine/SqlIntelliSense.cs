using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SqlServerManager.Services;

namespace SqlServerManager.Core.QueryEngine
{
    /// <summary>
    /// Provides IntelliSense capabilities for SQL editor including auto-completion and syntax assistance
    /// </summary>
    public class SqlIntelliSense
    {
        private readonly ConnectionService _connectionService;
        private List<string> _sqlKeywords;
        private List<string> _sqlFunctions;
        private Dictionary<string, List<string>> _databaseSchemas;
        private Dictionary<string, Dictionary<string, List<ColumnInfo>>> _tableColumns;
        private DateTime _lastSchemaRefresh;
        private const int SCHEMA_CACHE_MINUTES = 5;

        public SqlIntelliSense(ConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _databaseSchemas = new Dictionary<string, List<string>>();
            _tableColumns = new Dictionary<string, Dictionary<string, List<ColumnInfo>>>();
            InitializeStaticLists();
        }

        public event EventHandler<SchemaRefreshEventArgs> SchemaRefreshed;

        /// <summary>
        /// Initialize SQL keywords and functions
        /// </summary>
        private void InitializeStaticLists()
        {
            _sqlKeywords = new List<string>
            {
                // Basic SQL keywords
                "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER",
                "TABLE", "DATABASE", "INDEX", "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER",
                
                // Clauses and operators
                "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS", "NULL",
                "ORDER", "BY", "GROUP", "HAVING", "DISTINCT", "TOP", "LIMIT",
                "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "JOIN", "ON", "CROSS",
                
                // Data types
                "VARCHAR", "CHAR", "TEXT", "INT", "BIGINT", "SMALLINT", "TINYINT",
                "DECIMAL", "NUMERIC", "FLOAT", "REAL", "MONEY", "SMALLMONEY",
                "DATETIME", "DATETIME2", "DATE", "TIME", "TIMESTAMP",
                "BIT", "BINARY", "VARBINARY", "IMAGE", "UNIQUEIDENTIFIER",
                
                // Common constraints
                "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "UNIQUE", "CHECK",
                "DEFAULT", "IDENTITY", "AUTO_INCREMENT",
                
                // Transaction keywords
                "BEGIN", "COMMIT", "ROLLBACK", "TRANSACTION", "SAVEPOINT",
                
                // Conditional
                "CASE", "WHEN", "THEN", "ELSE", "END", "IF", "WHILE", "FOR",
                
                // Set operations
                "UNION", "INTERSECT", "EXCEPT", "ALL",
                
                // Common SQL Server specific
                "WITH", "CTE", "OVER", "PARTITION", "ROW_NUMBER", "RANK", "DENSE_RANK",
                "CAST", "CONVERT", "TRY_CAST", "TRY_CONVERT", "ISNULL", "COALESCE"
            };

            _sqlFunctions = new List<string>
            {
                // Aggregate functions
                "COUNT", "SUM", "AVG", "MIN", "MAX", "STDEV", "VAR",
                
                // String functions
                "LEN", "LENGTH", "LEFT", "RIGHT", "SUBSTRING", "CHARINDEX", "PATINDEX",
                "REPLACE", "REVERSE", "UPPER", "LOWER", "LTRIM", "RTRIM", "TRIM",
                "CONCAT", "CONCAT_WS", "STRING_AGG", "STUFF", "REPLICATE",
                
                // Date functions
                "GETDATE", "GETUTCDATE", "SYSDATETIME", "CURRENT_TIMESTAMP",
                "DATEADD", "DATEDIFF", "DATENAME", "DATEPART", "YEAR", "MONTH", "DAY",
                "EOMONTH", "DATEFROMPARTS", "TIMEFROMPARTS",
                
                // Mathematical functions
                "ABS", "CEILING", "FLOOR", "ROUND", "SQRT", "POWER", "EXP", "LOG",
                "PI", "RAND", "SIGN", "SIN", "COS", "TAN", "ASIN", "ACOS", "ATAN",
                
                // Conversion functions
                "CAST", "CONVERT", "PARSE", "TRY_PARSE", "FORMAT",
                
                // System functions
                "@@VERSION", "@@SERVERNAME", "@@IDENTITY", "@@ROWCOUNT",
                "USER_NAME", "SYSTEM_USER", "SESSION_USER", "HOST_NAME",
                "DB_NAME", "DB_ID", "OBJECT_NAME", "OBJECT_ID",
                
                // Window functions
                "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE", "LAG", "LEAD",
                "FIRST_VALUE", "LAST_VALUE",
                
                // Conditional functions
                "ISNULL", "NULLIF", "COALESCE", "IIF", "CHOOSE"
            };
        }

        /// <summary>
        /// Get auto-completion suggestions based on current context
        /// </summary>
        public async Task<List<CompletionItem>> GetCompletionsAsync(string text, int position, string currentDatabase = null)
        {
            var completions = new List<CompletionItem>();

            try
            {
                // Refresh schema if needed
                await RefreshSchemaIfNeeded(currentDatabase);

                // Analyze context around cursor position
                var context = AnalyzeContext(text, position);
                
                switch (context.Type)
                {
                    case CompletionContextType.Keyword:
                        completions.AddRange(GetKeywordCompletions(context.PartialText));
                        break;
                        
                    case CompletionContextType.TableName:
                        completions.AddRange(GetTableCompletions(context.PartialText, currentDatabase));
                        break;
                        
                    case CompletionContextType.ColumnName:
                        completions.AddRange(GetColumnCompletions(context.PartialText, context.TableName, currentDatabase));
                        break;
                        
                    case CompletionContextType.DatabaseName:
                        completions.AddRange(await GetDatabaseCompletions(context.PartialText));
                        break;
                        
                    case CompletionContextType.Function:
                        completions.AddRange(GetFunctionCompletions(context.PartialText));
                        break;
                        
                    case CompletionContextType.General:
                    default:
                        // Provide all types of completions
                        completions.AddRange(GetKeywordCompletions(context.PartialText));
                        completions.AddRange(GetFunctionCompletions(context.PartialText));
                        completions.AddRange(GetTableCompletions(context.PartialText, currentDatabase));
                        break;
                }

                // Sort by relevance and alphabetically
                completions = completions
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Text)
                    .ToList();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error getting SQL completions");
            }

            return completions;
        }

        /// <summary>
        /// Analyze the context around the cursor position
        /// </summary>
        private CompletionContext AnalyzeContext(string text, int position)
        {
            if (position > text.Length) position = text.Length;
            if (position < 0) position = 0;

            // Get text before cursor
            var textBeforeCursor = position > 0 ? text.Substring(0, position) : "";
            
            // Find the current word being typed
            var wordStart = position;
            while (wordStart > 0 && char.IsLetterOrDigit(text[wordStart - 1]))
            {
                wordStart--;
            }
            
            var partialText = wordStart < position ? text.Substring(wordStart, position - wordStart) : "";
            
            // Analyze context based on previous keywords
            var words = textBeforeCursor.Split(new[] { ' ', '\t', '\n', '\r', '(', ')', ',', ';' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            var context = new CompletionContext
            {
                PartialText = partialText,
                Type = CompletionContextType.General
            };

            if (words.Length > 0)
            {
                var lastWords = words.TakeLast(3).Select(w => w.ToUpper()).ToArray();
                
                // Determine context type
                if (lastWords.Contains("FROM") || lastWords.Contains("JOIN") || lastWords.Contains("INTO"))
                {
                    context.Type = CompletionContextType.TableName;
                }
                else if (lastWords.Contains("SELECT") || lastWords.Contains("WHERE") || lastWords.Contains("ORDER") || lastWords.Contains("GROUP"))
                {
                    context.Type = CompletionContextType.ColumnName;
                    
                    // Try to find table name from FROM clause
                    context.TableName = ExtractTableNameFromQuery(textBeforeCursor);
                }
                else if (lastWords.Contains("USE") || lastWords.Any(w => w.EndsWith(".dbo")))
                {
                    context.Type = CompletionContextType.DatabaseName;
                }
                else if (partialText.Length > 0 && (char.IsLetter(partialText[0]) || partialText[0] == '@'))
                {
                    context.Type = CompletionContextType.Keyword;
                }
            }

            return context;
        }

        /// <summary>
        /// Extract table name from FROM clause for column suggestions
        /// </summary>
        private string ExtractTableNameFromQuery(string text)
        {
            try
            {
                // Simple pattern matching for FROM clause
                var fromMatch = System.Text.RegularExpressions.Regex.Match(
                    text.ToUpper(), @"FROM\s+(\w+)", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (fromMatch.Success && fromMatch.Groups.Count > 1)
                {
                    return fromMatch.Groups[1].Value;
                }
            }
            catch (Exception)
            {
                // Ignore regex errors
            }
            
            return null;
        }

        /// <summary>
        /// Get keyword completions
        /// </summary>
        private List<CompletionItem> GetKeywordCompletions(string partialText)
        {
            var filter = partialText.ToUpper();
            return _sqlKeywords
                .Where(k => k.StartsWith(filter))
                .Select(k => new CompletionItem
                {
                    Text = k,
                    Type = CompletionItemType.Keyword,
                    Description = $"SQL keyword: {k}"
                })
                .ToList();
        }

        /// <summary>
        /// Get function completions
        /// </summary>
        private List<CompletionItem> GetFunctionCompletions(string partialText)
        {
            var filter = partialText.ToUpper();
            return _sqlFunctions
                .Where(f => f.StartsWith(filter))
                .Select(f => new CompletionItem
                {
                    Text = f + "()",
                    Type = CompletionItemType.Function,
                    Description = $"SQL function: {f}"
                })
                .ToList();
        }

        /// <summary>
        /// Get table name completions
        /// </summary>
        private List<CompletionItem> GetTableCompletions(string partialText, string databaseName)
        {
            var completions = new List<CompletionItem>();
            
            if (!_connectionService.IsConnected || string.IsNullOrEmpty(databaseName))
                return completions;

            try
            {
                if (_databaseSchemas.TryGetValue(databaseName, out var tables))
                {
                    var filter = partialText.ToUpper();
                    completions.AddRange(tables
                        .Where(t => t.ToUpper().StartsWith(filter))
                        .Select(t => new CompletionItem
                        {
                            Text = t,
                            Type = CompletionItemType.Table,
                            Description = $"Table: {t}"
                        }));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error getting table completions");
            }

            return completions;
        }

        /// <summary>
        /// Get column name completions
        /// </summary>
        private List<CompletionItem> GetColumnCompletions(string partialText, string tableName, string databaseName)
        {
            var completions = new List<CompletionItem>();
            
            if (!_connectionService.IsConnected || string.IsNullOrEmpty(databaseName) || string.IsNullOrEmpty(tableName))
                return completions;

            try
            {
                var cacheKey = $"{databaseName}.{tableName}";
                if (_tableColumns.TryGetValue(databaseName, out var dbTables) && 
                    dbTables.TryGetValue(tableName, out var columns))
                {
                    var filter = partialText.ToUpper();
                    completions.AddRange(columns
                        .Where(c => c.ColumnName.ToUpper().StartsWith(filter))
                        .Select(c => new CompletionItem
                        {
                            Text = c.ColumnName,
                            Type = CompletionItemType.Column,
                            Description = $"Column: {c.ColumnName} ({c.DataType})"
                        }));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error getting column completions");
            }

            return completions;
        }

        /// <summary>
        /// Get database name completions
        /// </summary>
        private async Task<List<CompletionItem>> GetDatabaseCompletions(string partialText)
        {
            var completions = new List<CompletionItem>();
            
            if (!_connectionService.IsConnected)
                return completions;

            try
            {
                var databases = await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    const string query = "SELECT name FROM sys.databases WHERE state = 0 ORDER BY name";
                    var dbNames = new List<string>();
                    
                    using var command = new SqlCommand(query, conn);
                    using var reader = await command.ExecuteReaderAsync(ct);
                    
                    while (await reader.ReadAsync(ct))
                    {
                        dbNames.Add(reader.GetString(0));
                    }
                    
                    return dbNames;
                });

                var filter = partialText.ToUpper();
                completions.AddRange(databases
                    .Where(db => db.ToUpper().StartsWith(filter))
                    .Select(db => new CompletionItem
                    {
                        Text = db,
                        Type = CompletionItemType.Database,
                        Description = $"Database: {db}"
                    }));
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error getting database completions");
            }

            return completions;
        }

        /// <summary>
        /// Refresh schema cache if needed
        /// </summary>
        private async Task RefreshSchemaIfNeeded(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName) || !_connectionService.IsConnected)
                return;

            var shouldRefresh = DateTime.Now.Subtract(_lastSchemaRefresh).TotalMinutes > SCHEMA_CACHE_MINUTES ||
                               !_databaseSchemas.ContainsKey(databaseName);

            if (shouldRefresh)
            {
                await RefreshSchemaCache(databaseName);
            }
        }

        /// <summary>
        /// Refresh schema cache for the specified database
        /// </summary>
        public async Task RefreshSchemaCache(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName) || !_connectionService.IsConnected)
                return;

            try
            {
                // Get tables and views
                var tables = await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    var query = $@"
                        USE [{databaseName}];
                        SELECT TABLE_NAME 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_TYPE IN ('BASE TABLE', 'VIEW')
                        ORDER BY TABLE_NAME";
                    
                    var tableNames = new List<string>();
                    using var command = new SqlCommand(query, conn);
                    using var reader = await command.ExecuteReaderAsync(ct);
                    
                    while (await reader.ReadAsync(ct))
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                    
                    return tableNames;
                });

                _databaseSchemas[databaseName] = tables;

                // Get columns for each table
                if (!_tableColumns.ContainsKey(databaseName))
                {
                    _tableColumns[databaseName] = new Dictionary<string, List<ColumnInfo>>();
                }

                var dbColumns = _tableColumns[databaseName];
                
                foreach (var tableName in tables.Take(20)) // Limit to first 20 tables for performance
                {
                    var columns = await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                    {
                        var query = $@"
                            USE [{databaseName}];
                            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_NAME = @TableName
                            ORDER BY ORDINAL_POSITION";
                        
                        var columnInfos = new List<ColumnInfo>();
                        using var command = new SqlCommand(query, conn);
                        command.Parameters.AddWithValue("@TableName", tableName);
                        using var reader = await command.ExecuteReaderAsync(ct);
                        
                        while (await reader.ReadAsync(ct))
                        {
                            columnInfos.Add(new ColumnInfo
                            {
                                ColumnName = reader.GetString(0),
                                DataType = reader.GetString(1),
                                IsNullable = reader.GetString(2) == "YES",
                                DefaultValue = reader.IsDBNull(3) ? null : reader.GetString(3)
                            });
                        }
                        
                        return columnInfos;
                    });

                    dbColumns[tableName] = columns;
                }

                _lastSchemaRefresh = DateTime.Now;
                SchemaRefreshed?.Invoke(this, new SchemaRefreshEventArgs(databaseName, tables.Count));
                
                LoggingService.LogInformation("Schema cache refreshed for database: {DatabaseName}, Tables: {TableCount}", 
                    databaseName, tables.Count);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error refreshing schema cache for database: {DatabaseName}", databaseName);
            }
        }

        /// <summary>
        /// Clear schema cache
        /// </summary>
        public void ClearCache()
        {
            _databaseSchemas.Clear();
            _tableColumns.Clear();
            _lastSchemaRefresh = DateTime.MinValue;
        }
    }

    #region Supporting Classes

    public class CompletionItem
    {
        public string Text { get; set; }
        public CompletionItemType Type { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
    }

    public enum CompletionItemType
    {
        Keyword,
        Function,
        Table,
        Column,
        Database,
        Variable
    }

    public class CompletionContext
    {
        public string PartialText { get; set; }
        public CompletionContextType Type { get; set; }
        public string TableName { get; set; }
    }

    public enum CompletionContextType
    {
        General,
        Keyword,
        TableName,
        ColumnName,
        DatabaseName,
        Function
    }

    public class ColumnInfo
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; }
    }

    public class SchemaRefreshEventArgs : EventArgs
    {
        public string DatabaseName { get; }
        public int TableCount { get; }

        public SchemaRefreshEventArgs(string databaseName, int tableCount)
        {
            DatabaseName = databaseName;
            TableCount = tableCount;
        }
    }

    #endregion
}
