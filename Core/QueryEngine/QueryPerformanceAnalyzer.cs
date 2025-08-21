using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using SqlServerManager.Services;

namespace SqlServerManager.Core.QueryEngine
{
    /// <summary>
    /// Analyzes query performance and provides optimization suggestions
    /// </summary>
    public class QueryPerformanceAnalyzer
    {
        private readonly ConnectionService _connectionService;
        private readonly List<QueryPerformanceMetric> _queryHistory;
        private readonly Dictionary<string, List<ExecutionPlan>> _executionPlanCache;

        public QueryPerformanceAnalyzer(ConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _queryHistory = new List<QueryPerformanceMetric>();
            _executionPlanCache = new Dictionary<string, List<ExecutionPlan>>();
        }

        /// <summary>
        /// Analyze query performance and get recommendations
        /// </summary>
        public async Task<QueryAnalysisResult> AnalyzeQueryAsync(string queryText)
        {
            if (string.IsNullOrWhiteSpace(queryText) || !_connectionService.IsConnected)
                return new QueryAnalysisResult();

            try
            {
                var result = new QueryAnalysisResult
                {
                    QueryText = queryText,
                    AnalyzedAt = DateTime.Now
                };

                // Get execution plan
                result.ExecutionPlan = await GetExecutionPlanAsync(queryText);
                
                // Get query statistics
                result.Statistics = await GetQueryStatisticsAsync(queryText);
                
                // Analyze query complexity
                result.ComplexityMetrics = AnalyzeQueryComplexity(queryText);
                
                // Generate optimization suggestions
                result.OptimizationSuggestions = GenerateOptimizationSuggestions(queryText, result.ExecutionPlan, result.Statistics);
                
                // Store in history
                var metric = new QueryPerformanceMetric
                {
                    QueryText = queryText,
                    ExecutionTime = result.Statistics?.Duration ?? TimeSpan.Zero,
                    LogicalReads = result.Statistics?.LogicalReads ?? 0,
                    PhysicalReads = result.Statistics?.PhysicalReads ?? 0,
                    ExecutedAt = DateTime.Now
                };
                
                _queryHistory.Add(metric);
                
                // Keep only recent history
                if (_queryHistory.Count > 1000)
                {
                    _queryHistory.RemoveRange(0, 100);
                }

                return result;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error analyzing query performance");
                return new QueryAnalysisResult
                {
                    QueryText = queryText,
                    AnalyzedAt = DateTime.Now,
                    HasError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get execution plan for a query
        /// </summary>
        private async Task<ExecutionPlan> GetExecutionPlanAsync(string queryText)
        {
            return await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
            {
                var plan = new ExecutionPlan();
                
                try
                {
                    // Enable execution plan capture
                    using (var cmd = new SqlCommand("SET SHOWPLAN_XML ON", conn))
                    {
                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    // Execute query to get plan
                    using var planCmd = new SqlCommand(queryText, conn);
                    planCmd.CommandTimeout = 30;
                    
                    using var reader = await planCmd.ExecuteReaderAsync(ct);
                    if (reader.Read())
                    {
                        plan.PlanXml = reader.GetString(0);
                        plan.EstimatedCost = ExtractEstimatedCostFromPlan(plan.PlanXml);
                        plan.Operations = ExtractOperationsFromPlan(plan.PlanXml);
                    }

                    // Disable execution plan capture
                    using (var cmd = new SqlCommand("SET SHOWPLAN_XML OFF", conn))
                    {
                        await cmd.ExecuteNonQueryAsync(ct);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning("Could not retrieve execution plan: {Message}", ex.Message);
                    plan.HasError = true;
                    plan.ErrorMessage = ex.Message;
                }

                return plan;
            });
        }

        /// <summary>
        /// Get query execution statistics
        /// </summary>
        private async Task<QueryStatistics> GetQueryStatisticsAsync(string queryText)
        {
            return await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
            {
                var stats = new QueryStatistics();
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    // Enable IO and TIME statistics
                    using (var cmd = new SqlCommand("SET STATISTICS IO ON; SET STATISTICS TIME ON", conn))
                    {
                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    var messages = new List<string>();
                    conn.InfoMessage += (s, e) => messages.Add(e.Message);

                    // Execute the query
                    using (var queryCmd = new SqlCommand(queryText, conn))
                    {
                        queryCmd.CommandTimeout = 300; // 5 minutes
                        using var reader = await queryCmd.ExecuteReaderAsync(ct);
                        
                        // Count rows
                        while (reader.Read())
                        {
                            stats.RowsReturned++;
                        }
                    }

                    stopwatch.Stop();
                    stats.Duration = stopwatch.Elapsed;

                    // Parse statistics from messages
                    ParseStatisticsFromMessages(stats, messages);

                    // Disable statistics
                    using (var cmd = new SqlCommand("SET STATISTICS IO OFF; SET STATISTICS TIME OFF", conn))
                    {
                        await cmd.ExecuteNonQueryAsync(ct);
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    stats.Duration = stopwatch.Elapsed;
                    stats.HasError = true;
                    stats.ErrorMessage = ex.Message;
                }

                return stats;
            });
        }

        /// <summary>
        /// Analyze query complexity metrics
        /// </summary>
        private QueryComplexityMetrics AnalyzeQueryComplexity(string queryText)
        {
            var metrics = new QueryComplexityMetrics();
            var upperQuery = queryText.ToUpper();

            // Count various SQL constructs
            metrics.JoinCount = CountOccurrences(upperQuery, @"\bJOIN\b");
            metrics.SubqueryCount = CountOccurrences(queryText, @"\([^)]*SELECT[^)]*\)");
            metrics.UnionCount = CountOccurrences(upperQuery, @"\bUNION\b");
            metrics.CteCount = CountOccurrences(upperQuery, @"\bWITH\b.*\bAS\b");
            metrics.WindowFunctionCount = CountOccurrences(upperQuery, @"\bOVER\s*\(");
            metrics.AggregateCount = CountOccurrences(upperQuery, @"\b(COUNT|SUM|AVG|MIN|MAX|STRING_AGG)\b");

            // Calculate complexity score
            metrics.ComplexityScore = 
                (metrics.JoinCount * 2) +
                (metrics.SubqueryCount * 3) +
                (metrics.UnionCount * 2) +
                (metrics.CteCount * 1) +
                (metrics.WindowFunctionCount * 2) +
                (metrics.AggregateCount * 1);

            // Determine complexity level
            metrics.ComplexityLevel = metrics.ComplexityScore switch
            {
                <= 5 => "Simple",
                <= 15 => "Moderate",
                <= 30 => "Complex",
                _ => "Very Complex"
            };

            return metrics;
        }

        /// <summary>
        /// Generate optimization suggestions
        /// </summary>
        private List<OptimizationSuggestion> GenerateOptimizationSuggestions(string queryText, ExecutionPlan plan, QueryStatistics stats)
        {
            var suggestions = new List<OptimizationSuggestion>();
            var upperQuery = queryText.ToUpper();

            // Check for SELECT *
            if (upperQuery.Contains("SELECT *"))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Category = "Column Selection",
                    Priority = "Medium",
                    Description = "Avoid using SELECT * - specify only needed columns",
                    Recommendation = "Replace SELECT * with explicit column list to reduce I/O and improve performance"
                });
            }

            // Check for WHERE clause issues
            if (!upperQuery.Contains("WHERE") && (upperQuery.Contains("SELECT") && !upperQuery.Contains("COUNT(*)")))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Category = "Filtering",
                    Priority = "High",
                    Description = "Query lacks WHERE clause - may return excessive data",
                    Recommendation = "Add appropriate WHERE clause to limit result set"
                });
            }

