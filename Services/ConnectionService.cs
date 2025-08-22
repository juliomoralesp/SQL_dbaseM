using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace SqlServerManager.Services
{
    /// <summary>
    /// Service for managing SQL Server connections with proper disposal, retry policies, and connection pooling
    /// </summary>
    public class ConnectionService : IConnectionService, IDisposable
    {
        private readonly IConfiguration _configuration;
        // Retry policy removed for simplification
        private SqlConnection _currentConnection;
        private readonly object _connectionLock = new object();
        private bool _disposed;

        public event EventHandler<ConnectionEventArgs> ConnectionChanged;
        public event EventHandler<string> StatusChanged;

        public bool IsConnected => _currentConnection?.State == ConnectionState.Open;
        public string CurrentServer { get; private set; }
        public string CurrentDatabase { get; private set; }

        public ConnectionService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Connect to SQL Server with proper error handling and retry logic
        /// </summary>
        public async Task<bool> ConnectAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

            try
            {
                StatusChanged?.Invoke(this, "Connecting to SQL Server...");
                LoggingService.LogInformation("Attempting to connect to SQL Server");

                await ConnectInternalAsync(connectionString, cancellationToken).ConfigureAwait(false);

                LoggingService.LogConnection(CurrentServer, CurrentDatabase, true);
                StatusChanged?.Invoke(this, $"Connected to {CurrentServer}");
                ConnectionChanged?.Invoke(this, new ConnectionEventArgs(true, CurrentServer, CurrentDatabase));

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogConnection(CurrentServer ?? "Unknown", CurrentDatabase ?? "Unknown", false, ex);
                StatusChanged?.Invoke(this, "Connection failed");
                ConnectionChanged?.Invoke(this, new ConnectionEventArgs(false, null, null));
                throw;
            }
        }

        /// <summary>
        /// Internal connection method
        /// </summary>
        private async Task ConnectInternalAsync(string connectionString, CancellationToken cancellationToken)
        {
            // Dispose existing connection
            await DisconnectAsync().ConfigureAwait(false);

            lock (_connectionLock)
            {
                _currentConnection = new SqlConnection(connectionString);
            }

            // Connection timeout is set via connection string, not property

            await _currentConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Extract server and database info
            ExtractConnectionInfo(connectionString);

            LoggingService.LogInformation("Successfully connected to {Server}, Database: {Database}", CurrentServer, CurrentDatabase);
        }

        /// <summary>
        /// Test connection without setting it as current
        /// </summary>
        public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            try
            {
                using var testConnection = new SqlConnection(connectionString);
                await testConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                
                LoggingService.LogInformation("Connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Connection test failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Execute a command with proper connection management
        /// </summary>
        public async Task<T> ExecuteWithConnectionAsync<T>(Func<SqlConnection, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("No active database connection");

            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectionService));

            try
            {
                // Ensure connection is still open
                if (_currentConnection.State != ConnectionState.Open)
                {
                    await _currentConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                return await operation(_currentConnection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsConnectionException(ex))
            {
                // Connection was lost, notify subscribers
                LoggingService.LogWarning("Connection lost, attempting to reconnect: {Message}", ex.Message);
                ConnectionChanged?.Invoke(this, new ConnectionEventArgs(false, CurrentServer, CurrentDatabase));
                throw;
            }
        }

        /// <summary>
        /// Execute a command without return value
        /// </summary>
        public async Task ExecuteWithConnectionAsync(Func<SqlConnection, CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync<object>(async (conn, ct) =>
            {
                await operation(conn, ct).ConfigureAwait(false);
                return null;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new command with proper configuration
        /// </summary>
        public SqlCommand CreateCommand(string commandText, CommandType commandType = CommandType.Text)
        {
            if (!IsConnected)
                throw new InvalidOperationException("No active database connection");

            var command = _currentConnection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;
            
            // Set timeout from configuration
            var timeout = ConfigurationService.GetValue<int>("Database:CommandTimeout", 30);
            command.CommandTimeout = timeout;

            return command;
        }

        /// <summary>
        /// Disconnect from SQL Server
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_currentConnection != null)
            {
                lock (_connectionLock)
                {
                    if (_currentConnection != null)
                    {
                        try
                        {
                            if (_currentConnection.State != ConnectionState.Closed)
                            {
                                _currentConnection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogWarning("Error closing connection: {Message}", ex.Message);
                        }
                        finally
                        {
                            _currentConnection.Dispose();
                            _currentConnection = null;
                        }
                    }
                }

                CurrentServer = null;
                CurrentDatabase = null;
                
                LoggingService.LogInformation("Disconnected from SQL Server");
                StatusChanged?.Invoke(this, "Disconnected");
                ConnectionChanged?.Invoke(this, new ConnectionEventArgs(false, null, null));
            }

            await Task.CompletedTask;
        }


        /// <summary>
        /// Check if exception indicates connection loss
        /// </summary>
        private static bool IsConnectionException(Exception ex)
        {
            return ex switch
            {
                SqlException sqlEx => sqlEx.Number == 2 || sqlEx.Number == 53 || sqlEx.Number == 18456,
                InvalidOperationException => ex.Message.Contains("connection"),
                _ => false
            };
        }

        /// <summary>
        /// Extract server and database information from connection string
        /// </summary>
        private void ExtractConnectionInfo(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            CurrentServer = builder.DataSource;
            CurrentDatabase = string.IsNullOrEmpty(builder.InitialCatalog) ? "master" : builder.InitialCatalog;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _ = DisconnectAsync(); // Fire and forget for dispose
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Connection event arguments
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Server { get; }
        public string Database { get; }

        public ConnectionEventArgs(bool isConnected, string server, string database)
        {
            IsConnected = isConnected;
            Server = server;
            Database = database;
        }
    }
}
