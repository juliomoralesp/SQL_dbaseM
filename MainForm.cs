using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;
using System.IO;

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
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripButton connectButton;
        private ToolStripButton disconnectButton;
        private ToolStripButton refreshButton;
        private ToolStripButton quitButton;
        private ToolStripLabel connectionLabel;
        private ContextMenuStrip databaseContextMenu;
        private ContextMenuStrip tableContextMenu;
        private SplitContainer tablesSplitContainer;
        private Label tablesLabel;
        private Label columnsLabel;
        private string currentDatabaseName;
        private float currentFontScale = 1.2f; // Start with 20% larger fonts

        public MainForm()
        {
            InitializeComponent();
            LoadSavedConnections();
            LoadSavedSettings();
            ApplyCurrentSettings();
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
            
            mainToolStrip.Items.Add(connectButton);
            mainToolStrip.Items.Add(disconnectButton);
            mainToolStrip.Items.Add(new ToolStripSeparator());
            mainToolStrip.Items.Add(refreshButton);
            mainToolStrip.Items.Add(new ToolStripSeparator());
            mainToolStrip.Items.Add(connectionLabel);
            mainToolStrip.Items.Add(quitButton);

            // Create StatusStrip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusStrip.Items.Add(statusLabel);

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
            
            databaseContextMenu.Items.AddRange(new ToolStripItem[] {
                createDatabaseMenuItem,
                new ToolStripSeparator(),
                renameDatabaseMenuItem,
                deleteDatabaseMenuItem,
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
            var refreshTablesMenuItem = new ToolStripMenuItem("Refresh Tables", null, (s, e) => { 
                if (!string.IsNullOrEmpty(currentDatabaseName)) LoadTables(currentDatabaseName); 
            });
            
            tableContextMenu.Items.AddRange(new ToolStripItem[] {
                createTableMenuItem,
                editTableMenuItem,
                deleteTableMenuItem,
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

            // Add tabs to TabControl
            mainTabControl.TabPages.Add(databasesTab);
            mainTabControl.TabPages.Add(tablesTab);

            // Add controls to form
            this.Controls.Add(mainTabControl);
            this.Controls.Add(mainToolStrip);
            this.Controls.Add(mainMenuStrip);
            this.Controls.Add(statusStrip);
            this.MainMenuStrip = mainMenuStrip;
        }

        private void ConnectButton_Click(object sender, EventArgs e)
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
                    try
                    {
                        currentConnectionString = connectionDialog.ConnectionString;
                        currentConnection = new SqlConnection(currentConnectionString);
                        currentConnection.Open();
                        
                        // Test connection
                        using (var command = new SqlCommand("SELECT 1", currentConnection))
                        {
                            command.ExecuteScalar();
                        }
                        
                        // Save successful connection
                        SaveConnection(currentConnectionString);
                        
                        // Update UI
                        connectionLabel.Text = $"Connected to: {currentConnection.DataSource}";
                        connectionLabel.ForeColor = Color.Green;
                        connectButton.Enabled = false;
                        disconnectButton.Enabled = true;
                        refreshButton.Enabled = true;
                        statusLabel.Text = "Connected successfully";
                        
                        // Load databases
                        LoadDatabases();
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
                    }
                }
            }
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
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
            databaseListBox.Items.Clear();
            tablesGridView.DataSource = null;
            columnsGridView.DataSource = null;
            currentDatabaseName = null;
            statusLabel.Text = "Disconnected";
            
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

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadDatabases();
            if (databaseListBox.SelectedItem != null)
            {
                LoadTables(databaseListBox.SelectedItem.ToString());
            }
        }

        private void LoadDatabases()
        {
            try
            {
                databaseListBox.Items.Clear();
                using (var command = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", currentConnection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            databaseListBox.Items.Add(reader["name"].ToString());
                        }
                    }
                }
                statusLabel.Text = $"Loaded {databaseListBox.Items.Count} databases";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading databases: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void LoadTables(string databaseName)
        {
            // Check if connection is valid before attempting to load
            if (currentConnection == null || currentConnection.State != ConnectionState.Open)
            {
                tablesGridView.DataSource = null;
                tablesLabel.Text = "Tables: (Not connected)";
                statusLabel.Text = "Not connected";
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
                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    tablesGridView.DataSource = dataTable;
                    tablesLabel.Text = $"Tables in {databaseName}:";
                }
                statusLabel.Text = $"Loaded tables from {databaseName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tables: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            var disconnectMenuItem = new ToolStripMenuItem("&Disconnect", null, DisconnectButton_Click);
            var quitMenuItem = new ToolStripMenuItem("&Quit", null, QuitButton_Click);
            quitMenuItem.ShortcutKeys = Keys.Control | Keys.Q;
            
            fileMenu.DropDownItems.Add(connectMenuItem);
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

            // Help Menu
            var helpMenu = new ToolStripMenuItem("&Help");
            var aboutMenuItem = new ToolStripMenuItem("&About", null, (s, e) => 
            {
                MessageBox.Show("SQL Server Manager\nVersion 1.0\n\nA tool for managing SQL Server databases.", 
                    "About SQL Server Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            helpMenu.DropDownItems.Add(aboutMenuItem);

            mainMenuStrip.Items.Add(fileMenu);
            mainMenuStrip.Items.Add(viewMenu);
            mainMenuStrip.Items.Add(helpMenu);
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

        private void ApplyTheme(ThemeManager.Theme theme)
        {
            ThemeManager.ApplyTheme(this, theme);
            statusLabel.Text = $"Theme changed to {theme}";
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
            
            statusLabel.Text = $"Font size changed to {(int)(scale * 100)}%";
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
    }
}