            // Check for ORDER BY without LIMIT/TOP
            if (upperQuery.Contains("ORDER BY") && !upperQuery.Contains("TOP") && !upperQuery.Contains("LIMIT"))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Category = "Result Set",
                    Priority = "Medium",
                    Description = "ORDER BY without row limitation may be inefficient",
                    Recommendation = "Consider adding TOP N clause to limit sorted results"
                });
            }

            // Check for potential N+1 problems (multiple similar queries)
            var similarQueries = _queryHistory.Where(q => 
                LevenshteinDistance(q.QueryText, queryText) < queryText.Length * 0.2)
                .Count();

            if (similarQueries > 5)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Category = "Query Pattern",
                    Priority = "High",
                    Description = "Multiple similar queries detected - potential N+1 problem",
                    Recommendation = "Consider using JOINs or CTEs to reduce round trips"
                });
            }

            // Check statistics-based suggestions
            if (stats != null && !stats.HasError)
            {
                if (stats.PhysicalReads > stats.LogicalReads * 0.1)
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Category = "I/O Performance",
                        Priority = "High",
                        Description = "High physical reads detected - data not cached",
                        Recommendation = "Consider adding indexes or optimizing query to improve caching"
                    });
                }

                if (stats.Duration.TotalSeconds > 10)
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Category = "Execution Time",
                        Priority = "High",
                        Description = "Query execution time is high",
                        Recommendation = "Review execution plan for table scans, missing indexes, or inefficient joins"
                    });
                }
            }

            // Check execution plan for issues
            if (plan != null && !plan.HasError && !string.IsNullOrEmpty(plan.PlanXml))
            {
                if (plan.PlanXml.Contains("TableScan"))
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Category = "Indexing",
                        Priority = "High",
                        Description = "Table scan detected in execution plan",
                        Recommendation = "Consider adding indexes on columns used in WHERE, JOIN, and ORDER BY clauses"
                    });
                }

                if (plan.EstimatedCost > 50)
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Category = "Query Cost",
                        Priority = "Medium",
                        Description = "High estimated query cost",
                        Recommendation = "Review query logic and consider breaking into smaller operations"
                    });
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Get performance trends for dashboard
        /// </summary>
        public QueryPerformanceTrends GetPerformanceTrends()
        {
            var trends = new QueryPerformanceTrends();
            
            if (_queryHistory.Count == 0)
                return trends;

            var recentQueries = _queryHistory
                .Where(q => q.ExecutedAt > DateTime.Now.AddDays(-7))
                .OrderBy(q => q.ExecutedAt)
                .ToList();

            if (recentQueries.Count == 0)
                return trends;

            trends.AverageExecutionTime = TimeSpan.FromTicks((long)recentQueries.Average(q => q.ExecutionTime.Ticks));
            trends.TotalQueriesExecuted = recentQueries.Count;
            trends.SlowestQuery = recentQueries.OrderByDescending(q => q.ExecutionTime).FirstOrDefault();
            trends.FastestQuery = recentQueries.OrderBy(q => q.ExecutionTime).FirstOrDefault();
            trends.TotalLogicalReads = recentQueries.Sum(q => q.LogicalReads);
            trends.TotalPhysicalReads = recentQueries.Sum(q => q.PhysicalReads);

            // Calculate daily averages
            trends.DailyAverages = recentQueries
                .GroupBy(q => q.ExecutedAt.Date)
                .Select(g => new DailyPerformanceMetric
                {
                    Date = g.Key,
                    AverageExecutionTime = TimeSpan.FromTicks((long)g.Average(q => q.ExecutionTime.Ticks)),
                    QueryCount = g.Count(),
                    AverageLogicalReads = g.Average(q => q.LogicalReads)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return trends;
        }

        #region Helper Methods

        private int CountOccurrences(string text, string pattern)
        {
            try
            {
                return Regex.Matches(text, pattern, RegexOptions.IgnoreCase).Count;
            }
            catch
            {
                return 0;
            }
        }

        private double ExtractEstimatedCostFromPlan(string planXml)
        {
            try
            {
                var match = Regex.Match(planXml, @"EstimatedTotalSubtreeCost=""([^""]+)""");
                if (match.Success && double.TryParse(match.Groups[1].Value, out var cost))
                {
                    return cost;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error extracting cost from execution plan: {Message}", ex.Message);
            }
            return 0;
        }

        private List<string> ExtractOperationsFromPlan(string planXml)
        {
            var operations = new List<string>();
            try
            {
                var matches = Regex.Matches(planXml, @"PhysicalOp=""([^""]+)""");
                foreach (Match match in matches)
                {
                    operations.Add(match.Groups[1].Value);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error extracting operations from execution plan: {Message}", ex.Message);
            }
            return operations;
        }

        private void ParseStatisticsFromMessages(QueryStatistics stats, List<string> messages)
        {
            foreach (var message in messages)
            {
                // Parse logical reads
                var logicalMatch = Regex.Match(message, @"logical reads (\d+)");
                if (logicalMatch.Success && long.TryParse(logicalMatch.Groups[1].Value, out var logicalReads))
                {
                    stats.LogicalReads += logicalReads;
                }

                // Parse physical reads
                var physicalMatch = Regex.Match(message, @"physical reads (\d+)");
                if (physicalMatch.Success && long.TryParse(physicalMatch.Groups[1].Value, out var physicalReads))
                {
                    stats.PhysicalReads += physicalReads;
                }

                // Parse CPU time
                var cpuMatch = Regex.Match(message, @"CPU time = (\d+) ms");
                if (cpuMatch.Success && long.TryParse(cpuMatch.Groups[1].Value, out var cpuTime))
                {
                    stats.CpuTime = TimeSpan.FromMilliseconds(cpuTime);
                }
            }
        }

        private int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t))
                return 0;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        #endregion
    }

    #region Supporting Classes

    public class QueryAnalysisResult
    {
        public string QueryText { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; }
        public ExecutionPlan ExecutionPlan { get; set; }
        public QueryStatistics Statistics { get; set; }
        public QueryComplexityMetrics ComplexityMetrics { get; set; }
        public List<OptimizationSuggestion> OptimizationSuggestions { get; set; } = new List<OptimizationSuggestion>();
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ExecutionPlan
    {
        public string PlanXml { get; set; } = string.Empty;
        public double EstimatedCost { get; set; }
        public List<string> Operations { get; set; } = new List<string>();
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class QueryStatistics
    {
        public TimeSpan Duration { get; set; }
        public TimeSpan CpuTime { get; set; }
        public long LogicalReads { get; set; }
        public long PhysicalReads { get; set; }
        public int RowsReturned { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class QueryComplexityMetrics
    {
        public int JoinCount { get; set; }
        public int SubqueryCount { get; set; }
        public int UnionCount { get; set; }
        public int CteCount { get; set; }
        public int WindowFunctionCount { get; set; }
        public int AggregateCount { get; set; }
        public int ComplexityScore { get; set; }
        public string ComplexityLevel { get; set; } = "Simple";
    }

    public class OptimizationSuggestion
    {
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class QueryPerformanceMetric
    {
        public string QueryText { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public long LogicalReads { get; set; }
        public long PhysicalReads { get; set; }
        public DateTime ExecutedAt { get; set; }
    }

    public class QueryPerformanceTrends
    {
        public TimeSpan AverageExecutionTime { get; set; }
        public int TotalQueriesExecuted { get; set; }
        public QueryPerformanceMetric SlowestQuery { get; set; }
        public QueryPerformanceMetric FastestQuery { get; set; }
        public long TotalLogicalReads { get; set; }
        public long TotalPhysicalReads { get; set; }
        public List<DailyPerformanceMetric> DailyAverages { get; set; } = new List<DailyPerformanceMetric>();
    }

    public class DailyPerformanceMetric
    {
        public DateTime Date { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public int QueryCount { get; set; }
        public double AverageLogicalReads { get; set; }
    }

    #endregion
}
