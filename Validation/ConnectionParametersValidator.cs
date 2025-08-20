using FluentValidation;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace SqlServerManager.Validation
{
    /// <summary>
    /// Connection parameters for validation
    /// </summary>
    public class ConnectionParameters
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseWindowsAuthentication { get; set; }
        public bool TrustServerCertificate { get; set; }
        public int ConnectionTimeout { get; set; } = 15;
        public int CommandTimeout { get; set; } = 30;
    }

    /// <summary>
    /// Validator for SQL Server connection parameters
    /// </summary>
    public class ConnectionParametersValidator : AbstractValidator<ConnectionParameters>
    {
        public ConnectionParametersValidator()
        {
            RuleFor(x => x.Server)
                .NotEmpty().WithMessage("Server name is required")
                .MaximumLength(128).WithMessage("Server name cannot exceed 128 characters")
                .Must(BeValidServerName).WithMessage("Server name contains invalid characters");

            RuleFor(x => x.Database)
                .MaximumLength(128).WithMessage("Database name cannot exceed 128 characters")
                .Must(BeValidDatabaseName).WithMessage("Database name contains invalid characters")
                .When(x => !string.IsNullOrEmpty(x.Database));

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required when using SQL Server authentication")
                .MaximumLength(128).WithMessage("Username cannot exceed 128 characters")
                .When(x => !x.UseWindowsAuthentication);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required when using SQL Server authentication")
                .When(x => !x.UseWindowsAuthentication && !string.IsNullOrEmpty(x.Username));

            RuleFor(x => x.ConnectionTimeout)
                .GreaterThan(0).WithMessage("Connection timeout must be greater than 0")
                .LessThanOrEqualTo(300).WithMessage("Connection timeout cannot exceed 300 seconds");

            RuleFor(x => x.CommandTimeout)
                .GreaterThanOrEqualTo(0).WithMessage("Command timeout must be 0 or greater")
                .LessThanOrEqualTo(86400).WithMessage("Command timeout cannot exceed 24 hours");
        }

        /// <summary>
        /// Validate server name format
        /// </summary>
        private bool BeValidServerName(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
                return false;

            // Allow common server name formats
            var invalidChars = new[] { '<', '>', ':', '"', '|', '?', '*' };
            return !serverName.Any(c => invalidChars.Contains(c));
        }

        /// <summary>
        /// Validate database name format
        /// </summary>
        private bool BeValidDatabaseName(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                return true; // Optional field

            // SQL Server database name rules
            if (databaseName.Length > 128)
                return false;

            // Cannot start with space or certain characters
            if (databaseName.StartsWith(" ") || databaseName.EndsWith(" "))
                return false;

            var invalidChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            return !databaseName.Any(c => invalidChars.Contains(c));
        }
    }

    /// <summary>
    /// Database creation parameters
    /// </summary>
    public class DatabaseCreationParameters
    {
        public string DatabaseName { get; set; }
        public string InitialSize { get; set; }
        public string MaxSize { get; set; }
        public string GrowthSize { get; set; }
        public string Collation { get; set; }
    }

    /// <summary>
    /// Validator for database creation parameters
    /// </summary>
    public class DatabaseCreationValidator : AbstractValidator<DatabaseCreationParameters>
    {
        public DatabaseCreationValidator()
        {
            RuleFor(x => x.DatabaseName)
                .NotEmpty().WithMessage("Database name is required")
                .MaximumLength(128).WithMessage("Database name cannot exceed 128 characters")
                .Must(BeValidDatabaseName).WithMessage("Database name contains invalid characters or format")
                .Must(NotBeReservedName).WithMessage("Database name cannot be a reserved word");

            RuleFor(x => x.InitialSize)
                .Must(BeValidSize).WithMessage("Initial size must be a valid size (e.g., 100MB, 1GB)")
                .When(x => !string.IsNullOrEmpty(x.InitialSize));

            RuleFor(x => x.MaxSize)
                .Must(BeValidSize).WithMessage("Maximum size must be a valid size (e.g., 1GB, UNLIMITED)")
                .When(x => !string.IsNullOrEmpty(x.MaxSize));

            RuleFor(x => x.GrowthSize)
                .Must(BeValidGrowth).WithMessage("Growth size must be a valid size or percentage (e.g., 10MB, 10%)")
                .When(x => !string.IsNullOrEmpty(x.GrowthSize));

            RuleFor(x => x.Collation)
                .Must(BeValidCollation).WithMessage("Invalid collation name")
                .When(x => !string.IsNullOrEmpty(x.Collation));
        }

        private bool BeValidDatabaseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Cannot start with numbers, spaces, or special characters
            if (char.IsDigit(name[0]) || char.IsWhiteSpace(name[0]))
                return false;

            // Check for invalid characters
            var invalidChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '[', ']' };
            if (name.Any(c => invalidChars.Contains(c)))
                return false;

            // Cannot contain only periods
            if (name.All(c => c == '.'))
                return false;

            return true;
        }

        private bool NotBeReservedName(string name)
        {
            var reservedNames = new[] { "master", "model", "msdb", "tempdb", "CON", "PRN", "AUX", "NUL" };
            return !reservedNames.Contains(name?.ToUpper());
        }

        private bool BeValidSize(string size)
        {
            if (string.IsNullOrWhiteSpace(size))
                return true;

            if (size.Equals("UNLIMITED", System.StringComparison.OrdinalIgnoreCase))
                return true;

            // Check format: number + unit (KB, MB, GB, TB)
            var pattern = @"^\d+(\.\d+)?\s*(KB|MB|GB|TB)$";
            return System.Text.RegularExpressions.Regex.IsMatch(size, pattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private bool BeValidGrowth(string growth)
        {
            if (string.IsNullOrWhiteSpace(growth))
                return true;

            // Check for percentage format
            if (growth.EndsWith("%"))
            {
                var percentStr = growth.Substring(0, growth.Length - 1);
                if (int.TryParse(percentStr, out int percent))
                {
                    return percent > 0 && percent <= 100;
                }
            }

            // Check for size format
            return BeValidSize(growth);
        }

        private bool BeValidCollation(string collation)
        {
            if (string.IsNullOrWhiteSpace(collation))
                return true;

            // Basic collation name validation - should start with a letter and contain valid characters
            var pattern = @"^[a-zA-Z][a-zA-Z0-9_]*$";
            return System.Text.RegularExpressions.Regex.IsMatch(collation, pattern);
        }
    }

    /// <summary>
    /// SQL query parameters for validation
    /// </summary>
    public class SqlQueryParameters
    {
        public string Query { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Validator for SQL query parameters
    /// </summary>
    public class SqlQueryValidator : AbstractValidator<SqlQueryParameters>
    {
        public SqlQueryValidator()
        {
            RuleFor(x => x.Query)
                .NotEmpty().WithMessage("SQL query cannot be empty")
                .MaximumLength(50000).WithMessage("Query is too long (maximum 50,000 characters)")
                .Must(NotContainDangerousOperations).WithMessage("Query contains potentially dangerous operations");

            RuleFor(x => x.TimeoutSeconds)
                .GreaterThan(0).WithMessage("Query timeout must be greater than 0")
                .LessThanOrEqualTo(3600).WithMessage("Query timeout cannot exceed 1 hour");
        }

        private bool NotContainDangerousOperations(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            var normalizedQuery = query.ToUpperInvariant();

            // Check for dangerous operations (basic check - not foolproof)
            var dangerousPatterns = new[]
            {
                "SHUTDOWN",
                "DBCC",
                "xp_cmdshell",
                "sp_configure",
                "BULK INSERT",
                "OPENROWSET",
                "OPENDATASOURCE"
            };

            return !dangerousPatterns.Any(pattern => normalizedQuery.Contains(pattern));
        }
    }
}
