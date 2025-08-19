using System;
using System.IO;
using System.Windows.Forms;

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
                // Log startup
                LogToFile("Application starting...");
                
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Set up global exception handling
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                
                LogToFile("Creating MainForm...");
                using (var mainForm = new MainForm())
                {
                    LogToFile("Running application...");
                    Application.Run(mainForm);
                }
                
                LogToFile("Application ended normally.");
            }
            catch (Exception ex)
            {
                LogToFile($"Fatal error in Main: {ex}");
                MessageBox.Show($"Fatal error: {ex.Message}\n\nSee error.log for details.", 
                    "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            LogToFile($"Thread exception: {e.Exception}");
            MessageBox.Show($"Thread error: {e.Exception.Message}\n\nSee error.log for details.", 
                "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogToFile($"Unhandled exception: {e.ExceptionObject}");
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Unhandled error: {ex.Message}\n\nSee error.log for details.", 
                    "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
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
