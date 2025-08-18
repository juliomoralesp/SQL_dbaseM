using SqlServerManager.Mobile.Models;
using System.Data.SqlClient;

namespace SqlServerManager.Mobile.Services
{
    public interface IConnectionService
    {
        bool IsConnected { get; }
        string ConnectionString { get; }
        SqlConnection Connection { get; }
        
        Task<bool> ConnectAsync(string connectionString);
        Task<bool> TestConnectionAsync(string connectionString);
        void Disconnect();
        
        Task<List<ConnectionInfo>> GetSavedConnectionsAsync();
        Task SaveConnectionAsync(ConnectionInfo connection);
        Task RemoveSavedConnectionAsync(string id);
    }
}
