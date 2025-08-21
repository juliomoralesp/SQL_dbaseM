using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlServerManager.Core.Security
{
    /// <summary>
    /// Provides SQL validation and sanitization utilities to prevent SQL injection
    /// </summary>
    public static class SqlValidation
    {
        // Dangerous SQL keywords that could indicate injection attempts
        private static readonly string[] DangerousKeywords = new[]
        {
            "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "EXEC", "EXECUTE",
            "XP_", "SP_", "SHUTDOWN", "GRANT", "REVOKE", "--", "/*", "*/", ";"
        };

        // Valid identifier pattern (alphanumeric and underscore only)
        private static readonly Regex ValidIdentifierPattern = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        
        // Valid schema.table pattern
        private static readonly Regex ValidSchemaTablePattern = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        /// Validates a database/table/column identifier
        /// </summary>
        public static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Check length (SQL Server max identifier length is 128)
            if (identifier.Length > 128)
                return false;

            // Remove brackets if present
            var cleanIdentifier = identifier.Trim('[', ']');
            
            // Check if it matches valid pattern
            return ValidIdentifierPattern.IsMatch(cleanIdentifier);
        }

        /// <summary>
        /// Validates a schema.table identifier
        /// </summary>
        public static bool IsValidSchemaTable(string schemaTable)
        {
            if (string.IsNullOrWhiteSpace(schemaTable))
                return false;

            // Remove brackets if present
            var cleanIdentifier = schemaTable.Replace("[", "").Replace("]", "");
            
            return ValidSchemaTablePattern.IsMatch(cleanIdentifier);
        }

        /// <summary>
        /// Escapes an identifier for use in SQL queries
        /// </summary>
        public static string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentNullException(nameof(identifier));

            // Remove existing brackets
            var clean = identifier.Trim('[', ']');
            
            // Validate
            if (!IsValidIdentifier(clean))
                throw new ArgumentException($"Invalid identifier: {identifier}");

            // Return properly escaped
            return $"[{clean}]";
        }

        /// <summary>
        /// Escapes a string value for use in SQL queries
        /// </summary>
        public static string EscapeStringValue(string value)
        {
            if (value == null)
                return "NULL";

            // Escape single quotes by doubling them
            var escaped = value.Replace("'", "''");
            
            return $"'{escaped}'";
        }

        /// <summary>
        /// Checks if a SQL query contains potentially dangerous operations
        /// </summary>
        public static bool ContainsDangerousSQL(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return false;

            var upperSql = sql.ToUpper();
            
            // Check for dangerous keywords
            foreach (var keyword in DangerousKeywords)
            {
                if (upperSql.Contains(keyword))
                {
                    // Allow SELECT statements with semicolons (for multiple queries)
                    if (keyword == ";" && upperSql.StartsWith("SELECT"))
                        continue;
                        
                    // Allow comments in SELECT statements
                    if ((keyword == "--" || keyword == "/*" || keyword == "*/") && upperSql.StartsWith("SELECT"))
                        continue;
                        
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validates a WHERE clause for safety
        /// </summary>
        public static bool IsValidWhereClause(string whereClause)
        {
            if (string.IsNullOrWhiteSpace(whereClause))
                return true;

            // Check for SQL injection patterns
            var patterns = new[]
            {
                @"'.*OR.*'='",     // Classic OR '1'='1' injection
                @"'.*AND.*'='",    // Classic AND injection
                @"UNION\s+SELECT", // UNION injection
                @";\s*DROP",       // Command chaining
                @";\s*DELETE",     // Command chaining
                @";\s*UPDATE",     // Command chaining
                @"--\s*$",         // Comment at end
                @"/\*.*\*/",       // Block comments
            };

            var upperClause = whereClause.ToUpper();
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(upperClause, pattern))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sanitizes user input for use in LIKE clauses
        /// </summary>
        public static string SanitizeForLike(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Escape special LIKE characters
            return input
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]")
                .Replace("'", "''");
        }

        /// <summary>
        /// Validates a batch size value
        /// </summary>
        public static bool IsValidBatchSize(int batchSize)
        {
            return batchSize > 0 && batchSize <= 10000;
        }

        /// <summary>
        /// Validates a connection string for safety
        /// </summary>
        public static bool IsValidConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // Check for required components
            var requiredKeywords = new[] { "server", "database" };
            var lowerConnStr = connectionString.ToLower();
            
            foreach (var keyword in requiredKeywords)
            {
                if (!lowerConnStr.Contains(keyword))
                    return false;
            }

            // Check for dangerous keywords
            var dangerousPatterns = new[] { "xp_", "sp_", "cmd", "shell" };
            foreach (var pattern in dangerousPatterns)
            {
                if (lowerConnStr.Contains(pattern))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Generates a safe parameter name from a column name
        /// </summary>
        public static string GenerateParameterName(string columnName, string prefix = "@")
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return $"{prefix}param";

            // Remove invalid characters and ensure it starts with a letter or underscore
            var clean = Regex.Replace(columnName, @"[^a-zA-Z0-9_]", "_");
            
            if (clean.Length > 0 && !char.IsLetter(clean[0]) && clean[0] != '_')
                clean = "_" + clean;
                
            return $"{prefix}{clean}";
        }

        /// <summary>
        /// Validates a file path for import/export operations
        /// </summary>
        public static bool IsValidFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                // Check for invalid characters
                var invalidChars = System.IO.Path.GetInvalidPathChars();
                if (filePath.Any(c => invalidChars.Contains(c)))
                    return false;

                // Check for dangerous patterns
                var dangerousPatterns = new[] { "..", "~", "|", "<", ">" };
                foreach (var pattern in dangerousPatterns)
                {
                    if (filePath.Contains(pattern))
                        return false;
                }

                // Validate extension for common formats
                var validExtensions = new[] { ".csv", ".xlsx", ".json", ".xml", ".sql", ".txt", ".bak" };
                var extension = System.IO.Path.GetExtension(filePath)?.ToLower();
                
                if (!string.IsNullOrEmpty(extension) && !validExtensions.Contains(extension))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Builds a safe connection string from components
        /// </summary>
        public static string BuildSafeConnectionString(string server, string database, bool integratedSecurity, string username = null, string password = null)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder();
            
            // Validate and set server
            if (!IsValidIdentifier(server.Replace("\\", "_").Replace(".", "_")))
                throw new ArgumentException("Invalid server name");
            builder.DataSource = server;
            
            // Validate and set database
            if (!IsValidIdentifier(database))
                throw new ArgumentException("Invalid database name");
            builder.InitialCatalog = database;
            
            // Set authentication
            builder.IntegratedSecurity = integratedSecurity;
            
            if (!integratedSecurity)
            {
                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentException("Username is required for SQL authentication");
                    
                builder.UserID = username;
                builder.Password = password ?? string.Empty;
            }
            
            // Set reasonable defaults for safety
            builder.ConnectTimeout = 30;
            builder.CommandTimeout = 300;
            builder.TrustServerCertificate = false;
            builder.Encrypt = true;
            
            return builder.ConnectionString;
        }
    }
}
