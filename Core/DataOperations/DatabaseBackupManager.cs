using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SqlServerManager.Core.DataOperations
{
    public enum BackupType
    {
        Full,
        Differential,
        Log
    }

    public enum RestoreType
    {
        Database,
        FileGroup,
        Files,
        Page
    }

    public class BackupInfo
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public long SizeInBytes { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsCompressed { get; set; } = false;
        public bool IsEncrypted { get; set; } = false;
        public TimeSpan Duration { get; set; }
    }

    public class BackupHistory
    {
        public string DatabaseName { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public DateTime BackupStartDate { get; set; }
        public DateTime BackupFinishDate { get; set; }
        public string BackupPath { get; set; } = string.Empty;
        public long BackupSize { get; set; }
        public bool IsCompressed { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
    }

    public class RestoreOptions
    {
        public string NewDatabaseName { get; set; } = string.Empty;
        public bool Replace { get; set; } = false;
        public bool WithRecovery { get; set; } = true;
        public Dictionary<string, string> RelocateFiles { get; set; } = new();
        public DateTime? StopAt { get; set; }
        public string StopAtMarkName { get; set; } = string.Empty;
        public bool Verify { get; set; } = true;
        public bool CheckSumEnabled { get; set; } = true;
    }

    public class DatabaseBackupManager
    {
        private readonly string _connectionString;

        public DatabaseBackupManager(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        #region Backup Operations

        public async Task<BackupInfo> CreateBackupAsync(
            string databaseName,
            string backupPath,
            BackupType type = BackupType.Full,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default,
            string description = "",
            bool compress = true,
            bool checksum = true)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name is required", nameof(databaseName));

            if (string.IsNullOrWhiteSpace(backupPath))
                throw new ArgumentException("Backup path is required", nameof(backupPath));

            // Ensure backup directory exists
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir))
                Directory.CreateDirectory(backupDir);

            var startTime = DateTime.Now;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var backupCommand = BuildBackupCommand(databaseName, backupPath, type, description, compress, checksum);

            using var command = new SqlCommand(backupCommand, connection)
            {
                CommandTimeout = 0 // No timeout for backup operations
            };

            // Start the backup in a separate task
            var backupTask = command.ExecuteNonQueryAsync(cancellationToken);

            // Monitor progress if requested
            if (progress != null)
            {
                await MonitorBackupProgressAsync(connection, databaseName, progress, cancellationToken);
            }

            await backupTask;

            var endTime = DateTime.Now;
            var fileInfo = new FileInfo(backupPath);

            return new BackupInfo
            {
                DatabaseName = databaseName,
                BackupPath = backupPath,
                Type = type,
                CreatedAt = startTime,
                SizeInBytes = fileInfo.Exists ? fileInfo.Length : 0,
                Description = description,
                IsCompressed = compress,
                Duration = endTime - startTime
            };
        }

        private string BuildBackupCommand(string databaseName, string backupPath, BackupType type, 
            string description, bool compress, bool checksum)
        {
            var backupType = type switch
            {
                BackupType.Full => "DATABASE",
                BackupType.Differential => "DATABASE",
                BackupType.Log => "LOG",
                _ => "DATABASE"
            };

            var sql = $"BACKUP {backupType} [{databaseName}] TO DISK = @BackupPath";

            if (type == BackupType.Differential)
                sql += " WITH DIFFERENTIAL";
            else
                sql += " WITH";

            var options = new List<string>();

            if (!string.IsNullOrEmpty(description))
                options.Add("DESCRIPTION = @Description");

            if (compress)
                options.Add("COMPRESSION");

            if (checksum)
                options.Add("CHECKSUM");

            options.Add("STATS = 10"); // Progress every 10%

            if (options.Count > 0)
            {
                if (!sql.EndsWith(" WITH"))
                    sql += ",";
                sql += " " + string.Join(", ", options);
            }

            return sql;
        }

        private async Task MonitorBackupProgressAsync(SqlConnection connection, string databaseName, 
            IProgress<int> progress, CancellationToken cancellationToken)
        {
            const string progressQuery = @"
                SELECT 
                    r.percent_complete
                FROM sys.dm_exec_requests r
                INNER JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id
                WHERE r.command LIKE '%BACKUP%'
                  AND s.is_user_process = 1
                ORDER BY r.start_time DESC";

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var progressCommand = new SqlCommand(progressQuery, connection);
                    var result = await progressCommand.ExecuteScalarAsync(cancellationToken);
                    
                    if (result != null && result != DBNull.Value)
                    {
                        var percentComplete = Convert.ToInt32(result);
                        progress.Report(percentComplete);
                        
                        if (percentComplete >= 100)
                            break;
                    }
                }
                catch (Exception)
                {
                    // Ignore progress monitoring errors
                }

                await Task.Delay(1000, cancellationToken);
            }
        }

        #endregion

        #region Restore Operations

        public async Task<bool> RestoreBackupAsync(
            string backupPath,
            string targetDatabaseName,
            RestoreOptions options = null,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(backupPath))
                throw new ArgumentException("Backup path is required", nameof(backupPath));

            if (string.IsNullOrWhiteSpace(targetDatabaseName))
                throw new ArgumentException("Target database name is required", nameof(targetDatabaseName));

            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"Backup file not found: {backupPath}");

            options ??= new RestoreOptions();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Verify backup first if requested
            if (options.Verify)
            {
                await VerifyBackupAsync(connection, backupPath, cancellationToken);
            }

            // Get file list from backup
            var fileList = await GetBackupFileListAsync(connection, backupPath, cancellationToken);

            // Build restore command
            var restoreCommand = BuildRestoreCommand(backupPath, targetDatabaseName, options, fileList);

            using var command = new SqlCommand(restoreCommand, connection)
            {
                CommandTimeout = 0 // No timeout for restore operations
            };

            command.Parameters.AddWithValue("@BackupPath", backupPath);

            if (!string.IsNullOrEmpty(options.NewDatabaseName))
                command.Parameters.AddWithValue("@DatabaseName", options.NewDatabaseName);

            // Start the restore in a separate task
            var restoreTask = command.ExecuteNonQueryAsync(cancellationToken);

            // Monitor progress if requested
            if (progress != null)
            {
                await MonitorRestoreProgressAsync(connection, targetDatabaseName, progress, cancellationToken);
            }

            await restoreTask;
            return true;
        }

        private async Task VerifyBackupAsync(SqlConnection connection, string backupPath, CancellationToken cancellationToken)
        {
            const string verifyCommand = "RESTORE VERIFYONLY FROM DISK = @BackupPath";
            
            using var command = new SqlCommand(verifyCommand, connection);
            command.Parameters.AddWithValue("@BackupPath", backupPath);
            
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<List<BackupFileInfo>> GetBackupFileListAsync(SqlConnection connection, string backupPath, CancellationToken cancellationToken)
        {
            const string fileListCommand = @"
                RESTORE FILELISTONLY 
                FROM DISK = @BackupPath";

            var fileList = new List<BackupFileInfo>();

            using var command = new SqlCommand(fileListCommand, connection);
            command.Parameters.AddWithValue("@BackupPath", backupPath);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                fileList.Add(new BackupFileInfo
                {
                    LogicalName = reader.GetString("LogicalName"),
                    PhysicalName = reader.GetString("PhysicalName"),
                    Type = reader.GetString("Type"),
                    FileGroupName = reader.IsDBNull("FileGroupName") ? string.Empty : reader.GetString("FileGroupName"),
                    Size = reader.GetInt64("Size"),
                    MaxSize = reader.GetInt64("MaxSize")
                });
            }

            return fileList;
        }

        private string BuildRestoreCommand(string backupPath, string targetDatabaseName, RestoreOptions options, List<BackupFileInfo> fileList)
        {
            var sql = $"RESTORE DATABASE [{targetDatabaseName}] FROM DISK = @BackupPath";

            var withOptions = new List<string>();

            // Add file relocations
            if (options.RelocateFiles.Count > 0 || fileList.Count > 0)
            {
                foreach (var file in fileList)
                {
                    if (options.RelocateFiles.TryGetValue(file.LogicalName, out var newPath))
                    {
                        withOptions.Add($"MOVE '{file.LogicalName}' TO '{newPath}'");
                    }
                }
            }

            if (options.Replace)
                withOptions.Add("REPLACE");

            if (!options.WithRecovery)
                withOptions.Add("NORECOVERY");

            if (options.StopAt.HasValue)
                withOptions.Add($"STOPAT = '{options.StopAt.Value:yyyy-MM-dd HH:mm:ss}'");

            if (!string.IsNullOrEmpty(options.StopAtMarkName))
                withOptions.Add($"STOPATMARK = '{options.StopAtMarkName}'");

            if (options.CheckSumEnabled)
                withOptions.Add("CHECKSUM");

            withOptions.Add("STATS = 10"); // Progress every 10%

            if (withOptions.Count > 0)
            {
                sql += " WITH " + string.Join(", ", withOptions);
            }

            return sql;
        }

        private async Task MonitorRestoreProgressAsync(SqlConnection connection, string databaseName, 
            IProgress<int> progress, CancellationToken cancellationToken)
        {
            const string progressQuery = @"
                SELECT 
                    r.percent_complete
                FROM sys.dm_exec_requests r
                INNER JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id
                WHERE r.command LIKE '%RESTORE%'
                  AND s.is_user_process = 1
                ORDER BY r.start_time DESC";

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var progressCommand = new SqlCommand(progressQuery, connection);
                    var result = await progressCommand.ExecuteScalarAsync(cancellationToken);
                    
                    if (result != null && result != DBNull.Value)
                    {
                        var percentComplete = Convert.ToInt32(result);
                        progress.Report(percentComplete);
                        
                        if (percentComplete >= 100)
                            break;
                    }
                }
                catch (Exception)
                {
                    // Ignore progress monitoring errors
                }

                await Task.Delay(1000, cancellationToken);
            }
        }

        #endregion

        #region Backup History

        public async Task<List<BackupHistory>> GetBackupHistoryAsync(string databaseName = null, int maxRecords = 100)
        {
            const string historyQuery = @"
                SELECT TOP (@MaxRecords)
                    bs.database_name,
                    bs.type,
                    bs.backup_start_date,
                    bs.backup_finish_date,
                    bmf.physical_device_name,
                    bs.backup_size,
                    bs.compressed_backup_size,
                    bs.user_name,
                    bs.server_name
                FROM msdb.dbo.backupset bs
                INNER JOIN msdb.dbo.backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
                WHERE (@DatabaseName IS NULL OR bs.database_name = @DatabaseName)
                ORDER BY bs.backup_start_date DESC";

            var history = new List<BackupHistory>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(historyQuery, connection);
            command.Parameters.AddWithValue("@DatabaseName", databaseName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MaxRecords", maxRecords);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var backupType = reader.GetString("type") switch
                {
                    "D" => BackupType.Full,
                    "I" => BackupType.Differential,
                    "L" => BackupType.Log,
                    _ => BackupType.Full
                };

                history.Add(new BackupHistory
                {
                    DatabaseName = reader.GetString("database_name"),
                    Type = backupType,
                    BackupStartDate = reader.GetDateTime("backup_start_date"),
                    BackupFinishDate = reader.GetDateTime("backup_finish_date"),
                    BackupPath = reader.GetString("physical_device_name"),
                    BackupSize = reader.GetInt64("backup_size"),
                    IsCompressed = !reader.IsDBNull("compressed_backup_size"),
                    UserName = reader.GetString("user_name"),
                    ServerName = reader.GetString("server_name")
                });
            }

            return history;
        }

        #endregion

        #region Utility Methods

        public async Task<List<string>> GetDatabaseListAsync()
        {
            const string query = @"
                SELECT name 
                FROM sys.databases 
                WHERE state = 0 
                  AND name NOT IN ('master', 'tempdb', 'model', 'msdb')
                ORDER BY name";

            var databases = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }

            return databases;
        }

        public static string GetDefaultBackupPath(string databaseName, BackupType type)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var extension = type == BackupType.Log ? ".trn" : ".bak";
            var typePrefix = type switch
            {
                BackupType.Full => "Full",
                BackupType.Differential => "Diff",
                BackupType.Log => "Log",
                _ => "Full"
            };

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SQL Backups",
                $"{databaseName}_{typePrefix}_{timestamp}{extension}"
            );
        }

        #endregion
    }

    public class BackupFileInfo
    {
        public string LogicalName { get; set; } = string.Empty;
        public string PhysicalName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string FileGroupName { get; set; } = string.Empty;
        public long Size { get; set; }
        public long MaxSize { get; set; }
    }
}
