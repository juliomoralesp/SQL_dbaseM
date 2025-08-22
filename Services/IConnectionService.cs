using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SqlServerManager.Services
{
    /// <summary>
    /// Interface for connection services to provide database connectivity
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>
        /// Gets whether the service is currently connected
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Gets the current database name
        /// </summary>
        string CurrentDatabase { get; }
        
        /// <summary>
        /// Execute an operation with a database connection that returns a value
        /// </summary>
        Task<T> ExecuteWithConnectionAsync<T>(Func<SqlConnection, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Execute an operation with a database connection that doesn't return a value
        /// </summary>
        Task ExecuteWithConnectionAsync(Func<SqlConnection, CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    }
}
