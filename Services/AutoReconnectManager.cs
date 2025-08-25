using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SqlServerManager.Core.Configuration;
using Microsoft.Data.SqlClient;

namespace SqlServerManager.Services
{
    /// <summary>
    /// Manages automatic reconnection to the last known SQL Server
    /// </summary>
    public class AutoReconnectManager
    {
        private readonly IConnectionService _connectionService;
        private readonly UserSettingsManager _settingsManager;
        private RecentConnection _lastKnownConnection;
    private System.Threading.Timer _reconnectTimer;
        private int _reconnectAttempts = 0;
        private bool _isReconnecting = false;
        
        public event EventHandler<AutoReconnectEventArgs> ReconnectAttempted;
        public event EventHandler<AutoReconnectEventArgs> ReconnectSucceeded;
        public event EventHandler<AutoReconnectEventArgs> ReconnectFailed;

        public AutoReconnectManager(IConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _settingsManager = new UserSettingsManager();
        }

        /// <summary>
        /// Gets the auto-reconnect settings
        /// </summary>
        public AutoReconnectSettings Settings => _settingsManager.Settings.AutoReconnectSettings;

        /// <summary>
        /// Gets the last known connection
        /// </summary>
        public RecentConnection LastKnownConnection => _lastKnownConnection;

        /// <summary>
        /// Save the current connection as the last known connection
        /// </summary>
        /// <param name="server">Server name</param>
        /// <param name="database">Database name</param>
        /// <param name="connectionString">Full connection string</param>
        public void SaveLastConnection(string server, string database, string connectionString)
        {
            if (!Settings.RememberLastConnection) return;

            try
            {
                // Parse connection string to extract connection details
                var builder = new SqlConnectionStringBuilder(connectionString);
                
                // Clear previous last connection flags
                foreach (var conn in _settingsManager.Settings.RecentConnections)
                {
                    conn.IsLastConnection = false;
                }

                // Find existing connection or create new one
                var existingConnection = _settingsManager.Settings.RecentConnections
                    .Find(c => c.Server.Equals(server, StringComparison.OrdinalIgnoreCase) && 
                              c.Database.Equals(database, StringComparison.OrdinalIgnoreCase));

                if (existingConnection != null)
                {
                    existingConnection.IsLastConnection = true;
                    existingConnection.LastUsed = DateTime.Now;
                    existingConnection.UsageCount++;
                    _lastKnownConnection = existingConnection;
                }
                else
                {
                    var newConnection = new RecentConnection
                    {
                        Name = $"{server} - {database}",
                        Server = server,
                        Database = database,
                        AuthenticationType = builder.IntegratedSecurity ? "Windows" : "SQL Server",
                        Username = builder.UserID ?? string.Empty,
                        LastUsed = DateTime.Now,
                        UsageCount = 1,
                        IsLastConnection = true
                    };

                    _settingsManager.Settings.RecentConnections.Insert(0, newConnection);
                    _lastKnownConnection = newConnection;
                }

                _settingsManager.SaveSettings();
                LoggingService.LogInformation("Saved last connection: {Server} - {Database}", server, database);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Failed to save last connection");
            }
        }

        /// <summary>
        /// Load the last known connection from settings
        /// </summary>
        /// <returns>Last known connection or null if not found</returns>
        public RecentConnection LoadLastConnection()
        {
            try
            {
                _lastKnownConnection = _settingsManager.Settings.RecentConnections
                    .Find(c => c.IsLastConnection);
                    
                return _lastKnownConnection;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Failed to load last connection");
                return null;
            }
        }

