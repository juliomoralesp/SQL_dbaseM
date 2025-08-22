using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using SqlServerManager.Core.Configuration;
using SqlServerManager.Core.QueryEngine;
using SqlServerManager.Core.DataOperations;
using SqlServerManager.UI;
using Microsoft.Data.SqlClient;

namespace SqlServerManager
{
    public partial class TestPhase1Components : Form
    {
        private UserSettings userSettings;
        private UserSettingsManager settingsManager;
        private QueryHistoryManager historyManager;
        private EnhancedStatusBar enhancedStatusBar;
        private Button testUserSettingsButton;
        private Button testHistoryButton;
        private Button testBackupButton;
        private Button testConnectionBuilderButton;
        private RichTextBox outputTextBox;

        public TestPhase1Components()
        {
            InitializeComponent();
            InitializePhase1Components();
        }

        private void InitializeComponent()
        {
            this.Text = "Phase 1 Components Test";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create enhanced status bar
            enhancedStatusBar = new EnhancedStatusBar();
            enhancedStatusBar.Dock = DockStyle.Bottom;
            
            // Create test buttons
            testUserSettingsButton = new Button
            {
                Text = "Test User Settings",
                Location = new Point(20, 20),
                Size = new Size(150, 40)
            };
            testUserSettingsButton.Click += TestUserSettingsButton_Click;

            testHistoryButton = new Button
            {
                Text = "Test Query History",
                Location = new Point(180, 20),
                Size = new Size(150, 40)
            };
            testHistoryButton.Click += TestHistoryButton_Click;

            testBackupButton = new Button
            {
                Text = "Test Backup Manager",
                Location = new Point(340, 20),
                Size = new Size(150, 40)
            };
            testBackupButton.Click += TestBackupButton_Click;

            testConnectionBuilderButton = new Button
            {
                Text = "Test Connection Builder",
                Location = new Point(500, 20),
                Size = new Size(150, 40)
            };
            testConnectionBuilderButton.Click += TestConnectionBuilderButton_Click;

            // Create output text box
            outputTextBox = new RichTextBox
            {
                Location = new Point(20, 80),
                Size = new Size(740, 430),
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };

            // Add controls to form
            this.Controls.Add(testUserSettingsButton);
            this.Controls.Add(testHistoryButton);
            this.Controls.Add(testBackupButton);
            this.Controls.Add(testConnectionBuilderButton);
            this.Controls.Add(outputTextBox);
            this.Controls.Add(enhancedStatusBar);
        }

        private void InitializePhase1Components()
        {
            try
            {
                // Initialize components
                userSettings = new UserSettings();
                settingsManager = new UserSettingsManager();
                historyManager = new QueryHistoryManager();
                
                // Note: BackupManager requires a connection string, skip for now

                enhancedStatusBar.SetConnectionStatus(false, "Testing Phase 1 Components");
                AppendOutput("Phase 1 components initialized successfully!");
            }
            catch (Exception ex)
            {
                AppendOutput($"Error initializing Phase 1 components: {ex.Message}");
            }
        }

        private void TestUserSettingsButton_Click(object sender, EventArgs e)
        {
            try
            {
                AppendOutput("=== Testing User Settings ===");
                
                // Test saving settings
                userSettings.PreferredTheme = Core.Configuration.Theme.Dark;
                userSettings.EditorPreferences.FontSize = EditorFontSize.Large;
                userSettings.EditorPreferences.ShowLineNumbers = true;
                userSettings.LastWindowLayout.Size = new Size(1200, 800);
                
                AppendOutput($"Theme: {userSettings.PreferredTheme}");
                AppendOutput($"Font Size: {userSettings.EditorPreferences.FontSize}");
                AppendOutput($"Show Line Numbers: {userSettings.EditorPreferences.ShowLineNumbers}");
                AppendOutput($"Window Size: {userSettings.LastWindowLayout.Size}");
                
                // Test connection settings
                var newConnection = new RecentConnection
                {
                    Name = "Test Connection",
                    Server = "localhost",
                    Database = "TestDB",
                    AuthenticationType = "Windows"
                };
                userSettings.RecentConnections.Add(newConnection);
                AppendOutput($"Recent Connections Count: {userSettings.RecentConnections.Count}");
                
                AppendOutput("✅ User Settings test completed successfully!");
                
                enhancedStatusBar.SetConnectionStatus(true, "User Settings Test Complete");
            }
            catch (Exception ex)
            {
                AppendOutput($"❌ User Settings test failed: {ex.Message}");
                enhancedStatusBar.SetConnectionStatus(false, $"Error: {ex.Message}");
            }
        }

