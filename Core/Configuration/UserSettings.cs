using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;

namespace SqlServerManager.Core.Configuration
{
    public enum Theme
    {
        Light,
        Dark,
        Auto
    }

    public enum EditorFontSize
    {
        Small = 8,
        Normal = 10,
        Large = 12,
        ExtraLarge = 14
    }

    public class RecentConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string AuthenticationType { get; set; } = "Windows";
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public DateTime LastUsed { get; set; } = DateTime.Now;
        public int UsageCount { get; set; } = 0;
        public bool IsFavorite { get; set; } = false;
        public bool IsLastConnection { get; set; } = false;
    }

    public class AutoReconnectSettings
    {
        public bool EnableAutoReconnect { get; set; } = false;
        public bool RememberLastConnection { get; set; } = true;
        public int ReconnectTimeoutSeconds { get; set; } = 10;
        public int MaxReconnectAttempts { get; set; } = 3;
        public bool ShowReconnectDialog { get; set; } = true;
        public bool AutoReconnectOnStartup { get; set; } = false;
    }

    public class WindowLayout
    {
        public Point Location { get; set; } = new Point(100, 100);
        public Size Size { get; set; } = new Size(1200, 800);
        public bool IsMaximized { get; set; } = false;
        public Dictionary<string, object> PanelStates { get; set; } = new();
        public Dictionary<string, int> SplitterPositions { get; set; } = new();
    }

    public class QueryExecutionSettings
    {
        public int QueryTimeoutSeconds { get; set; } = 30;
        public int MaxRowsReturned { get; set; } = 10000;
        public bool ExecuteInTransactions { get; set; } = true;
        public bool ShowExecutionPlan { get; set; } = false;
        public bool ShowExecutionTime { get; set; } = true;
        public bool PromptBeforeExecute { get; set; } = false;
        public bool AutoCommit { get; set; } = true;
        public bool EnableIntelliSense { get; set; } = true;
    }

    public class EditorPreferences
    {
        public string FontFamily { get; set; } = "Consolas";
        public EditorFontSize FontSize { get; set; } = EditorFontSize.Normal;
        public bool WordWrap { get; set; } = false;
        public bool ShowLineNumbers { get; set; } = true;
        public bool HighlightCurrentLine { get; set; } = true;
        public bool ShowWhitespace { get; set; } = false;
        public int TabSize { get; set; } = 4;
        public bool UseSpacesForTabs { get; set; } = true;
        public bool AutoIndent { get; set; } = true;
        public bool BracketMatching { get; set; } = true;
        public string Theme { get; set; } = "Visual Studio";
        public Dictionary<string, string> Colors { get; set; } = new();
    }

    public class BackupSettings
    {
        public string DefaultBackupLocation { get; set; } = string.Empty;
        public bool CompressBackups { get; set; } = true;
        public bool VerifyBackups { get; set; } = true;
        public int RetentionDays { get; set; } = 30;
        public bool AutoBackupEnabled { get; set; } = false;
        public TimeSpan AutoBackupTime { get; set; } = new TimeSpan(2, 0, 0); // 2 AM
    }

    public class NetworkDiscoverySettings
    {
        public int TimeoutMilliseconds { get; set; } = 3000;
        public int MaxConcurrentScans { get; set; } = 50;
        public bool EnableIPv6 { get; set; } = false;
        public List<string> CustomPorts { get; set; } = new() { "1433", "1434" };
        public bool AutoRefresh { get; set; } = true;
        public int RefreshIntervalMinutes { get; set; } = 5;
    }

    public class UserSettings
    {
        public string Version { get; set; } = "1.2.0";
        public DateTime LastSaved { get; set; } = DateTime.Now;

        // Appearance
        public Theme PreferredTheme { get; set; } = Theme.Auto;
        public string Language { get; set; } = "en-US";
        public bool ShowToolTips { get; set; } = true;
        public bool ConfirmBeforeExit { get; set; } = true;

        // Editor preferences
        public EditorPreferences EditorPreferences { get; set; } = new();

        // Window and layout
        public WindowLayout LastWindowLayout { get; set; } = new();
        public bool RememberWindowState { get; set; } = true;
        public bool ShowStartupDialog { get; set; } = true;

        // Recent connections
        public List<RecentConnection> RecentConnections { get; set; } = new();
        public int MaxRecentConnections { get; set; } = 20;

        // Auto-reconnect settings
        public AutoReconnectSettings AutoReconnectSettings { get; set; } = new();

        // Query execution
        public QueryExecutionSettings QuerySettings { get; set; } = new();

        // Backup settings
        public BackupSettings BackupSettings { get; set; } = new();

        // Network discovery
        public NetworkDiscoverySettings NetworkDiscoverySettings { get; set; } = new();

        // Feature toggles
        public Dictionary<string, bool> FeatureFlags { get; set; } = new()
        {
            ["EnableQueryHistory"] = true,
            ["EnableAutoComplete"] = true,
            ["EnableSyntaxHighlighting"] = true,
            ["EnableBackgroundOperations"] = true,
            ["EnableTelemetry"] = false,
            ["EnableAutoUpdates"] = true,
            ["EnablePerformanceMonitoring"] = true
        };

        // Custom settings
        public Dictionary<string, object> CustomSettings { get; set; } = new();

        // Statistics
        public Dictionary<string, object> Statistics { get; set; } = new();

        [JsonIgnore]
        public static string DefaultSettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SqlServerManager",
            "settings.json"
        );
    }

    public class UserSettingsManager
    {
        private readonly string _settingsPath;
        private UserSettings _settings;
        private readonly object _lockObject = new object();

        public UserSettingsManager(string settingsPath = null)
        {
            _settingsPath = settingsPath ?? UserSettings.DefaultSettingsPath;
            _settings = LoadSettings();
        }

        public UserSettings Settings
        {
            get
            {
                lock (_lockObject)
                {
                    return _settings;
                }
            }
        }

        #region Loading and Saving

        public UserSettings LoadSettings()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(_settingsPath))
                    {
                        var json = File.ReadAllText(_settingsPath);
                        var settings = JsonSerializer.Deserialize<UserSettings>(json, GetJsonOptions());
                        
                        // Migrate settings if needed
                        if (settings != null)
                        {
                            MigrateSettings(settings);
                            return settings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't throw - use defaults
                    Console.WriteLine($"Error loading settings: {ex.Message}");
                }

                return CreateDefaultSettings();
            }
        }

        public void SaveSettings()
        {
            lock (_lockObject)
            {
                try
                {
                    _settings.LastSaved = DateTime.Now;
                    
                    var settingsDir = Path.GetDirectoryName(_settingsPath);
                    if (!string.IsNullOrEmpty(settingsDir))
                        Directory.CreateDirectory(settingsDir);

                    var json = JsonSerializer.Serialize(_settings, GetJsonOptions());
                    File.WriteAllText(_settingsPath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving settings: {ex.Message}");
                    throw;
                }
            }
        }

        public void ResetToDefaults()
        {
            lock (_lockObject)
            {
                _settings = CreateDefaultSettings();
                SaveSettings();
            }
        }

        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        private UserSettings CreateDefaultSettings()
        {
            var settings = new UserSettings();
            
            // Set default backup location
            settings.BackupSettings.DefaultBackupLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SQL Backups"
            );

            // Initialize default colors
            settings.EditorPreferences.Colors = new Dictionary<string, string>
            {
                ["Background"] = "#FFFFFF",
                ["Foreground"] = "#000000",
                ["Selection"] = "#3399FF",
                ["LineNumbers"] = "#2B91AF",
                ["Keywords"] = "#0000FF",
                ["Strings"] = "#A31515",
                ["Comments"] = "#008000"
            };

            return settings;
        }

        private void MigrateSettings(UserSettings settings)
        {
            // Perform any necessary migrations for older settings versions
            if (string.IsNullOrEmpty(settings.Version))
            {
                settings.Version = "1.2.0";
            }

            // Add new feature flags if they don't exist
            var defaultFlags = new UserSettings().FeatureFlags;
            foreach (var flag in defaultFlags)
            {
                if (!settings.FeatureFlags.ContainsKey(flag.Key))
                {
                    settings.FeatureFlags[flag.Key] = flag.Value;
                }
            }
        }

        #endregion

        #region Theme Management

        public void SetTheme(Theme theme)
        {
            lock (_lockObject)
            {
                _settings.PreferredTheme = theme;
            }
        }

        public Theme GetEffectiveTheme()
        {
            lock (_lockObject)
            {
                if (_settings.PreferredTheme == Theme.Auto)
                {
                    // Determine based on system settings or time of day
                    var hour = DateTime.Now.Hour;
                    return (hour >= 6 && hour < 18) ? Theme.Light : Theme.Dark;
                }
                return _settings.PreferredTheme;
            }
        }

        #endregion

        #region Recent Connections

        public void AddRecentConnection(string name, string server, string database, string authType)
        {
            lock (_lockObject)
            {
                // Remove existing connection with same server/database
                _settings.RecentConnections.RemoveAll(c => 
                    c.Server.Equals(server, StringComparison.OrdinalIgnoreCase) &&
                    c.Database.Equals(database, StringComparison.OrdinalIgnoreCase));

                // Add new connection at the beginning
                var connection = new RecentConnection
                {
                    Name = name,
                    Server = server,
                    Database = database,
                    AuthenticationType = authType,
                    LastUsed = DateTime.Now,
                    UsageCount = 1
                };

                _settings.RecentConnections.Insert(0, connection);

                // Maintain max recent connections
                if (_settings.RecentConnections.Count > _settings.MaxRecentConnections)
                {
                    _settings.RecentConnections.RemoveRange(_settings.MaxRecentConnections, 
                        _settings.RecentConnections.Count - _settings.MaxRecentConnections);
                }
            }
        }

        public void UpdateConnectionUsage(string server, string database)
        {
            lock (_lockObject)
            {
                // Clear previous last connection flag
                foreach (var conn in _settings.RecentConnections)
                {
                    conn.IsLastConnection = false;
                }

                var connection = _settings.RecentConnections.Find(c => 
                    c.Server.Equals(server, StringComparison.OrdinalIgnoreCase) &&
                    c.Database.Equals(database, StringComparison.OrdinalIgnoreCase));

                if (connection != null)
                {
                    connection.LastUsed = DateTime.Now;
                    connection.UsageCount++;
                    connection.IsLastConnection = true;
                    
                    // Move to front of list
                    _settings.RecentConnections.Remove(connection);
                    _settings.RecentConnections.Insert(0, connection);
                }
            }
        }

        public void ToggleConnectionFavorite(string id)
        {
            lock (_lockObject)
            {
                var connection = _settings.RecentConnections.Find(c => c.Id == id);
                if (connection != null)
                {
                    connection.IsFavorite = !connection.IsFavorite;
                }
            }
        }

        public void RemoveRecentConnection(string id)
        {
            lock (_lockObject)
            {
                _settings.RecentConnections.RemoveAll(c => c.Id == id);
            }
        }

        public List<RecentConnection> GetFavoriteConnections()
        {
            lock (_lockObject)
            {
                return _settings.RecentConnections.FindAll(c => c.IsFavorite);
            }
        }

        public RecentConnection GetLastConnection()
        {
            lock (_lockObject)
            {
                // First try to find the explicitly marked last connection
                var lastConnection = _settings.RecentConnections.Find(c => c.IsLastConnection);
                
                // If not found, return the most recently used connection
                if (lastConnection == null && _settings.RecentConnections.Count > 0)
                {
                    lastConnection = _settings.RecentConnections[0];
                }
                
                return lastConnection;
            }
        }

        #endregion

        #region Window State

        public void SaveWindowState(Point location, Size size, bool isMaximized)
        {
            lock (_lockObject)
            {
                if (_settings.RememberWindowState)
                {
                    _settings.LastWindowLayout.Location = location;
                    _settings.LastWindowLayout.Size = size;
                    _settings.LastWindowLayout.IsMaximized = isMaximized;
                }
            }
        }

        public void SaveSplitterPosition(string splitterName, int position)
        {
            lock (_lockObject)
            {
                _settings.LastWindowLayout.SplitterPositions[splitterName] = position;
            }
        }

        public int GetSplitterPosition(string splitterName, int defaultPosition)
        {
            lock (_lockObject)
            {
                return _settings.LastWindowLayout.SplitterPositions.TryGetValue(splitterName, out var position) 
                    ? position : defaultPosition;
            }
        }

        #endregion

        #region Feature Flags

        public bool IsFeatureEnabled(string featureName)
        {
            lock (_lockObject)
            {
                return _settings.FeatureFlags.TryGetValue(featureName, out var enabled) && enabled;
            }
        }

        public void SetFeatureEnabled(string featureName, bool enabled)
        {
            lock (_lockObject)
            {
                _settings.FeatureFlags[featureName] = enabled;
            }
        }

        #endregion

        #region Custom Settings

        public T GetCustomSetting<T>(string key, T defaultValue = default)
        {
            lock (_lockObject)
            {
                if (_settings.CustomSettings.TryGetValue(key, out var value))
                {
                    try
                    {
                        if (value is JsonElement jsonElement)
                        {
                            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                        }
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                return defaultValue;
            }
        }

        public void SetCustomSetting<T>(string key, T value)
        {
            lock (_lockObject)
            {
                _settings.CustomSettings[key] = value;
            }
        }

        #endregion

        #region Statistics

        public void IncrementStatistic(string key, int increment = 1)
        {
            lock (_lockObject)
            {
                if (_settings.Statistics.TryGetValue(key, out var current))
                {
                    if (current is int intValue)
                    {
                        _settings.Statistics[key] = intValue + increment;
                    }
                    else
                    {
                        _settings.Statistics[key] = increment;
                    }
                }
                else
                {
                    _settings.Statistics[key] = increment;
                }
            }
        }

        public void SetStatistic(string key, object value)
        {
            lock (_lockObject)
            {
                _settings.Statistics[key] = value;
            }
        }

        public T GetStatistic<T>(string key, T defaultValue = default)
        {
            lock (_lockObject)
            {
                if (_settings.Statistics.TryGetValue(key, out var value))
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                return defaultValue;
            }
        }

        #endregion

        #region Validation

        public bool ValidateSettings()
        {
            try
            {
                // Validate theme
                if (!Enum.IsDefined(typeof(Theme), _settings.PreferredTheme))
                    _settings.PreferredTheme = Theme.Auto;

                // Validate query timeout
                if (_settings.QuerySettings.QueryTimeoutSeconds < 0)
                    _settings.QuerySettings.QueryTimeoutSeconds = 30;

                // Validate max rows
                if (_settings.QuerySettings.MaxRowsReturned < 100)
                    _settings.QuerySettings.MaxRowsReturned = 10000;

                // Validate font size
                if (!Enum.IsDefined(typeof(EditorFontSize), _settings.EditorPreferences.FontSize))
                    _settings.EditorPreferences.FontSize = EditorFontSize.Normal;

                // Validate tab size
                if (_settings.EditorPreferences.TabSize < 1 || _settings.EditorPreferences.TabSize > 16)
                    _settings.EditorPreferences.TabSize = 4;

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Export/Import

        public void ExportSettings(string filePath)
        {
            lock (_lockObject)
            {
                var json = JsonSerializer.Serialize(_settings, GetJsonOptions());
                File.WriteAllText(filePath, json);
            }
        }

        public void ImportSettings(string filePath)
        {
            lock (_lockObject)
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var importedSettings = JsonSerializer.Deserialize<UserSettings>(json, GetJsonOptions());
                    
                    if (importedSettings != null)
                    {
                        _settings = importedSettings;
                        ValidateSettings();
                        SaveSettings();
                    }
                }
            }
        }

        #endregion
    }
}