        /// <summary>
        /// Attempt to auto-reconnect on startup
        /// </summary>
        public async Task<bool> TryAutoReconnectOnStartupAsync()
        {
            if (!Settings.AutoReconnectOnStartup)
            {
                LoggingService.LogDebug("Auto-reconnect on startup is disabled");
                return false;
            }

            var lastConnection = LoadLastConnection();
            if (lastConnection == null)
            {
                LoggingService.LogDebug("No last connection found for auto-reconnect");
                return false;
            }

            LoggingService.LogInformation("Attempting auto-reconnect to {Server} - {Database}", 
                lastConnection.Server, lastConnection.Database);

            return await TryReconnect(lastConnection, showDialog: Settings.ShowReconnectDialog);
        }

        /// <summary>
        /// Start monitoring for connection loss and attempt auto-reconnect
        /// </summary>
        public void StartAutoReconnect()
        {
            if (!Settings.EnableAutoReconnect)
            {
                LoggingService.LogDebug("Auto-reconnect is disabled");
                return;
            }

            if (_lastKnownConnection == null)
            {
                LoadLastConnection();
            }

            LoggingService.LogInformation("Auto-reconnect monitoring started");
        }

        /// <summary>
        /// Stop auto-reconnect monitoring
        /// </summary>
        public void StopAutoReconnect()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
            _isReconnecting = false;
            _reconnectAttempts = 0;
            
            LoggingService.LogInformation("Auto-reconnect monitoring stopped");
        }

        /// <summary>
        /// Manually trigger a reconnection attempt
        /// </summary>
        public async Task<bool> TriggerReconnect()
        {
            if (_lastKnownConnection == null)
            {
                LoadLastConnection();
                if (_lastKnownConnection == null)
                {
                    LoggingService.LogWarning("No last known connection available for manual reconnect");
                    return false;
                }
            }

            return await TryReconnect(_lastKnownConnection, showDialog: true);
        }

        /// <summary>
        /// Notify that connection succeeded
        /// </summary>
        public void NotifyConnectionSucceeded(string server, string database, string connectionString)
        {
            // Connection established - save as last known connection
            SaveLastConnection(server, database, connectionString);
            
            // Stop reconnection attempts
            StopReconnectTimer();
        }
        
        /// <summary>
        /// Notify that connection was lost
        /// </summary>
        public void NotifyConnectionLost()
        {
            if (Settings.EnableAutoReconnect && _lastKnownConnection != null && !_isReconnecting)
            {
                // Connection lost - start reconnection attempts
                LoggingService.LogWarning("Connection lost, starting auto-reconnect attempts");
                StartReconnectTimer();
            }
        }

        /// <summary>
        /// Start the reconnect timer
        /// </summary>
        private void StartReconnectTimer()
        {
            if (_reconnectTimer != null) return;

            _reconnectAttempts = 0;
            var timeoutMs = Settings.ReconnectTimeoutSeconds * 1000;
            
            _reconnectTimer = new System.Threading.Timer(async _ => await AttemptReconnect(), 
                null, timeoutMs, timeoutMs);
        }