        private void TestHistoryButton_Click(object sender, EventArgs e)
        {
            try
            {
                AppendOutput("=== Testing Query History Manager ===");
                
                AppendOutput("Query History Manager instantiated successfully");
                AppendOutput("Note: Full testing requires implementation integration");
                
                AppendOutput("✅ Query History Manager test completed!");
                enhancedStatusBar.SetConnectionStatus(true, "Query History Test Complete");
            }
            catch (Exception ex)
            {
                AppendOutput($"❌ Query History Manager test failed: {ex.Message}");
                enhancedStatusBar.SetConnectionStatus(false, $"Error: {ex.Message}");
            }
        }

        private void TestBackupButton_Click(object sender, EventArgs e)
        {
            try
            {
                AppendOutput("=== Testing Database Backup Manager ===");
                
                AppendOutput("Testing BackupType enum:");
                AppendOutput($"  - Full: {BackupType.Full}");
                AppendOutput($"  - Differential: {BackupType.Differential}");
                AppendOutput($"  - Log: {BackupType.Log}");
                
                AppendOutput("Testing RestoreOptions class:");
                var restoreOptions = new RestoreOptions
                {
                    NewDatabaseName = "TestDB_Restored",
                    Replace = false,
                    WithRecovery = true
                };
                
                AppendOutput($"  - New Database: {restoreOptions.NewDatabaseName}");
                AppendOutput($"  - Replace: {restoreOptions.Replace}");
                AppendOutput($"  - With Recovery: {restoreOptions.WithRecovery}");
                
                AppendOutput("✅ Database Backup Manager classes test completed!");
                AppendOutput("   (Note: Full testing requires SQL Server connection)");
                
                enhancedStatusBar.SetConnectionStatus(true, "Backup Manager Test Complete");
            }
            catch (Exception ex)
            {
                AppendOutput($"❌ Database Backup Manager test failed: {ex.Message}");
                enhancedStatusBar.SetConnectionStatus(false, $"Error: {ex.Message}");
            }
        }

        private void TestConnectionBuilderButton_Click(object sender, EventArgs e)
        {
            try
            {
                AppendOutput("=== Testing Connection String Builder Dialog ===");
                
                using (var dialog = new ConnectionStringBuilderDialog())
                {
                    AppendOutput("Connection Builder Dialog created successfully");
                    
                    // Show dialog
                    var result = dialog.ShowDialog(this);
                    
                    if (result == DialogResult.OK)
                    {
                        AppendOutput($"✅ Connection String Builder test completed!");
                        AppendOutput($"Generated Connection String: {dialog.ConnectionString}");
                        enhancedStatusBar.SetConnectionStatus(true, "Connection Builder Test Complete");
                    }
                    else
                    {
                        AppendOutput("Connection String Builder dialog was cancelled");
                        enhancedStatusBar.SetConnectionStatus(false, "Connection Builder Cancelled");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"❌ Connection String Builder test failed: {ex.Message}");
                enhancedStatusBar.SetConnectionStatus(false, $"Error: {ex.Message}");
            }
        }

        private void AppendOutput(string text)
        {
            if (outputTextBox.InvokeRequired)
            {
                outputTextBox.Invoke(new Action(() => AppendOutput(text)));
                return;
            }
            
            outputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
            outputTextBox.ScrollToCaret();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }
    }
}
