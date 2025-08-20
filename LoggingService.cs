using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace SqlServerManager
{
    /// <summary>
    /// Centralized logging service with structured logging capabilities
    /// </summary>
    public static class LoggingService
    {
        private static ILogger _logger;
        private static bool _isInitialized = false;
        
        /// <summary>
        /// Initialize the logging service
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            
            try
            {
                var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "SqlServerManager", "Logs");
                
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                var logFile = Path.Combine(logDirectory, "SqlServerManager-.log");
                
                _logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "SQL Server Manager")
                    .Enrich.WithProperty("Version", GetApplicationVersion())
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(logFile,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        fileSizeLimitBytes: 10 * 1024 * 1024) // 10MB
                    .CreateLogger();
                
                _isInitialized = true;
                LogInformation("Logging service initialized successfully");
                LogInformation("Log directory: {LogDirectory}", logDirectory);
            }
            catch (Exception ex)
            {
                // Fallback to console if file logging fails
                _logger = new LoggerConfiguration()
                    .MinimumLevel.Warning()
                    .WriteTo.Console()
                    .CreateLogger();
                
                LogError(ex, "Failed to initialize full logging configuration, using console only");
                _isInitialized = true;
            }
        }
        
        /// <summary>
        /// Log debug information
        /// </summary>
        public static void LogDebug(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Debug(messageTemplate, propertyValues);
        }
        
        /// <summary>
        /// Log general information
        /// </summary>
        public static void LogInformation(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Information(messageTemplate, propertyValues);
        }
        
        /// <summary>
        /// Log warning messages
        /// </summary>
        public static void LogWarning(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Warning(messageTemplate, propertyValues);
        }
        
        /// <summary>
        /// Log error messages
        /// </summary>
        public static void LogError(string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Error(messageTemplate, propertyValues);
        }
        
        /// <summary>
        /// Log exceptions with context
        /// </summary>
        public static void LogError(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Error(exception, messageTemplate, propertyValues);
        }
        
        /// <summary>
        /// Log fatal errors
        /// </summary>
        public static void LogFatal(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            EnsureInitialized();
            _logger?.Fatal(exception, messageTemplate, propertyValues);
        }
        
        /// <summary>
        /// Log database operations with performance metrics
        /// </summary>
        public static void LogDatabaseOperation(string operation, string database, string table, long elapsedMs, bool success, Exception exception = null)
        {
            EnsureInitialized();
            
            var context = new
            {
                Operation = operation,
                Database = database,
                Table = table,
                ElapsedMs = elapsedMs,
                Success = success
            };
            
            if (success)
            {
                if (elapsedMs > 5000) // Log slow operations as warnings
                {
                    _logger?.Warning("Slow database operation: {Operation} on {Database}.{Table} took {ElapsedMs}ms", 
                        operation, database, table, elapsedMs);
                }
                else
                {
                    _logger?.Information("Database operation: {Operation} on {Database}.{Table} completed in {ElapsedMs}ms", 
                        operation, database, table, elapsedMs);
                }
            }
            else
            {
                _logger?.Error(exception, "Database operation failed: {Operation} on {Database}.{Table} after {ElapsedMs}ms", 
                    operation, database, table, elapsedMs);
            }
        }
        
        /// <summary>
        /// Log connection events
        /// </summary>
        public static void LogConnection(string server, string database, bool success, Exception exception = null)
        {
            EnsureInitialized();
            
            if (success)
            {
                _logger?.Information("Successfully connected to {Server}, Database: {Database}", server, database);
            }
            else
            {
                _logger?.Error(exception, "Failed to connect to {Server}, Database: {Database}", server, database);
            }
        }
        
        /// <summary>
        /// Log user actions for audit trail
        /// </summary>
        public static void LogUserAction(string action, string details = null, params object[] propertyValues)
        {
            EnsureInitialized();
            
            if (string.IsNullOrEmpty(details))
            {
                _logger?.Information("User Action: {Action}", action);
            }
            else
            {
                _logger?.Information("User Action: {Action} - {Details}", action, string.Format(details, propertyValues));
            }
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public static void Shutdown()
        {
            LogInformation("Shutting down logging service");
            (_logger as IDisposable)?.Dispose();
        }
        
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
        
        private static string GetApplicationVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
    
    /// <summary>
    /// Performance measurement helper
    /// </summary>
    public class PerformanceTimer : IDisposable
    {
        private readonly System.Diagnostics.Stopwatch _stopwatch;
        private readonly string _operation;
        private readonly string _database;
        private readonly string _table;
        private Exception _exception;
        
        public PerformanceTimer(string operation, string database = null, string table = null)
        {
            _operation = operation;
            _database = database;
            _table = table;
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            LoggingService.LogDebug("Starting operation: {Operation}", operation);
        }
        
        public void SetException(Exception exception)
        {
            _exception = exception;
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            var success = _exception == null;
            
            LoggingService.LogDatabaseOperation(_operation, _database ?? "Unknown", _table ?? "Unknown", 
                _stopwatch.ElapsedMilliseconds, success, _exception);
        }
    }
}
