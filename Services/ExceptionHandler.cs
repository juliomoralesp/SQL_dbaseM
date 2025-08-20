using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace SqlServerManager.Services
{
    /// <summary>
    /// Centralized exception handling service with proper logging and user-friendly messages
    /// </summary>
    public static class ExceptionHandler
    {
        /// <summary>
        /// Handle exceptions with appropriate logging and user notifications
        /// </summary>
        public static void Handle(Exception ex, string operation, Form parentForm = null)
        {
            // Log the full exception details
            LoggingService.LogError(ex, "Operation failed: {Operation}", operation);

            // Determine user-friendly message based on exception type
            var userMessage = GetUserFriendlyMessage(ex, operation);

            // Show appropriate message to user
            ShowUserMessage(userMessage, GetMessageIcon(ex), parentForm);
        }

        /// <summary>
        /// Handle exceptions and return whether the operation should be retried
        /// </summary>
        public static bool HandleWithRetry(Exception ex, string operation, int attemptNumber, Form parentForm = null)
        {
            LoggingService.LogWarning("Operation attempt {Attempt} failed: {Operation} - {Error}", attemptNumber, operation, ex.Message);

            if (IsRetryableException(ex))
            {
                if (attemptNumber < 3) // Max 3 attempts
                {
                    LoggingService.LogInformation("Will retry operation: {Operation}", operation);
                    return true; // Retry
                }
                else
                {
                    // Max retries reached
                    Handle(ex, $"{operation} (after {attemptNumber} attempts)", parentForm);
                    return false;
                }
            }
            else
            {
                // Non-retryable error
                Handle(ex, operation, parentForm);
                return false;
            }
        }

        /// <summary>
        /// Get user-friendly message based on exception type
        /// </summary>
        private static string GetUserFriendlyMessage(Exception ex, string operation)
        {
            return ex switch
            {
                SqlException sqlEx => GetSqlExceptionMessage(sqlEx, operation),
                UnauthorizedAccessException => $"Access denied during {operation}. Please check your permissions.",
                TimeoutException => $"The {operation} operation timed out. Please try again or check your connection.",
                ArgumentException argEx => $"Invalid input provided for {operation}: {GetSafeMessage(argEx.Message)}",
                InvalidOperationException => $"Cannot perform {operation} in the current state.",
                NotSupportedException => $"The {operation} operation is not supported.",
                OutOfMemoryException => $"Not enough memory to complete {operation}. Try closing other applications.",
                System.IO.IOException => $"File or network error during {operation}. Please check your connection and try again.",
                _ => $"An unexpected error occurred during {operation}. Please try again or contact support if the problem persists."
            };
        }

        /// <summary>
        /// Get user-friendly message for SQL exceptions
        /// </summary>
        private static string GetSqlExceptionMessage(SqlException sqlEx, string operation)
        {
            return sqlEx.Number switch
            {
                2 => $"Cannot connect to SQL Server. Please check the server name and ensure SQL Server is running.",
                18 => "Login failed. Please check your username and password.",
                4060 => "The specified database could not be opened. Please verify the database name.",
                1205 => $"Database deadlock detected during {operation}. Please try again.",
                2 or 53 => "Network connection to SQL Server failed. Please check your network connection.",
                18456 => "Authentication failed. Please check your login credentials.",
                515 => "Cannot insert NULL value into a required field.",
                547 => "Foreign key constraint violation. Please check related data.",
                2627 or 2601 => "Duplicate key violation. The record already exists.",
                8152 => "String data would be truncated. Please check field lengths.",
                _ => "Database error occurred. Please check the error details and try again."
            };
        }

        /// <summary>
        /// Determine if an exception is retryable
        /// </summary>
        private static bool IsRetryableException(Exception ex)
        {
            return ex switch
            {
                SqlException sqlEx => IsRetryableSqlException(sqlEx),
                TimeoutException => true,
                System.IO.IOException => true,
                _ => false
            };
        }

        /// <summary>
        /// Determine if a SQL exception is retryable
        /// </summary>
        private static bool IsRetryableSqlException(SqlException sqlEx)
        {
            // Transient SQL error codes that are typically retryable
            return sqlEx.Number switch
            {
                1205 => true, // Deadlock
                1222 => true, // Lock request timeout
                8645 => true, // Memory/resource wait timeout
                8651 => true, // Low memory condition
                40197 => true, // Service unavailable
                40501 => true, // Service busy
                40613 => true, // Database unavailable
                49918 => true, // Cannot process request
                49919 => true, // Cannot process create/update request
                49920 => true, // Cannot process request
                _ => false
            };
        }

        /// <summary>
        /// Get appropriate message box icon based on exception type
        /// </summary>
        private static MessageBoxIcon GetMessageIcon(Exception ex)
        {
            return ex switch
            {
                SqlException sqlEx when sqlEx.Class >= 20 => MessageBoxIcon.Error,
                SqlException => MessageBoxIcon.Warning,
                ArgumentException or InvalidOperationException => MessageBoxIcon.Warning,
                UnauthorizedAccessException => MessageBoxIcon.Warning,
                _ => MessageBoxIcon.Error
            };
        }

        /// <summary>
        /// Show user message with proper parent form
        /// </summary>
        private static void ShowUserMessage(string message, MessageBoxIcon icon, Form parentForm)
        {
            var title = icon switch
            {
                MessageBoxIcon.Error => "Error",
                MessageBoxIcon.Warning => "Warning",
                MessageBoxIcon.Information => "Information",
                _ => "Notification"
            };

            try
            {
                if (parentForm?.InvokeRequired == true)
                {
                    parentForm.Invoke(() => MessageBox.Show(parentForm, message, title, MessageBoxButtons.OK, icon));
                }
                else
                {
                    MessageBox.Show(parentForm, message, title, MessageBoxButtons.OK, icon);
                }
            }
            catch
            {
                // Fallback if UI thread access fails
                MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
            }
        }

        /// <summary>
        /// Get safe message text (removes sensitive information)
        /// </summary>
        private static string GetSafeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "No details available.";

            // Remove potential sensitive information
            var safeMessage = message;
            
            // Remove connection strings or passwords
            safeMessage = System.Text.RegularExpressions.Regex.Replace(safeMessage, 
                @"(password|pwd)\s*=\s*[^;]+", "password=***", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Truncate very long messages
            if (safeMessage.Length > 500)
                safeMessage = safeMessage.Substring(0, 497) + "...";

            return safeMessage;
        }

        /// <summary>
        /// Handle database operation exceptions with specific context
        /// </summary>
        public static void HandleDatabaseException(Exception ex, string database, string table, string operation, Form parentForm = null)
        {
            var context = string.IsNullOrEmpty(table) 
                ? $"{operation} on database '{database}'" 
                : $"{operation} on table '{database}.{table}'";
                
            LoggingService.LogError(ex, "Database operation failed: {Operation} on {Database}.{Table}", 
                operation, database ?? "Unknown", table ?? "Unknown");

            Handle(ex, context, parentForm);
        }

        /// <summary>
        /// Handle validation exceptions
        /// </summary>
        public static void HandleValidationException(FluentValidation.ValidationException validationEx, Form parentForm = null)
        {
            var errors = string.Join(Environment.NewLine, validationEx.Errors.Select(e => $"• {e.ErrorMessage}"));
            
            LoggingService.LogWarning("Validation failed: {Errors}", errors);
            
            ShowUserMessage($"Please correct the following errors:{Environment.NewLine}{errors}", 
                MessageBoxIcon.Warning, parentForm);
        }
    }
}
