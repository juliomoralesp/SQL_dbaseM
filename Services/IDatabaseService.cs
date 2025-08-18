using SqlServerManager.Mobile.Models;

namespace SqlServerManager.Mobile.Services
{
    public interface IDatabaseService
    {
        Task<List<DatabaseInfo>> GetDatabasesAsync();
        Task<List<TableInfo>> GetTablesAsync(string databaseName);
        Task<List<ColumnInfo>> GetColumnsAsync(string databaseName, string tableName);
        Task<DatabaseProperties> GetDatabasePropertiesAsync(string databaseName);
        
        Task<bool> CreateDatabaseAsync(string databaseName);
        Task<bool> DeleteDatabaseAsync(string databaseName);
        Task<bool> RenameDatabaseAsync(string oldName, string newName);
        
        Task<bool> CreateTableAsync(string databaseName, string tableName, List<ColumnInfo> columns);
        Task<bool> DeleteTableAsync(string databaseName, string tableName);
        Task<bool> RenameTableAsync(string databaseName, string oldName, string newName);
    }
}
