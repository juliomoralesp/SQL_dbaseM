using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SqlServerManager.UI;
using SqlServerManager.Services;
using SqlServerManager.Core.Configuration;

namespace SqlServerManager
{
    public partial class MainForm : Form
    {
        private SqlConnection currentConnection;
        private string currentConnectionString;
        private TabControl mainTabControl;
        private TabPage databasesTab;
        private TabPage tablesTab;
        private ListBox databaseListBox;
        private DataGridView tablesGridView;
        private DataGridView columnsGridView;
        private MenuStrip mainMenuStrip;
        private ToolStrip mainToolStrip;
        private EnhancedStatusBar enhancedStatusBar;
        private ToolStripButton connectButton;
        private ToolStripButton disconnectButton;
        private ToolStripButton refreshButton;
        private ToolStripButton importWizardButton;
        private ToolStripButton quitButton;
        private ToolStripLabel connectionLabel;
        private ContextMenuStrip databaseContextMenu;
        private ContextMenuStrip tableContextMenu;
        private SplitContainer tablesSplitContainer;
        private Label tablesLabel;
        private Label columnsLabel;
        private string currentDatabaseName;
        private float currentFontScale = 1.2f; // Start with 20% larger fonts
        private SqlEditorControl sqlEditor;
        private KeyboardShortcutManager shortcutManager;
        private AutoReconnectManager autoReconnectManager;
        
        // Public properties for keyboard shortcut integration
        public TabControl TabControl => mainTabControl;