        /// <summary>
        /// Stop the reconnect timer
        /// </summary>
        private void StopReconnectTimer()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
            _isReconnecting = false;
            _reconnectAttempts = 0;
        }

        /// <summary>
        /// Attempt to reconnect (called by timer)
        /// </summary>
        private async Task AttemptReconnect()
        {
            if (_isReconnecting || _lastKnownConnection == null) return;

            _isReconnecting = true;
            _reconnectAttempts++;

            try
            {
                LoggingService.LogInformation("Auto-reconnect attempt {Attempt}/{MaxAttempts} to {Server}", 
                    _reconnectAttempts, Settings.MaxReconnectAttempts, _lastKnownConnection.Server);

                var success = await TryReconnect(_lastKnownConnection, showDialog: false);
                
                if (success)
                {
                    ReconnectSucceeded?.Invoke(this, new AutoReconnectEventArgs(true, _reconnectAttempts, _lastKnownConnection));
                    StopReconnectTimer();
                }
                else
                {
                    ReconnectAttempted?.Invoke(this, new AutoReconnectEventArgs(false, _reconnectAttempts, _lastKnownConnection));
                    
                    if (_reconnectAttempts >= Settings.MaxReconnectAttempts)
                    {
                        LoggingService.LogWarning("Auto-reconnect failed after {Attempts} attempts", _reconnectAttempts);
                        ReconnectFailed?.Invoke(this, new AutoReconnectEventArgs(false, _reconnectAttempts, _lastKnownConnection));
                        StopReconnectTimer();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error during auto-reconnect attempt {Attempt}", _reconnectAttempts);
                ReconnectAttempted?.Invoke(this, new AutoReconnectEventArgs(false, _reconnectAttempts, _lastKnownConnection, ex));
                
                if (_reconnectAttempts >= Settings.MaxReconnectAttempts)
                {
                    ReconnectFailed?.Invoke(this, new AutoReconnectEventArgs(false, _reconnectAttempts, _lastKnownConnection, ex));
                    StopReconnectTimer();
                }
            }
            finally
            {
                _isReconnecting = false;
            }
        }

        /// <summary>
        /// Try to reconnect to the specified connection
        /// </summary>
        private async Task<bool> TryReconnect(RecentConnection connection, bool showDialog)
        {
            try
            {
                // Build connection string
                var connectionString = BuildConnectionString(connection);
                
                if (showDialog && Settings.ShowReconnectDialog)
                {
                    // Show reconnect confirmation dialog on UI thread
                    var result = await ShowReconnectDialog(connection);
                    if (result != DialogResult.Yes)
                    {
                        LoggingService.LogInformation("User cancelled auto-reconnect dialog");
                        return false;
                    }
                }

                // Test connection by executing a simple query
                using (var testConnection = new SqlConnection(connectionString))
                {
                    await testConnection.OpenAsync();
                    using (var command = new SqlCommand("SELECT 1", testConnection))
                    {
                        await command.ExecuteScalarAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Failed to reconnect to {Server} - {Database}", 
                    connection.Server, connection.Database);
                return false;
            }
        }

        /// <summary>
        /// Build connection string from recent connection
        /// </summary>
        private string BuildConnectionString(RecentConnection connection)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = connection.Server,
                InitialCatalog = connection.Database,
                IntegratedSecurity = connection.AuthenticationType == "Windows",
                TrustServerCertificate = true,
                ConnectTimeout = 15
            };

            if (!builder.IntegratedSecurity && !string.IsNullOrEmpty(connection.Username))
            {
                builder.UserID = connection.Username;
                // Note: Password would need to be handled securely in a real implementation
                // For now, we'll rely on integrated security or prompt user
            }

            return builder.ConnectionString;
        }

        /// <summary>
        /// Show reconnect dialog to user
        /// </summary>
        private async Task<DialogResult> ShowReconnectDialog(RecentConnection connection)
        {
            return await Task.Run(() =>
            {
                // Must be called on UI thread
                if (Application.OpenForms.Count == 0) return DialogResult.No;
                
                var mainForm = Application.OpenForms[0];
                return mainForm.Invoke(new Func<DialogResult>(() =>
                {
                    var message = $"Connection lost to {connection.Server} - {connection.Database}.\n\n" +
                                 $"Would you like to attempt to reconnect?\n\n" +
                                 $"Attempt {_reconnectAttempts + 1} of {Settings.MaxReconnectAttempts}";
                                 
                    return MessageBox.Show(mainForm, message, "Auto-Reconnect", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }));
            });
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            StopAutoReconnect();
        }
    }

    /// <summary>
    /// Event arguments for auto-reconnect events
    /// </summary>
    public class AutoReconnectEventArgs : EventArgs
    {
        public bool Success { get; }
        public int AttemptNumber { get; }
        public RecentConnection Connection { get; }
        public Exception Exception { get; }

        public AutoReconnectEventArgs(bool success, int attemptNumber, RecentConnection connection, Exception exception = null)
        {
            Success = success;
            AttemptNumber = attemptNumber;
            Connection = connection;
            Exception = exception;
        }
    }
}
