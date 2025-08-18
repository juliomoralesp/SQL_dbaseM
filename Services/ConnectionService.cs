using SqlServerManager.Mobile.Models;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace SqlServerManager.Mobile.Services
{
    public class ConnectionService : IConnectionService
    {
        private SqlConnection _connection;
        private string _connectionString;
        private readonly string _savedConnectionsFile;

        public ConnectionService()
        {
            _savedConnectionsFile = Path.Combine(FileSystem.AppDataDirectory, "saved_connections.json");
        }

        public bool IsConnected => _connection?.State == System.Data.ConnectionState.Open;
        public string ConnectionString => _connectionString;
        public SqlConnection Connection => _connection;

        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                Disconnect(); // Close any existing connection
                
                _connection = new SqlConnection(connectionString);
                await _connection.OpenAsync();
                _connectionString = connectionString;
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
                Disconnect();
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                using var testConnection = new SqlConnection(connectionString);
                await testConnection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Test connection error: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_connection != null)
                {
                    if (_connection.State != System.Data.ConnectionState.Closed)
                    {
                        _connection.Close();
                    }
                    _connection.Dispose();
                    _connection = null;
                }
                _connectionString = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
            }
        }

        public async Task<List<ConnectionInfo>> GetSavedConnectionsAsync()
        {
            try
            {
                if (File.Exists(_savedConnectionsFile))
                {
                    var json = await File.ReadAllTextAsync(_savedConnectionsFile);
                    return JsonSerializer.Deserialize<List<ConnectionInfo>>(json) ?? new List<ConnectionInfo>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saved connections: {ex.Message}");
            }
            
            return new List<ConnectionInfo>();
        }

        public async Task SaveConnectionAsync(ConnectionInfo connection)
        {
            try
            {
                var connections = await GetSavedConnectionsAsync();
                
                // Remove existing connection with same server and auth type
                connections.RemoveAll(c => c.ServerName == connection.ServerName && 
                                          c.AuthType == connection.AuthType &&
                                          c.Username == connection.Username);
                
                // Add new connection at the beginning
                connections.Insert(0, connection);
                
                // Keep only last 10 connections
                if (connections.Count > 10)
                {
                    connections = connections.Take(10).ToList();
                }
                
                var json = JsonSerializer.Serialize(connections, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_savedConnectionsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving connection: {ex.Message}");
            }
        }

        public async Task RemoveSavedConnectionAsync(string id)
        {
            try
            {
                var connections = await GetSavedConnectionsAsync();
                connections.RemoveAll(c => c.Id == id);
                
                var json = JsonSerializer.Serialize(connections, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_savedConnectionsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing connection: {ex.Message}");
            }
        }
    }
}