        public MainForm()
        {
            try
            {
                LogToFile("MainForm constructor starting...");
                InitializeComponent();
                LogToFile("InitializeComponent completed.");
                
                LoadSavedConnections();
                LogToFile("LoadSavedConnections completed.");
                
                LoadSavedSettings();
                LogToFile("LoadSavedSettings completed.");
                
                ApplyCurrentSettings();
                LogToFile("ApplyCurrentSettings completed.");
                
                // Attempt auto-reconnect on startup if enabled
                TryAutoReconnectOnStartup();
                
                LogToFile("MainForm constructor completed successfully.");
            }
            catch (Exception ex)
            {
                LogToFile($"Error in MainForm constructor: {ex}");
                // Try to show a basic form even if theming fails
                this.Text = "SQL Server Manager (Safe Mode)";
                this.Size = new Size(1200, 700);
                this.StartPosition = FormStartPosition.CenterScreen;
                
                MessageBox.Show($"Initialization warning: {ex.Message}\n\nRunning in safe mode.", 
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "SQL Server Manager";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create MenuStrip
            CreateMenuStrip();

            // Create ToolStrip
            mainToolStrip = new ToolStrip();
            connectButton = new ToolStripButton("Connect", null, ConnectButton_Click);
            connectButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            disconnectButton = new ToolStripButton("Disconnect", null, DisconnectButton_Click);
            disconnectButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            disconnectButton.Enabled = false;
            
            refreshButton = new ToolStripButton("Refresh", null, RefreshButton_Click);
            refreshButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            refreshButton.Enabled = false;
            
            connectionLabel = new ToolStripLabel("Not Connected");
            connectionLabel.ForeColor = Color.Red;
            
            quitButton = new ToolStripButton("Quit", null, QuitButton_Click);
            quitButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            quitButton.Alignment = ToolStripItemAlignment.Right;
            
            importWizardButton = new ToolStripButton("Import Wizard", null, ShowDataImportWizard_Click);
            importWizardButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            importWizardButton.Enabled = false; // Enable when connected
            
            mainToolStrip.Items.Add(connectButton);
            mainToolStrip.Items.Add(disconnectButton);
            mainToolStrip.Items.Add(new ToolStripSeparator());
            mainToolStrip.Items.Add(refreshButton);
            mainToolStrip.Items.Add(new ToolStripSeparator());
            mainToolStrip.Items.Add(importWizardButton);
            mainToolStrip.Items.Add(new ToolStripSeparator());
            mainToolStrip.Items.Add(connectionLabel);
            mainToolStrip.Items.Add(quitButton);

            // Create EnhancedStatusBar
            enhancedStatusBar = new EnhancedStatusBar();

            // Create TabControl
            mainTabControl = new TabControl();
            mainTabControl.Dock = DockStyle.Fill;

            // Create Databases Tab
            databasesTab = new TabPage("Databases");
            databaseListBox = new ListBox();
            databaseListBox.Dock = DockStyle.Fill;
            databaseListBox.DoubleClick += DatabaseListBox_DoubleClick;
            databaseListBox.MouseDown += DatabaseListBox_MouseDown;
            
            // Create context menu for databases
            databaseContextMenu = new ContextMenuStrip();
            var createDatabaseMenuItem = new ToolStripMenuItem("Create New Database", null, CreateDatabase_Click);
            var renameDatabaseMenuItem = new ToolStripMenuItem("Rename Database", null, RenameDatabase_Click);
            var deleteDatabaseMenuItem = new ToolStripMenuItem("Delete Database", null, DeleteDatabase_Click);
            var propertiesMenuItem = new ToolStripMenuItem("Properties", null, DatabaseProperties_Click);
            
            // Database maintenance actions
            var backupDatabaseMenuItem = new ToolStripMenuItem("Backup Database", null, BackupDatabase_Click);
            var restoreDatabaseMenuItem = new ToolStripMenuItem("Restore Database", null, RestoreDatabase_Click);
            var shrinkDatabaseMenuItem = new ToolStripMenuItem("Shrink Database", null, ShrinkDatabase_Click);
            var checkIntegrityMenuItem = new ToolStripMenuItem("Check Database Integrity", null, CheckDatabaseIntegrity_Click);
            
            // Database scripting actions
            var scriptDatabaseMenuItem = new ToolStripMenuItem("Generate Scripts", null, ScriptDatabase_Click);
            var exportDataMenuItem = new ToolStripMenuItem("Export Data", null, ExportDatabaseData_Click);
            var importDataMenuItem = new ToolStripMenuItem("Import Data", null, ImportDatabaseData_Click);
            
            databaseContextMenu.Items.AddRange(new ToolStripItem[] {
                createDatabaseMenuItem,
                new ToolStripSeparator(),
                renameDatabaseMenuItem,
                deleteDatabaseMenuItem,
                new ToolStripSeparator(),
                backupDatabaseMenuItem,
                restoreDatabaseMenuItem,
                shrinkDatabaseMenuItem,
                checkIntegrityMenuItem,
                new ToolStripSeparator(),
                scriptDatabaseMenuItem,
                exportDataMenuItem,
                importDataMenuItem,
                new ToolStripSeparator(),
                propertiesMenuItem
            });
            
            databaseListBox.ContextMenuStrip = databaseContextMenu;
            databasesTab.Controls.Add(databaseListBox);

            // Create Tables/Columns Tab
            tablesTab = new TabPage("Tables & Columns");
            tablesSplitContainer = new SplitContainer();
            tablesSplitContainer.Dock = DockStyle.Fill;
            tablesSplitContainer.Orientation = Orientation.Horizontal;
            tablesSplitContainer.SplitterDistance = 300;

            // Tables section
            var tablesPanel = new Panel();
            tablesPanel.Dock = DockStyle.Fill;
            tablesLabel = new Label();
            tablesLabel.Text = "Tables:";
            tablesLabel.Dock = DockStyle.Top;
            tablesLabel.Height = 30;
            tablesLabel.Font = FontManager.GetScaledFont("Segoe UI", 10, FontStyle.Bold);
            
            tablesGridView = new DataGridView();
            tablesGridView.Dock = DockStyle.Fill;
            tablesGridView.AllowUserToAddRows = false;
            tablesGridView.AllowUserToDeleteRows = false;
            tablesGridView.ReadOnly = true;
            tablesGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablesGridView.MultiSelect = false;
            tablesGridView.SelectionChanged += TablesGridView_SelectionChanged;
            tablesGridView.MouseDown += TablesGridView_MouseDown;
            
            // Create context menu for tables
            tableContextMenu = new ContextMenuStrip();
            var createTableMenuItem = new ToolStripMenuItem("Create New Table", null, CreateTable_Click);
            var editTableMenuItem = new ToolStripMenuItem("Edit Table Structure", null, EditTable_Click);
            var deleteTableMenuItem = new ToolStripMenuItem("Delete Table", null, DeleteTable_Click);
            
            // Data operations
            var viewDataMenuItem = new ToolStripMenuItem("View Data", null, ViewTableData_Click);
            var printTableMenuItem = new ToolStripMenuItem("Print Table Data", null, PrintTableData_Click);
            var editDataMenuItem = new ToolStripMenuItem("Edit Data", null, EditTableData_Click);
            var insertDataMenuItem = new ToolStripMenuItem("Insert Data", null, InsertTableData_Click);
            
            // Table operations
            var truncateTableMenuItem = new ToolStripMenuItem("Truncate Table", null, TruncateTable_Click);
            var copyTableMenuItem = new ToolStripMenuItem("Duplicate Table", null, CopyTable_Click);
            var renameTableMenuItem = new ToolStripMenuItem("Rename Table", null, RenameTable_Click);
            
            // Table analysis
            var viewIndexesMenuItem = new ToolStripMenuItem("View Indexes", null, ViewTableIndexes_Click);
            var viewConstraintsMenuItem = new ToolStripMenuItem("View Constraints", null, ViewTableConstraints_Click);
            var tableStatsMenuItem = new ToolStripMenuItem("Table Statistics", null, ViewTableStats_Click);
            
            // Import/Export operations
            var exportTableMenuItem = new ToolStripMenuItem("Export Table Data", null, ExportTableData_Click);
            var importTableMenuItem = new ToolStripMenuItem("Import Table Data", null, ImportTableData_Click);
            var scriptTableMenuItem = new ToolStripMenuItem("Generate Table Script", null, ScriptTable_Click);
            
            var refreshTablesMenuItem = new ToolStripMenuItem("Refresh Tables", null, (s, e) => { 
                if (!string.IsNullOrEmpty(currentDatabaseName)) LoadTables(currentDatabaseName); 
            });
            
            tableContextMenu.Items.AddRange(new ToolStripItem[] {
                createTableMenuItem,
                editTableMenuItem,
                deleteTableMenuItem,
                new ToolStripSeparator(),
                viewDataMenuItem,
                printTableMenuItem,
                editDataMenuItem,
                insertDataMenuItem,
                new ToolStripSeparator(),
                truncateTableMenuItem,
                copyTableMenuItem,
                renameTableMenuItem,
                new ToolStripSeparator(),
                viewIndexesMenuItem,
                viewConstraintsMenuItem,
                tableStatsMenuItem,
                new ToolStripSeparator(),
                exportTableMenuItem,
                importTableMenuItem,
                scriptTableMenuItem,
                new ToolStripSeparator(),
                refreshTablesMenuItem
            });
            
            tablesGridView.ContextMenuStrip = tableContextMenu;
            
            tablesPanel.Controls.Add(tablesGridView);
            tablesPanel.Controls.Add(tablesLabel);
            tablesSplitContainer.Panel1.Controls.Add(tablesPanel);

            // Columns section
            var columnsPanel = new Panel();
            columnsPanel.Dock = DockStyle.Fill;
            columnsLabel = new Label();
            columnsLabel.Text = "Columns:";
            columnsLabel.Dock = DockStyle.Top;
            columnsLabel.Height = 30;
            columnsLabel.Font = FontManager.GetScaledFont("Segoe UI", 10, FontStyle.Bold);
            
            columnsGridView = new DataGridView();
            columnsGridView.Dock = DockStyle.Fill;
            columnsGridView.AllowUserToAddRows = false;
            columnsGridView.AllowUserToDeleteRows = false;
            columnsGridView.ReadOnly = true;
            
            columnsPanel.Controls.Add(columnsGridView);
            columnsPanel.Controls.Add(columnsLabel);
            tablesSplitContainer.Panel2.Controls.Add(columnsPanel);

            tablesTab.Controls.Add(tablesSplitContainer);

            // Create SQL Editor Tab
            var sqlEditorTab = new TabPage("Query Builder");
            var sqlEditor = new SqlEditorControl();
            sqlEditor.Dock = DockStyle.Fill;
            sqlEditorTab.Controls.Add(sqlEditor);
            
            // Create Advanced SQL Editor Tab
            var advancedSqlEditorTab = new TabPage("Advanced SQL Editor");
            try
            {
                var connectionService = Services.ConfigurationService.GetService<Services.ConnectionService>();
                var scintillaSqlEditor = new Core.QueryEngine.ScintillaSqlEditor(connectionService);
                scintillaSqlEditor.Dock = DockStyle.Fill;
                advancedSqlEditorTab.Controls.Add(scintillaSqlEditor);
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to create Advanced SQL Editor: {ex.Message}");
                // Add placeholder label
                var placeholder = new Label
                {
                    Text = "Advanced SQL Editor not available. Check logs for details.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red
                };
                advancedSqlEditorTab.Controls.Add(placeholder);
            }
            
            // Add tabs to TabControl
            mainTabControl.TabPages.Add(databasesTab);
            mainTabControl.TabPages.Add(tablesTab);
            mainTabControl.TabPages.Add(sqlEditorTab);
            mainTabControl.TabPages.Add(advancedSqlEditorTab);

            // Add controls to form
            this.Controls.Add(mainTabControl);
            this.Controls.Add(mainToolStrip);
            this.Controls.Add(mainMenuStrip);
            this.Controls.Add(enhancedStatusBar);
            this.MainMenuStrip = mainMenuStrip;
            
            // Store reference to SQL Editor for connection updates
            this.sqlEditor = sqlEditor;
            
            // Initialize keyboard shortcut manager
            try
            {
                shortcutManager = new KeyboardShortcutManager(this);
                LoggingService.LogInformation("Keyboard shortcut manager initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Failed to initialize keyboard shortcut manager: {Exception}", ex.Message);
            }
            
            // Initialize auto-reconnect manager
            try
            {
                var connectionService = new SimpleConnectionService(null, null);
                autoReconnectManager = new AutoReconnectManager(connectionService);
                
                // Set up event handlers for auto-reconnect events
                autoReconnectManager.ReconnectAttempted += OnReconnectAttempted;
                autoReconnectManager.ReconnectSucceeded += OnReconnectSucceeded;
                autoReconnectManager.ReconnectFailed += OnReconnectFailed;
                
                LoggingService.LogInformation("AutoReconnectManager initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Failed to initialize AutoReconnectManager: {Exception}", ex.Message);
            }
        }

        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            using (var connectionDialog = new ConnectionDialog())
            {
                // Apply theme and font to dialog
                try
                {
                    ThemeManager.ApplyThemeToDialog(connectionDialog);
                    FontManager.ApplyFontSize(connectionDialog, currentFontScale);
                }
                catch
                {
                    // Continue even if theme application fails
                }
                
                if (connectionDialog.ShowDialog() == DialogResult.OK)
                {
                    // Disable connect button and show progress
                    connectButton.Enabled = false;
                    enhancedStatusBar.ShowMessage("Connecting...", MessageType.Info);
                    enhancedStatusBar.SetConnectionStatus(false, "Connecting...");
                    
                    try
                    {
                        currentConnectionString = connectionDialog.ConnectionString;
                        currentConnection = new SqlConnection(currentConnectionString);
                        
                        // Use async open
                        await currentConnection.OpenAsync();
                        
                        // Test connection
                        using (var command = new SqlCommand("SELECT 1", currentConnection))
                        {
                            await command.ExecuteScalarAsync();
                        }
                        
                        // Save successful connection
                        SaveConnection(currentConnectionString);
                        
                        // Notify AutoReconnectManager of successful connection
                        try
                        {
                            var builder = new SqlConnectionStringBuilder(currentConnectionString);
                            autoReconnectManager?.NotifyConnectionSucceeded(builder.DataSource, builder.InitialCatalog, currentConnectionString);
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error notifying AutoReconnectManager of connection success: {ex.Message}");
                        }
                        
                        // Update UI
                        connectionLabel.Text = $"Connected to: {currentConnection.DataSource}";
                        connectionLabel.ForeColor = Color.Green;
                        disconnectButton.Enabled = true;
                        refreshButton.Enabled = true;
                        importWizardButton.Enabled = true;
                        enhancedStatusBar.SetConnectionStatus(true, $"Connected to: {currentConnection.DataSource}");
                        enhancedStatusBar.ShowMessage("Connected successfully", MessageType.Success);
                        
                        // Update SQL Editor connection
                        if (sqlEditor != null)
                        {
                            sqlEditor.UpdateConnection(currentConnection);
                        }
                        
                        // Load databases
                        await LoadDatabasesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Connection failed: {ex.Message}", "Connection Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (currentConnection != null)
                        {
                            currentConnection.Close();
                            currentConnection.Dispose();
                            currentConnection = null;
                        }
                        
                        // Re-enable connect button on failure
                        connectButton.Enabled = true;
                        enhancedStatusBar.SetConnectionStatus(false, "Connection failed");
                        enhancedStatusBar.ShowMessage("Connection failed", MessageType.Error);
                    }
                }
                else
                {
                    // Dialog was cancelled, ensure connect button is enabled
                    connectButton.Enabled = true;
                }
            }
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            // Notify AutoReconnectManager of connection loss
            try
            {
                autoReconnectManager?.NotifyConnectionLost();
            }
            catch (Exception ex)
            {
                LogToFile($"Error notifying AutoReconnectManager of connection loss: {ex.Message}");
            }
            
            // Close existing connection
            if (currentConnection != null && currentConnection.State == ConnectionState.Open)
            {
                currentConnection.Close();
                currentConnection.Dispose();
                currentConnection = null;
            }
            
            // Reset UI
            connectionLabel.Text = "Not Connected";
            connectionLabel.ForeColor = Color.Red;
            connectButton.Enabled = true;
            disconnectButton.Enabled = false;
            refreshButton.Enabled = false;
            importWizardButton.Enabled = false;
            databaseListBox.Items.Clear();
            tablesGridView.DataSource = null;
            columnsGridView.DataSource = null;
            currentDatabaseName = null;
            enhancedStatusBar.SetConnectionStatus(false, "Disconnected");
            enhancedStatusBar.ShowMessage("Disconnected", MessageType.Info);
            
            // Clear SQL Editor connection
            if (sqlEditor != null)
            {
                sqlEditor.UpdateConnection(null);
            }
            
            // Automatically show connection dialog for reconnection
            var result = MessageBox.Show(
                "You have been disconnected. Would you like to connect to another server?", 
                "Reconnect?", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                // Small delay to ensure UI updates
                Application.DoEvents();
                ConnectButton_Click(sender, e);
            }
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            refreshButton.Enabled = false;
            enhancedStatusBar.ShowMessage("Refreshing...", MessageType.Info);
            
            try
            {
                await LoadDatabasesAsync();
                if (databaseListBox.SelectedItem != null)
                {
                    await LoadTablesAsync(databaseListBox.SelectedItem.ToString());
                }
            }
            finally
            {
                refreshButton.Enabled = true;
            }
        }

        private async Task LoadDatabasesAsync()
        {
            try
            {
                databaseListBox.Items.Clear();
                using (var command = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", currentConnection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            databaseListBox.Items.Add(reader["name"].ToString());
                        }
                    }
                }
                enhancedStatusBar.ShowMessage($"Loaded {databaseListBox.Items.Count} databases", MessageType.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading databases: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadDatabases()
        {
            // Keep synchronous version for backward compatibility
            _ = LoadDatabasesAsync();
        }

        private void DatabaseListBox_DoubleClick(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem != null)
            {
                string selectedDatabase = databaseListBox.SelectedItem.ToString();
                currentDatabaseName = selectedDatabase;
                LoadTables(selectedDatabase);
                mainTabControl.SelectedTab = tablesTab;
            }
        }

        private void DatabaseListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = databaseListBox.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    databaseListBox.SelectedIndex = index;
                }
            }
        }

        private async Task LoadTablesAsync(string databaseName)
        {
            // Check if connection is valid before attempting to load
            if (currentConnection == null || currentConnection.State != ConnectionState.Open)
            {
                tablesGridView.DataSource = null;
                tablesLabel.Text = "Tables: (Not connected)";
                enhancedStatusBar.ShowMessage("Not connected", MessageType.Warning);
                return;
            }
            
            try
            {
                currentDatabaseName = databaseName;
                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        TABLE_SCHEMA as [Schema],
                        TABLE_NAME as [Table Name],
                        TABLE_TYPE as [Type]
                    FROM INFORMATION_SCHEMA.TABLES
                    ORDER BY TABLE_SCHEMA, TABLE_NAME";
                
                using (var command = new SqlCommand(query, currentConnection))
                {
                    var dataTable = new DataTable();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        dataTable.Load(reader);
                    }
                    tablesGridView.DataSource = dataTable;
                    tablesLabel.Text = $"Tables in {databaseName}:";
                }
                enhancedStatusBar.ShowMessage($"Loaded tables from {databaseName}", MessageType.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tables: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadTables(string databaseName)
        {
            // Keep synchronous version for backward compatibility
            _ = LoadTablesAsync(databaseName);
        }

        private void TablesGridView_SelectionChanged(object sender, EventArgs e)
        {
            // Only load columns if we have a valid connection
            if (currentConnection != null && currentConnection.State == ConnectionState.Open &&
                tablesGridView.CurrentRow != null && tablesGridView.CurrentRow.Cells["Table Name"] != null)
            {
                string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
                string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
                LoadColumns(schema, tableName);
            }
        }

        private void LoadColumns(string schema, string tableName)
        {
            // Check if connection is valid before attempting to load
            if (currentConnection == null || currentConnection.State != ConnectionState.Open)
            {
                columnsGridView.DataSource = null;
                columnsLabel.Text = "Columns: (Not connected)";
                return;
            }
            
            try
            {
                var query = $@"
                    SELECT 
                        COLUMN_NAME as [Column Name],
                        DATA_TYPE as [Data Type],
                        CHARACTER_MAXIMUM_LENGTH as [Max Length],
                        IS_NULLABLE as [Nullable],
                        COLUMN_DEFAULT as [Default Value]
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                    ORDER BY ORDINAL_POSITION";
                
                using (var command = new SqlCommand(query, currentConnection))
                {
                    command.Parameters.AddWithValue("@schema", schema);
                    command.Parameters.AddWithValue("@table", tableName);
                    
                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    columnsGridView.DataSource = dataTable;
                    columnsLabel.Text = $"Columns in {schema}.{tableName}:";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading columns: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateDatabase_Click(object sender, EventArgs e)
        {
            using (var dialog = new CreateDatabaseDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string createQuery = $"CREATE DATABASE [{dialog.DatabaseName}]";
                        using (var command = new SqlCommand(createQuery, currentConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                        MessageBox.Show($"Database '{dialog.DatabaseName}' created successfully.", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDatabases();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating database: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RenameDatabase_Click(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database to rename.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string oldName = databaseListBox.SelectedItem.ToString();
            using (var dialog = new InputDialog($"Rename database '{oldName}':", "New name:", oldName))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string query = $"ALTER DATABASE [{oldName}] MODIFY NAME = [{dialog.InputValue}]";
                        using (var command = new SqlCommand(query, currentConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                        MessageBox.Show($"Database renamed successfully.", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDatabases();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming database: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DeleteDatabase_Click(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database to delete.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string dbName = databaseListBox.SelectedItem.ToString();
            var result = MessageBox.Show($"Are you sure you want to delete the database '{dbName}'?\n\nThis action cannot be undone!", 
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    string query = $@"
                        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{dbName}]";
                    using (var command = new SqlCommand(query, currentConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                    MessageBox.Show($"Database '{dbName}' deleted successfully.", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadDatabases();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting database: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DatabaseProperties_Click(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string dbName = databaseListBox.SelectedItem.ToString();
            using (var dialog = new DatabasePropertiesDialog(currentConnection, dbName))
            {
                dialog.ShowDialog();
            }
        }

        private void SaveConnection(string connectionString)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["SavedConnections"] == null)
                {
                    config.AppSettings.Settings.Add("SavedConnections", connectionString);
                }
                else
                {
                    var savedConnections = config.AppSettings.Settings["SavedConnections"].Value;
                    if (!savedConnections.Contains(connectionString))
                    {
                        config.AppSettings.Settings["SavedConnections"].Value = savedConnections + "|" + connectionString;
                    }
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt workflow
                Console.WriteLine($"Error saving connection: {ex.Message}");
            }
        }

        private void LoadSavedConnections()
        {
            try
            {
                var savedConnections = ConfigurationManager.AppSettings["SavedConnections"];
                if (!string.IsNullOrEmpty(savedConnections))
                {
                    // Saved connections are available for use in ConnectionDialog
                }
            }
            catch
            {
                // No saved connections yet
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();
            
            // Cleanup keyboard shortcut manager
            try
            {
                shortcutManager?.Dispose();
                LoadingManager.Cleanup();
                LoggingService.LogInformation("Application cleanup completed");
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error during application cleanup: {Exception}", ex.Message);
            }
            
            if (currentConnection != null && currentConnection.State == ConnectionState.Open)
            {
                currentConnection.Close();
                currentConnection.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void CreateMenuStrip()
        {
            mainMenuStrip = new MenuStrip();
            mainMenuStrip.Font = FontManager.GetScaledFont(9);

            // File Menu
            var fileMenu = new ToolStripMenuItem("&File");
            var connectMenuItem = new ToolStripMenuItem("&Connect...", null, ConnectButton_Click);
            var discoverServersMenuItem = new ToolStripMenuItem("&Discover Servers...", null, DiscoverServers_Click);
            var disconnectMenuItem = new ToolStripMenuItem("&Disconnect", null, DisconnectButton_Click);
            var quitMenuItem = new ToolStripMenuItem("&Quit", null, QuitButton_Click);
            quitMenuItem.ShortcutKeys = Keys.Control | Keys.Q;
            
            fileMenu.DropDownItems.Add(connectMenuItem);
            fileMenu.DropDownItems.Add(discoverServersMenuItem);
            fileMenu.DropDownItems.Add(disconnectMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(quitMenuItem);

            // View Menu
            var viewMenu = new ToolStripMenuItem("&View");
            
            // Theme submenu
            var themeMenu = new ToolStripMenuItem("&Theme");
            var lightThemeMenuItem = new ToolStripMenuItem("&Light", null, (s, e) => ApplyTheme(ThemeManager.Theme.Light));
            var darkThemeMenuItem = new ToolStripMenuItem("&Dark", null, (s, e) => ApplyTheme(ThemeManager.Theme.Dark));
            var systemThemeMenuItem = new ToolStripMenuItem("&System", null, (s, e) => ApplyTheme(ThemeManager.Theme.System));
            
            themeMenu.DropDownItems.Add(lightThemeMenuItem);
            themeMenu.DropDownItems.Add(darkThemeMenuItem);
            themeMenu.DropDownItems.Add(systemThemeMenuItem);
            
            // Font size submenu
            var fontSizeMenu = new ToolStripMenuItem("&Font Size");
            var smallFontMenuItem = new ToolStripMenuItem("&Small (80%)", null, (s, e) => ApplyFontScale(0.8f));
            var normalFontMenuItem = new ToolStripMenuItem("&Normal (100%)", null, (s, e) => ApplyFontScale(1.0f));
            var largeFontMenuItem = new ToolStripMenuItem("&Large (120%)", null, (s, e) => ApplyFontScale(1.2f));
            var extraLargeFontMenuItem = new ToolStripMenuItem("&Extra Large (150%)", null, (s, e) => ApplyFontScale(1.5f));
            
            fontSizeMenu.DropDownItems.Add(smallFontMenuItem);
            fontSizeMenu.DropDownItems.Add(normalFontMenuItem);
            fontSizeMenu.DropDownItems.Add(largeFontMenuItem);
            fontSizeMenu.DropDownItems.Add(extraLargeFontMenuItem);
            
            viewMenu.DropDownItems.Add(themeMenu);
            viewMenu.DropDownItems.Add(fontSizeMenu);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            var refreshMenuItem = new ToolStripMenuItem("&Refresh", null, RefreshButton_Click);
            refreshMenuItem.ShortcutKeys = Keys.F5;
            viewMenu.DropDownItems.Add(refreshMenuItem);

            // Tools Menu
            var toolsMenu = new ToolStripMenuItem("&Tools");
            var scriptDatabaseMenuItem = new ToolStripMenuItem("&Script Database...", null, ScriptDatabase_Click);
            var importExportWizardMenuItem = new ToolStripMenuItem("Data Import/Export &Wizard...", null, ShowDataImportWizard_Click);
            var advancedSearchMenuItem = new ToolStripMenuItem("\u0026Advanced Search...", null, AdvancedSearchMenuItem_Click);
            advancedSearchMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.F;
            var settingsMenuItem = new ToolStripMenuItem("&Settings...", null, Settings_Click);
            settingsMenuItem.ShortcutKeys = Keys.Control | Keys.Comma;
            toolsMenu.DropDownItems.Add(scriptDatabaseMenuItem);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add(advancedSearchMenuItem);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add(importExportWizardMenuItem);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add(settingsMenuItem);

             // Help Menu
            var helpMenu = new ToolStripMenuItem("&Help");
            var aboutMenuItem = new ToolStripMenuItem("&About", null, (s, e) => 
            {
                MessageBox.Show("SQL Server Manager\nVersion 1.0\n\nA tool for managing SQL Server databases.", 
                    "About SQL Server Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            helpMenu.DropDownItems.Add(aboutMenuItem);

            mainMenuStrip.Items.Add(fileMenu);
            mainMenuStrip.Items.Add(toolsMenu);
            mainMenuStrip.Items.Add(helpMenu);
        }

        private void DiscoverServers_Click(object sender, EventArgs e)
        {
            try
            {
                using (var discoveryDialog = new ServerDiscoveryDialog())
                {
                    ThemeManager.ApplyThemeToDialog(discoveryDialog);
                    FontManager.ApplyFontSize(discoveryDialog, currentFontScale);
                    
                    if (discoveryDialog.ShowDialog() == DialogResult.OK && !discoveryDialog.UserCancelled)
                    {
                        var selectedInstance = discoveryDialog.SelectedInstance;
                        if (selectedInstance != null)
                        {
                            // Create connection dialog with the discovered server pre-filled
                            using (var connectionDialog = new ConnectionDialog())
                            {
                                // Set the server name in the connection dialog
                                connectionDialog.ServerName = selectedInstance.ServerName;
                                
                                ThemeManager.ApplyThemeToDialog(connectionDialog);
                                FontManager.ApplyFontSize(connectionDialog, currentFontScale);
                                
                                if (connectionDialog.ShowDialog() == DialogResult.OK)
                                {
                                    // Use the existing connection logic
                                    ConnectWithConnectionString(connectionDialog.ConnectionString);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening server discovery dialog: {ex.Message}", "Discovery Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ConnectWithConnectionString(string connectionString)
        {
            // Disable connect button and show progress
            connectButton.Enabled = false;
                            enhancedStatusBar.ShowMessage("Connecting...", MessageType.Info);
            
            try
            {
                currentConnectionString = connectionString;
                currentConnection = new SqlConnection(currentConnectionString);
                
                // Use async open
                await currentConnection.OpenAsync();
                
                // Test connection
                using (var command = new SqlCommand("SELECT 1", currentConnection))
                {
                    await command.ExecuteScalarAsync();
                }
                
                // Save successful connection
                SaveConnection(currentConnectionString);
                
                // Notify AutoReconnectManager of successful connection
                try
                {
                    var builder = new SqlConnectionStringBuilder(currentConnectionString);
                    autoReconnectManager?.NotifyConnectionSucceeded(builder.DataSource, builder.InitialCatalog, currentConnectionString);
                }
                catch (Exception ex)
                {
                    LogToFile($"Error notifying AutoReconnectManager of connection success: {ex.Message}");
                }
                
                // Update UI
                connectionLabel.Text = $"Connected to: {currentConnection.DataSource}";
                connectionLabel.ForeColor = Color.Green;
                disconnectButton.Enabled = true;
                refreshButton.Enabled = true;
                enhancedStatusBar.ShowMessage("Connected successfully", MessageType.Success);
                
                // Update SQL Editor connection
                if (sqlEditor != null)
                {
                    sqlEditor.UpdateConnection(currentConnection);
                }
                
                // Load databases
                await LoadDatabasesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Connection Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (currentConnection != null)
                {
                    currentConnection.Close();
                    currentConnection.Dispose();
                    currentConnection = null;
                }
                
                // Re-enable connect button on failure
                connectButton.Enabled = true;
                enhancedStatusBar.ShowMessage("Connection failed", MessageType.Error);
            }
        }

        private void QuitButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to quit?", "Confirm Exit", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void PrintTableData_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a table to print.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dt = (DataTable)tablesGridView.DataSource;
            if (dt == null)
            {
                MessageBox.Show("No data to print.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PrintUtility.PrintDataTable(dt, $"Table: {tablesGridView.CurrentRow.Cells[1].Value}");
        }

        private void ApplyTheme(ThemeManager.Theme theme)
        {
            ThemeManager.ApplyTheme(this, theme);
            enhancedStatusBar.ShowMessage($"Theme changed to {theme}", MessageType.Info);
        }

        private void ApplyFontScale(float scale)
        {
            currentFontScale = scale;
            FontManager.ApplyFontSize(this, scale);
            
            // Reapply theme to ensure colors are preserved
            ThemeManager.ApplyTheme(this, ThemeManager.CurrentTheme);
            
            // Adjust form size if needed for larger fonts
            if (scale > 1.2f)
            {
                this.Size = new Size((int)(1200 * scale / 1.2f), (int)(700 * scale / 1.2f));
            }
            
            enhancedStatusBar.ShowMessage($"Font size changed to {(int)(scale * 100)}%", MessageType.Info);
        }

        private void ApplyCurrentSettings()
        {
            // Apply saved font scale
            FontManager.ApplyFontSize(this, currentFontScale);
            
            // Apply saved theme
            var savedTheme = ThemeManager.CurrentTheme;
            ThemeManager.ApplyTheme(this, savedTheme);
        }

        private void SaveSettings()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                // Save font scale
                if (config.AppSettings.Settings["FontScale"] == null)
                {
                    config.AppSettings.Settings.Add("FontScale", currentFontScale.ToString());
                }
                else
                {
                    config.AppSettings.Settings["FontScale"].Value = currentFontScale.ToString();
                }
                
                // Save theme
                if (config.AppSettings.Settings["Theme"] == null)
                {
                    config.AppSettings.Settings.Add("Theme", ThemeManager.CurrentTheme.ToString());
                }
                else
                {
                    config.AppSettings.Settings["Theme"].Value = ThemeManager.CurrentTheme.ToString();
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void LoadSavedSettings()
        {
            try
            {
                // Load font scale
                var fontScaleStr = ConfigurationManager.AppSettings["FontScale"];
                if (!string.IsNullOrEmpty(fontScaleStr) && float.TryParse(fontScaleStr, out float scale))
                {
                    currentFontScale = scale;
                }
                
                // Load theme
                var themeStr = ConfigurationManager.AppSettings["Theme"];
                if (!string.IsNullOrEmpty(themeStr) && Enum.TryParse<ThemeManager.Theme>(themeStr, out var theme))
                {
                    // Theme will be applied in ApplyCurrentSettings
                }
            }
            catch
            {
                // Use defaults if no saved settings
            }
        }

        private void TablesGridView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hitTestInfo = tablesGridView.HitTest(e.X, e.Y);
                if (hitTestInfo.RowIndex >= 0)
                {
                    tablesGridView.ClearSelection();
                    tablesGridView.Rows[hitTestInfo.RowIndex].Selected = true;
                }
            }
        }

        private void CreateTable_Click(object sender, EventArgs e)
        {
            if (currentConnection == null || currentConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Please connect to a database first.", "Not Connected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(currentDatabaseName))
            {
                MessageBox.Show("Please select a database first.", "No Database Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialog = new TableEditorDialog(currentConnection, currentDatabaseName))
            {
                dialog.Font = this.Font;
                ThemeManager.ApplyThemeToDialog(dialog);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadTables(currentDatabaseName);
                }
            }
        }

        private void EditTable_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to edit.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            
            using (var dialog = new TableEditorDialog(currentConnection, currentDatabaseName, tableName))
            {
                dialog.Font = this.Font;
                ThemeManager.ApplyThemeToDialog(dialog);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadTables(currentDatabaseName);
                    // Reload columns if the same table is selected
                    if (tablesGridView.CurrentRow != null && 
                        tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString() == tableName)
                    {
                        string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
                        LoadColumns(schema, tableName);
                    }
                }
            }
        }

        private void DeleteTable_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to delete.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            var result = MessageBox.Show(
                $"Are you sure you want to delete the table '{schema}.{tableName}'?\n\n" +
                "This action cannot be undone and will delete all data in the table!", 
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    string query = $"USE [{currentDatabaseName}]; DROP TABLE [{schema}].[{tableName}]";
                    using (var command = new SqlCommand(query, currentConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                    MessageBox.Show($"Table '{schema}.{tableName}' deleted successfully.", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTables(currentDatabaseName);
                    columnsGridView.DataSource = null;
                    columnsLabel.Text = "Columns:";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting table: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void LogToFile(string message)
        {
            try
            {
                string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainForm: {message}\n");
            }
            catch
            {
                // Ignore logging errors to prevent infinite loops
            }
        }

        // Database action methods
        private void BackupDatabase_Click(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database to backup.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string dbName = databaseListBox.SelectedItem.ToString();
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*";
                saveDialog.Title = "Save Database Backup";
                saveDialog.FileName = $"{dbName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string query = $"BACKUP DATABASE [{dbName}] TO DISK = @backupPath";
                        using (var command = new SqlCommand(query, currentConnection))
                        {
                            command.Parameters.AddWithValue("@backupPath", saveDialog.FileName);
                            command.CommandTimeout = 300; // 5 minutes timeout for backup
                            enhancedStatusBar.ShowMessage("Creating backup...", MessageType.Info);
                            command.ExecuteNonQuery();
                        }
                        MessageBox.Show($"Database '{dbName}' backed up successfully to:\n{saveDialog.FileName}", 
                            "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        enhancedStatusBar.ShowMessage("Backup completed", MessageType.Success);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error backing up database: {ex.Message}", "Backup Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        enhancedStatusBar.ShowMessage("Backup failed", MessageType.Error);
                    }
                }
            }
        }

        private void RestoreDatabase_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*";
                openDialog.Title = "Select Database Backup File";
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var dialog = new InputDialog("Restore Database", "Database name:", ""))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                string dbName = dialog.InputValue;
                                string query = $"RESTORE DATABASE [{dbName}] FROM DISK = @backupPath WITH REPLACE";
                                using (var command = new SqlCommand(query, currentConnection))
                                {
                                    command.Parameters.AddWithValue("@backupPath", openDialog.FileName);
                                    command.CommandTimeout = 300; // 5 minutes timeout for restore
                                    enhancedStatusBar.ShowMessage("Restoring database...", MessageType.Info);
                                    command.ExecuteNonQuery();
                                }
                                MessageBox.Show($"Database '{dbName}' restored successfully.", 
                                    "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadDatabases();
                                enhancedStatusBar.ShowMessage("Restore completed", MessageType.Success);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error restoring database: {ex.Message}", "Restore Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                                enhancedStatusBar.ShowMessage("Restore failed", MessageType.Error);
                            }
                        }
                    }
                }
            }
        }

        private void ShrinkDatabase_Click(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database to shrink.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string dbName = databaseListBox.SelectedItem.ToString();
            var result = MessageBox.Show($"Are you sure you want to shrink the database '{dbName}'?\n\n" +
                "This operation may take a long time and should be done during low activity periods.", 
                "Confirm Shrink", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    string query = $"DBCC SHRINKDATABASE([{dbName}])";
                    using (var command = new SqlCommand(query, currentConnection))
                    {
                        command.CommandTimeout = 300; // 5 minutes timeout
                        enhancedStatusBar.ShowMessage("Shrinking database...", MessageType.Info);
                        command.ExecuteNonQuery();
                    }
                    MessageBox.Show($"Database '{dbName}' shrunk successfully.", "Shrink Complete", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    enhancedStatusBar.ShowMessage("Shrink completed", MessageType.Success);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error shrinking database: {ex.Message}", "Shrink Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    enhancedStatusBar.ShowMessage("Shrink failed", MessageType.Error);
                }
            }
        }

        private void CheckDatabaseIntegrity_Click(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database to check.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string dbName = databaseListBox.SelectedItem.ToString();
            try
            {
                string query = $"DBCC CHECKDB([{dbName}]) WITH NO_INFOMSGS";
                using (var command = new SqlCommand(query, currentConnection))
                {
                    command.CommandTimeout = 300; // 5 minutes timeout
                    enhancedStatusBar.ShowMessage("Checking database integrity...", MessageType.Info);
                    command.ExecuteNonQuery();
                }
                MessageBox.Show($"Database integrity check completed for '{dbName}'.", "Check Complete", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                enhancedStatusBar.ShowMessage("Integrity check completed", MessageType.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking database integrity: {ex.Message}", "Check Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                enhancedStatusBar.ShowMessage("Integrity check failed", MessageType.Error);
            }
        }

        private void ScriptDatabase_Click(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database to script.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dbName = databaseListBox.SelectedItem.ToString();
            using (var dialog = new UI.DatabaseScriptGenerator(currentConnection, dbName))
            {
                ThemeManager.ApplyThemeToDialog(dialog);
                dialog.ShowDialog();
            }
        }

        private void ExportDatabaseData_Click(object sender, EventArgs e)
        {
            if (currentConnection == null || currentConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Please connect to a SQL Server instance first.", "No Connection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database to export.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dbName = databaseListBox.SelectedItem.ToString();
            try
            {
                using (var dialog = new UI.DataImportExportDialog(currentConnection, dbName, null, null, UI.DataImportExportDialog.OperationType.Export))
                {
                    ThemeManager.ApplyThemeToDialog(dialog);
                    FontManager.ApplyFontSize(dialog, currentFontScale);
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        enhancedStatusBar.ShowMessage("Export completed successfully", MessageType.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening export dialog: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportDatabaseData_Click(object sender, EventArgs e)
        {
            if (databaseListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a database to import data into.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dbName = databaseListBox.SelectedItem.ToString();
            using (var dialog = new UI.DataImportExportDialog(currentConnection, dbName, null, null, UI.DataImportExportDialog.OperationType.Import))
            {
                ThemeManager.ApplyThemeToDialog(dialog);
                dialog.ShowDialog();
            }
        }

        // Table action methods
        private void ViewTableData_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to view.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                var query = $"USE [{currentDatabaseName}]; SELECT TOP 1000 * FROM [{schema}].[{tableName}]";
                using (var command = new SqlCommand(query, currentConnection))
                {
                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    
                    // Create a simple data viewer form
                    var dataForm = new Form()
                    {
                        Text = $"Data: {schema}.{tableName} (Top 1000 rows)",
                        Size = new Size(800, 600),
                        StartPosition = FormStartPosition.CenterParent
                    };
                    
                    var dataGrid = new DataGridView()
                    {
                        Dock = DockStyle.Fill,
                        DataSource = dataTable,
                        ReadOnly = true,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false
                    };
                    
                    dataForm.Controls.Add(dataGrid);
                    ThemeManager.ApplyThemeToDialog(dataForm);
                    dataForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing table data: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditTableData_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to edit.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                using (var dataEditor = new TableDataEditor(currentConnection, currentDatabaseName, schema, tableName))
                {
                    ThemeManager.ApplyThemeToDialog(dataEditor);
                    FontManager.ApplyFontSize(dataEditor, currentFontScale);
                    dataEditor.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening data editor: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InsertTableData_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to insert data into.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                using (var dataEditor = new TableDataEditor(currentConnection, currentDatabaseName, schema, tableName))
                {
                    ThemeManager.ApplyThemeToDialog(dataEditor);
                    FontManager.ApplyFontSize(dataEditor, currentFontScale);
                    
                    // The data editor will open ready for editing - user can add new rows using the Add Row button
                    dataEditor.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening data editor: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TruncateTable_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to truncate.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            var result = MessageBox.Show(
                $"Are you sure you want to truncate the table '{schema}.{tableName}'?\n\n" +
                "This will delete all data in the table and cannot be undone!", 
                "Confirm Truncate", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    string query = $"USE [{currentDatabaseName}]; TRUNCATE TABLE [{schema}].[{tableName}]";
                    using (var command = new SqlCommand(query, currentConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                    MessageBox.Show($"Table '{schema}.{tableName}' truncated successfully.", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error truncating table: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CopyTable_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to copy.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            using (var dialog = new InputDialog($"Copy table '{schema}.{tableName}'", "New table name:", $"{tableName}_Copy"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string newTableName = dialog.InputValue;
                        string query = $"USE [{currentDatabaseName}]; SELECT * INTO [{schema}].[{newTableName}] FROM [{schema}].[{tableName}]";
                        using (var command = new SqlCommand(query, currentConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                        MessageBox.Show($"Table copied successfully to '{schema}.{newTableName}'.", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadTables(currentDatabaseName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error copying table: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RenameTable_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to rename.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            using (var dialog = new InputDialog($"Rename table '{schema}.{tableName}'", "New table name:", tableName))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string newTableName = dialog.InputValue;
                        string query = $"USE [{currentDatabaseName}]; EXEC sp_rename '[{schema}].[{tableName}]', '{newTableName}'";
                        using (var command = new SqlCommand(query, currentConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                        MessageBox.Show($"Table renamed successfully to '{schema}.{newTableName}'.", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadTables(currentDatabaseName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming table: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ViewTableIndexes_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to view indexes.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                var query = $@"
                    USE [{currentDatabaseName}];
                    SELECT 
                        i.name as [Index Name],
                        i.type_desc as [Index Type],
                        i.is_unique as [Is Unique],
                        i.is_primary_key as [Is Primary Key],
                        STRING_AGG(c.name, ', ') as [Columns]
                    FROM sys.indexes i
                    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    INNER JOIN sys.tables t ON i.object_id = t.object_id
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = @schema AND t.name = @table
                    GROUP BY i.name, i.type_desc, i.is_unique, i.is_primary_key
                    ORDER BY i.name";
                
                using (var command = new SqlCommand(query, currentConnection))
                {
                    command.Parameters.AddWithValue("@schema", schema);
                    command.Parameters.AddWithValue("@table", tableName);
                    
                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    
                    var indexForm = new Form()
                    {
                        Text = $"Indexes: {schema}.{tableName}",
                        Size = new Size(700, 400),
                        StartPosition = FormStartPosition.CenterParent
                    };
                    
                    var indexGrid = new DataGridView()
                    {
                        Dock = DockStyle.Fill,
                        DataSource = dataTable,
                        ReadOnly = true,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false
                    };
                    
                    indexForm.Controls.Add(indexGrid);
                    ThemeManager.ApplyThemeToDialog(indexForm);
                    indexForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing table indexes: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ViewTableConstraints_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to view constraints.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                var query = $@"
                    USE [{currentDatabaseName}];
                    SELECT 
                        tc.CONSTRAINT_NAME as [Constraint Name],
                        tc.CONSTRAINT_TYPE as [Constraint Type],
                        STRING_AGG(cc.COLUMN_NAME, ', ') as [Columns]
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    LEFT JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc 
                        ON tc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
                    WHERE tc.TABLE_SCHEMA = @schema AND tc.TABLE_NAME = @table
                    GROUP BY tc.CONSTRAINT_NAME, tc.CONSTRAINT_TYPE
                    ORDER BY tc.CONSTRAINT_TYPE, tc.CONSTRAINT_NAME";
                
                using (var command = new SqlCommand(query, currentConnection))
                {
                    command.Parameters.AddWithValue("@schema", schema);
                    command.Parameters.AddWithValue("@table", tableName);
                    
                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    
                    var constraintForm = new Form()
                    {
                        Text = $"Constraints: {schema}.{tableName}",
                        Size = new Size(600, 400),
                        StartPosition = FormStartPosition.CenterParent
                    };
                    
                    var constraintGrid = new DataGridView()
                    {
                        Dock = DockStyle.Fill,
                        DataSource = dataTable,
                        ReadOnly = true,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false
                    };
                    
                    constraintForm.Controls.Add(constraintGrid);
                    ThemeManager.ApplyThemeToDialog(constraintForm);
                    constraintForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing table constraints: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ViewTableStats_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to view statistics.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                var query = $@"
                    USE [{currentDatabaseName}];
                    SELECT 
                        'Row Count' as [Statistic],
                        COUNT(*) as [Value]
                    FROM [{schema}].[{tableName}]
                    UNION ALL
                    SELECT 
                        'Column Count' as [Statistic],
                        COUNT(*) as [Value]
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table";
                
                using (var command = new SqlCommand(query, currentConnection))
                {
                    command.Parameters.AddWithValue("@schema", schema);
                    command.Parameters.AddWithValue("@table", tableName);
                    
                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    
                    var statsForm = new Form()
                    {
                        Text = $"Statistics: {schema}.{tableName}",
                        Size = new Size(400, 300),
                        StartPosition = FormStartPosition.CenterParent
                    };
                    
                    var statsGrid = new DataGridView()
                    {
                        Dock = DockStyle.Fill,
                        DataSource = dataTable,
                        ReadOnly = true,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false
                    };
                    
                    statsForm.Controls.Add(statsGrid);
                    ThemeManager.ApplyThemeToDialog(statsForm);
                    statsForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing table statistics: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportTableData_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to export.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                using (var exportDialog = new SqlServerManager.UI.DataImportExportDialog(currentConnection, currentDatabaseName, schema, tableName, SqlServerManager.UI.DataImportExportDialog.OperationType.Export))
                {
                    ThemeManager.ApplyThemeToDialog(exportDialog);
                    FontManager.ApplyFontSize(exportDialog, currentFontScale);
                    
                    if (exportDialog.ShowDialog() == DialogResult.OK)
                    {
                        enhancedStatusBar.ShowMessage("Export completed successfully", MessageType.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening export dialog: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportTableData_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to import data into.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                using (var importDialog = new SqlServerManager.UI.DataImportExportDialog(currentConnection, currentDatabaseName, schema, tableName, SqlServerManager.UI.DataImportExportDialog.OperationType.Import))
                {
                    ThemeManager.ApplyThemeToDialog(importDialog);
                    FontManager.ApplyFontSize(importDialog, currentFontScale);
                    
                    if (importDialog.ShowDialog() == DialogResult.OK)
                    {
                        enhancedStatusBar.ShowMessage("Import completed successfully", MessageType.Success);
                        // Optionally refresh the table data view if it's currently open
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening import dialog: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ScriptTable_Click(object sender, EventArgs e)
        {
            if (tablesGridView.CurrentRow == null || tablesGridView.CurrentRow.Cells["Table Name"] == null)
            {
                MessageBox.Show("Please select a table to script.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tableName = tablesGridView.CurrentRow.Cells["Table Name"].Value.ToString();
            string schema = tablesGridView.CurrentRow.Cells["Schema"].Value.ToString();
            
            try
            {
                using (var scriptGenerator = new SqlServerManager.UI.TableScriptGenerator(currentConnection, currentDatabaseName, schema, tableName))
                {
                    ThemeManager.ApplyThemeToDialog(scriptGenerator);
                    FontManager.ApplyFontSize(scriptGenerator, currentFontScale);
                    scriptGenerator.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening script generator: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ShowDataImportWizard_Click(object sender, EventArgs e)
        {
            if (currentConnection == null || currentConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Please connect to a SQL Server instance first.", "No Connection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(currentDatabaseName))
            {
                MessageBox.Show("Please select a database first.", "No Database Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                using (var importDialog = new UI.DataImportExportDialog(currentConnection, currentDatabaseName, null, null, UI.DataImportExportDialog.OperationType.Import))
                {
                    ThemeManager.ApplyThemeToDialog(importDialog);
                    FontManager.ApplyFontSize(importDialog, currentFontScale);
                    
                    if (importDialog.ShowDialog() == DialogResult.OK)
                    {
                        enhancedStatusBar.ShowMessage("Data import completed successfully", MessageType.Success);
                        
                        // Refresh the current database view if we have one selected
                        if (!string.IsNullOrEmpty(currentDatabaseName))
                        {
                            LoadTables(currentDatabaseName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Data Import Wizard: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void Settings_Click(object sender, EventArgs e)
        {
            try
            {
                using (var settingsDialog = new UI.SettingsDialog())
                {
                    ThemeManager.ApplyThemeToDialog(settingsDialog);
                    FontManager.ApplyFontSize(settingsDialog, currentFontScale);
                    
                    if (settingsDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Settings have been saved by the dialog
                        // Reload settings to apply any changes
                        LoadSavedSettings();
                        ApplyCurrentSettings();
                        
                        enhancedStatusBar.ShowMessage("Settings updated successfully", MessageType.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Settings dialog: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError("Error opening Settings dialog: {Exception}", ex.Message);
            }
        }
        
        private void AdvancedSearchMenuItem_Click(object sender, EventArgs e)
        {
            if (currentConnection == null || currentConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Please connect to a SQL Server instance first.", "No Connection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                // Create a simple connection service wrapper for current connection
                var connectionService = new SimpleConnectionService(currentConnection, currentDatabaseName);
                
                using (var searchDialog = new UI.AdvancedSearchDialog(connectionService))
                {
                    // Apply current theme using the existing ThemeManager
                    ThemeManager.ApplyThemeToDialog(searchDialog);
                    FontManager.ApplyFontSize(searchDialog, currentFontScale);
                    
                    // Handle search result selections
                    searchDialog.SearchResultSelected += (s, args) =>
                    {
                        var result = args.Result;
                        
                        // Navigate to the found object
                        if (!string.IsNullOrEmpty(result.Database))
                        {
                            // Select the database in the list
                            for (int i = 0; i < databaseListBox.Items.Count; i++)
                            {
                                if (databaseListBox.Items[i].ToString().Equals(result.Database, StringComparison.OrdinalIgnoreCase))
                                {
                                    databaseListBox.SelectedIndex = i;
                                    LoadTables(result.Database);
                                    break;
                                }
                            }
                            
                            // If it's a table, select it in the tables view
                            if (!string.IsNullOrEmpty(result.Table))
                            {
                                mainTabControl.SelectedTab = tablesTab;
                                
                                // Find and select the table
                                for (int i = 0; i < tablesGridView.Rows.Count; i++)
                                {
                                    var row = tablesGridView.Rows[i];
                                    if (row.Cells["Table Name"].Value?.ToString().Equals(result.Table, StringComparison.OrdinalIgnoreCase) == true &&
                                        row.Cells["Schema"].Value?.ToString().Equals(result.Schema, StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        tablesGridView.ClearSelection();
                                        row.Selected = true;
                                        tablesGridView.FirstDisplayedScrollingRowIndex = i;
                                        break;
                                    }
                                }
                            }
                        }
                        
                        enhancedStatusBar.ShowMessage($"Navigated to search result: {result.Schema}.{result.Table}.{result.Field}", MessageType.Info);
                    };
                    
                    searchDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Advanced Search: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError("Error opening Advanced Search: {Exception}", ex.Message);
            }
        }
        
        // Auto-reconnect methods
        private async void TryAutoReconnectOnStartup()
        {
            try
            {
                if (autoReconnectManager != null)
                {
                    var success = await autoReconnectManager.TryAutoReconnectOnStartupAsync();
                    if (success)
                    {
                        LogToFile("Auto-reconnect on startup attempted");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error during auto-reconnect on startup: {ex.Message}");
            }
        }
        
        private void OnReconnectAttempted(object sender, AutoReconnectEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnReconnectAttempted(sender, e)));
                return;
            }
            
            var serverName = e.Connection?.Server ?? "Unknown";
            enhancedStatusBar.ShowMessage($"Auto-reconnecting to {serverName}... (Attempt {e.AttemptNumber})", MessageType.Info);
            LoggingService.LogInformation("Auto-reconnect attempt {AttemptNumber} to {ServerName}", e.AttemptNumber, serverName);
        }
        
        private void OnReconnectSucceeded(object sender, AutoReconnectEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnReconnectSucceeded(sender, e)));
                return;
            }
            
            try
            {
                var serverName = e.Connection?.Server ?? "Unknown";
                var database = e.Connection?.Database ?? "Unknown";
                
                // Build connection string and create new connection
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = e.Connection.Server,
                    InitialCatalog = e.Connection.Database,
                    IntegratedSecurity = e.Connection.AuthenticationType == "Windows",
                    TrustServerCertificate = true,
                    ConnectTimeout = 15
                };
                
                if (!builder.IntegratedSecurity && !string.IsNullOrEmpty(e.Connection.Username))
                {
                    builder.UserID = e.Connection.Username;
                }
                
                currentConnectionString = builder.ConnectionString;
                currentConnection = new SqlConnection(currentConnectionString);
                currentConnection.Open();
                
                connectionLabel.Text = $"Connected to: {currentConnection.DataSource}";
                connectionLabel.ForeColor = Color.Green;
                disconnectButton.Enabled = true;
                refreshButton.Enabled = true;
                importWizardButton.Enabled = true;
                connectButton.Enabled = true;
                
                enhancedStatusBar.SetConnectionStatus(true, $"Auto-reconnected to: {currentConnection.DataSource}");
                enhancedStatusBar.ShowMessage("Auto-reconnected successfully", MessageType.Success);
                
                // Update SQL Editor connection
                if (sqlEditor != null)
                {
                    sqlEditor.UpdateConnection(currentConnection);
                }
                
                // Load databases
                LoadDatabases();
                
                LoggingService.LogInformation("Auto-reconnected successfully to {ServerName}", serverName);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error updating UI after successful auto-reconnect: {Exception}", ex.Message);
                enhancedStatusBar.ShowMessage("Auto-reconnect succeeded but UI update failed", MessageType.Warning);
            }
        }
        
        private void OnReconnectFailed(object sender, AutoReconnectEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnReconnectFailed(sender, e)));
                return;
            }
            
            var serverName = e.Connection?.Server ?? "Unknown";
            var errorMessage = e.Exception?.Message ?? "Connection failed";
            
            enhancedStatusBar.ShowMessage($"Auto-reconnect failed: {errorMessage}", MessageType.Error);
            LoggingService.LogWarning("Auto-reconnect failed after {AttemptNumber} attempts to {ServerName}: {ErrorMessage}", 
                e.AttemptNumber, serverName, errorMessage);
        }
    }
    
    // Simple wrapper for ConnectionService to work with existing SqlConnection
    public class SimpleConnectionService : Services.IConnectionService
    {
        private readonly SqlConnection _connection;
        private readonly string _currentDatabase;
        
        public SimpleConnectionService(SqlConnection connection, string currentDatabase)
        {
            _connection = connection; // Allow null during initialization
            _currentDatabase = currentDatabase;
        }
        
        public bool IsConnected => _connection?.State == ConnectionState.Open;
        public string CurrentDatabase => _currentDatabase ?? "master";
        
        public async Task<T> ExecuteWithConnectionAsync<T>(Func<SqlConnection, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("No active database connection");
                
            return await operation(_connection, cancellationToken);
        }
        
        public async Task ExecuteWithConnectionAsync(Func<SqlConnection, CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                throw new InvalidOperationException("No active database connection");
                
            await operation(_connection, cancellationToken);
        }
    }
}
