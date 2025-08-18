using SqlServerManager.Mobile.Models;
using System.Data;
using System.Data.SqlClient;

namespace SqlServerManager.Mobile.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConnectionService _connectionService;

        public DatabaseService()
        {
            _connectionService = new ConnectionService();
        }

        public DatabaseService(IConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public async Task<List<DatabaseInfo>> GetDatabasesAsync()
        {
            var databases = new List<DatabaseInfo>();
            
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            try
            {
                using var command = new SqlCommand(@"
                    SELECT 
                        name,
                        SUSER_SNAME(owner_sid) as Owner,
                        create_date as CreatedDate,
                        collation_name as Collation,
                        state_desc as Status
                    FROM sys.databases
                    WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')
                    ORDER BY name", _connectionService.Connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    databases.Add(new DatabaseInfo
                    {
                        Name = reader.GetString(0),
                        Owner = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1),
                        CreatedDate = reader.GetDateTime(2),
                        Collation = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Status = reader.GetString(4)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting databases: {ex.Message}");
                throw;
            }

            return databases;
        }

        public async Task<List<TableInfo>> GetTablesAsync(string databaseName)
        {
            var tables = new List<TableInfo>();
            
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            try
            {
                using var command = new SqlCommand($@"
                    USE [{databaseName}];
                    SELECT 
                        TABLE_SCHEMA,
                        TABLE_NAME,
                        TABLE_TYPE
                    FROM INFORMATION_SCHEMA.TABLES
                    ORDER BY TABLE_SCHEMA, TABLE_NAME", _connectionService.Connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(new TableInfo
                    {
                        TableSchema = reader.GetString(0),
                        TableName = reader.GetString(1),
                        TableType = reader.GetString(2)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting tables: {ex.Message}");
                throw;
            }

            return tables;
        }

        public async Task<List<ColumnInfo>> GetColumnsAsync(string databaseName, string tableName)
        {
            var columns = new List<ColumnInfo>();
            
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            try
            {
                using var command = new SqlCommand($@"
                    USE [{databaseName}];
                    SELECT 
                        COLUMN_NAME,
                        ORDINAL_POSITION,
                        DATA_TYPE,
                        CHARACTER_MAXIMUM_LENGTH,
                        NUMERIC_PRECISION,
                        NUMERIC_SCALE,
                        IS_NULLABLE,
                        COLUMN_DEFAULT
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @tableName
                    ORDER BY ORDINAL_POSITION", _connectionService.Connection);

                command.Parameters.AddWithValue("@tableName", tableName);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(new ColumnInfo
                    {
                        ColumnName = reader.GetString(0),
                        OrdinalPosition = reader.GetInt32(1),
                        DataType = reader.GetString(2),
                        MaxLength = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        NumericPrecision = reader.IsDBNull(4) ? null : reader.GetByte(4),
                        NumericScale = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        IsNullable = reader.GetString(6),
                        DefaultValue = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting columns: {ex.Message}");
                throw;
            }

            return columns;
        }

        public async Task<DatabaseProperties> GetDatabasePropertiesAsync(string databaseName)
        {
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            var properties = new DatabaseProperties { Name = databaseName };

            try
            {
                using var command = new SqlCommand($@"
                    SELECT 
                        SUSER_SNAME(owner_sid) as Owner,
                        create_date as CreatedDate,
                        collation_name as Collation,
                        recovery_model_desc as RecoveryModel,
                        state_desc as State
                    FROM sys.databases
                    WHERE name = @dbName", _connectionService.Connection);

                command.Parameters.AddWithValue("@dbName", databaseName);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    properties.Owner = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
                    properties.CreatedDate = reader.GetDateTime(1);
                    properties.Collation = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    properties.RecoveryModel = reader.GetString(3);
                    properties.State = reader.GetString(4);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting database properties: {ex.Message}");
                throw;
            }

            return properties;
        }

        public async Task<bool> CreateDatabaseAsync(string databaseName)
        {
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            try
            {
                using var command = new SqlCommand($"CREATE DATABASE [{databaseName}]", _connectionService.Connection);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating database: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteDatabaseAsync(string databaseName)
        {
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            try
            {
                using var command = new SqlCommand($@"
                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{databaseName}]", _connectionService.Connection);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting database: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RenameDatabaseAsync(string oldName, string newName)
        {
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            try
            {
                using var command = new SqlCommand($@"
                    ALTER DATABASE [{oldName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    ALTER DATABASE [{oldName}] MODIFY NAME = [{newName}];
                    ALTER DATABASE [{newName}] SET MULTI_USER;", _connectionService.Connection);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error renaming database: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateTableAsync(string databaseName, string tableName, List<ColumnInfo> columns)
        {
            // Implementation for creating tables
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteTableAsync(string databaseName, string tableName)
        {
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            try
            {
                using var command = new SqlCommand($@"
                    USE [{databaseName}];
                    DROP TABLE [{tableName}]", _connectionService.Connection);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting table: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RenameTableAsync(string databaseName, string oldName, string newName)
        {
            if (!_connectionService.IsConnected)
                throw new InvalidOperationException("Not connected to SQL Server");

            try
            {
                using var command = new SqlCommand($@"
                    USE [{databaseName}];
                    EXEC sp_rename '{oldName}', '{newName}'", _connectionService.Connection);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error renaming table: {ex.Message}");
                return false;
            }
        }
    }
}
