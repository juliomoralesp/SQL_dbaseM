using System;
using System.IO;
using System.Windows.Forms;
using SqlServerManager.Services;

namespace SqlServerManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Initialize configuration first
                ConfigurationService.Initialize();
                
                // Initialize logging with configuration
                LoggingService.Initialize();
                LoggingService.LogInformation("=== SQL Server Manager Starting ===");
                LoggingService.LogInformation("OS: {OS}, CLR: {CLR}, User: {User}", 
                    Environment.OSVersion, Environment.Version, Environment.UserName);
            
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                LoggingService.LogInformation("Application configuration initialized");
                
                // Set up global exception handling
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                
                LoggingService.LogInformation("Creating and starting MainForm");
                using (var mainForm = new MainForm())
                {
                    Application.Run(mainForm);
                }
                
                LoggingService.LogInformation("Application ended normally");
            }
            catch (Exception ex)
            {
                LoggingService.LogFatal(ex, "Fatal error during application execution");
                
                // Use centralized exception handler
                ExceptionHandler.Handle(ex, "application startup");
                
                // Fallback error logging to file for compatibility
                LogToFile($"Fatal error in Main: {ex}");
            }
            finally
            {
                LoggingService.LogInformation("=== SQL Server Manager Shutting Down ===");
                LoggingService.Shutdown();
            }
        }
        
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            LoggingService.LogError(e.Exception, "Unhandled thread exception occurred");
            LogToFile($"Thread exception: {e.Exception}");
            
            // Use centralized exception handler
            ExceptionHandler.Handle(e.Exception, "thread operation");
        }
        
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LoggingService.LogFatal(e.ExceptionObject as Exception, 
                "Unhandled domain exception, IsTerminating: {IsTerminating}", e.IsTerminating);
            LogToFile($"Unhandled exception: {e.ExceptionObject}");
            
            if (e.ExceptionObject is Exception ex)
            {
                // Use centralized exception handler for critical errors
                ExceptionHandler.Handle(ex, "critical system operation");
            }
        }
        
        // Keep the old logging method for compatibility
        private static void LogToFile(string message)
        {
            try
            {
                string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch
            {
                // Ignore logging errors to prevent infinite loops
            }
        }
    }
}
