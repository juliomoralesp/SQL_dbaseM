using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using SqlServerManager.Services;

namespace SqlServerManager.UI
{
    /// <summary>
    /// Dialog for selecting specific tables to include in advanced search operations
    /// </summary>
    public partial class TableSelectionDialog : Form
    {
        private readonly SqlConnection _connection;
        private readonly string _databaseName;
        private readonly List<string> _selectedTables = new List<string>();
        
        // UI Controls
        private CheckedListBox tablesCheckedListBox;
        private TextBox searchTextBox;
        private CheckBox selectAllCheckBox;
        private Button okButton;
        private Button cancelButton;
        private Label statusLabel;
        private ProgressBar loadingProgressBar;
        
        public TableSelectionDialog(SqlConnection connection, string databaseName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            
            InitializeComponent();
            LoadTablesAsync();
        }
        
        public List<string> SelectedTables => _selectedTables.ToList();
        
        private void InitializeComponent()
        {
            this.Text = $"Select Tables - {_databaseName}";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(400, 400);
            this.ShowIcon = false;
            this.MaximizeBox = false;
            
            CreateLayout();
            SetupEventHandlers();
        }
        
        private void CreateLayout()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10)
            };
            
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Search box
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Select all checkbox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Tables list
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Buttons
            
            this.Controls.Add(mainLayout);
            
            // Search box
            var searchPanel = new Panel { Dock = DockStyle.Fill };
            var searchLabel = new Label
            {
                Text = "Filter tables:",
                Dock = DockStyle.Left,
                Width = 80,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            searchTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Type to filter tables..."
            };
            
            searchPanel.Controls.Add(searchTextBox);
            searchPanel.Controls.Add(searchLabel);
            mainLayout.Controls.Add(searchPanel, 0, 0);
            
            // Select all checkbox
            selectAllCheckBox = new CheckBox
            {
                Text = "Select All Tables",
                Dock = DockStyle.Fill,
                Checked = true
            };
            mainLayout.Controls.Add(selectAllCheckBox, 0, 1);
            
            // Tables list
            var tablesPanel = new Panel { Dock = DockStyle.Fill };
            var tablesLabel = new Label
            {
                Text = "Available Tables:",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            
            tablesCheckedListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Font = new Font("Consolas", 9)
            };
            
            tablesPanel.Controls.Add(tablesCheckedListBox);
            tablesPanel.Controls.Add(tablesLabel);
            mainLayout.Controls.Add(tablesPanel, 0, 2);
            
            // Status
            var statusPanel = new Panel { Dock = DockStyle.Fill };
            loadingProgressBar = new ProgressBar
            {
                Dock = DockStyle.Right,
                Width = 150,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            
            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Loading tables...",
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            statusPanel.Controls.Add(statusLabel);
            statusPanel.Controls.Add(loadingProgressBar);
            mainLayout.Controls.Add(statusPanel, 0, 3);
            
            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            
            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(5, 5, 0, 5)
            };
            
            okButton = new Button
            {
                Text = "OK",
                Size = new Size(80, 35),
                DialogResult = DialogResult.OK,
                Margin = new Padding(5, 5, 5, 5),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);
            mainLayout.Controls.Add(buttonPanel, 0, 4);
            
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        
        private void SetupEventHandlers()
        {
            searchTextBox.TextChanged += SearchTextBox_TextChanged;
            selectAllCheckBox.CheckedChanged += SelectAllCheckBox_CheckedChanged;
            tablesCheckedListBox.ItemCheck += TablesCheckedListBox_ItemCheck;
            okButton.Click += OkButton_Click;
            
            this.Load += (s, e) => ApplyTheme();
        }
        
        private async void LoadTablesAsync()
        {
            try
            {
                loadingProgressBar.Visible = true;
                statusLabel.Text = "Loading tables...";
                tablesCheckedListBox.Items.Clear();
                
                if (_connection?.State != ConnectionState.Open)
                {
                    statusLabel.Text = "Error: Database connection is not available";
                    return;
                }
                
                var tables = await GetDatabaseTablesAsync();
                
                foreach (var table in tables.OrderBy(t => t.Schema).ThenBy(t => t.TableName))
                {
                    var displayText = $"{table.Schema}.{table.TableName}";
                    if (table.RowCount.HasValue)
                        displayText += $" ({table.RowCount:N0} rows)";
                    
                    tablesCheckedListBox.Items.Add(displayText, true);
                }
                
                statusLabel.Text = $"{tables.Count} tables loaded";
                UpdateSelectedTablesCount();
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error loading tables: {ex.Message}";
                LoggingService.LogError(ex, "Error loading tables for selection");
            }
            finally
            {
                loadingProgressBar.Visible = false;
            }
        }
        
        private async Task<List<TableInfo>> GetDatabaseTablesAsync()
        {
            var tables = new List<TableInfo>();
            
            var sql = @"
                SELECT 
                    s.name AS SchemaName,
                    t.name AS TableName,
                    p.rows AS [RowCount]
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                LEFT JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
                WHERE t.is_ms_shipped = 0 
                AND s.name NOT IN ('sys', 'information_schema')
                ORDER BY s.name, t.name";
            
            using var command = new SqlCommand(sql, _connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add(new TableInfo
                {
                    Schema = reader.GetString("SchemaName"),
                    TableName = reader.GetString("TableName"),
                    RowCount = reader.IsDBNull("RowCount") ? (long?)null : reader.GetInt64("RowCount")
                });
            }
            
            return tables;
        }
        
        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            var searchTerm = searchTextBox.Text.ToLowerInvariant();
            
            for (int i = 0; i < tablesCheckedListBox.Items.Count; i++)
            {
                var itemText = tablesCheckedListBox.Items[i].ToString().ToLowerInvariant();
                var visible = string.IsNullOrEmpty(searchTerm) || itemText.Contains(searchTerm);
                
                // Unfortunately, CheckedListBox doesn't support hiding items easily
                // We'll need to rebuild the list or use a different approach
            }
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                FilterTablesList(searchTerm);
            }
            else
            {
                LoadTablesAsync(); // Reload all tables
            }
        }
        
        private async void FilterTablesList(string searchTerm)
        {
            try
            {
                var allTables = await GetDatabaseTablesAsync();
                var filteredTables = allTables
                    .Where(t => $"{t.Schema}.{t.TableName}".ToLowerInvariant().Contains(searchTerm))
                    .ToList();
                
                tablesCheckedListBox.Items.Clear();
                
                foreach (var table in filteredTables.OrderBy(t => t.Schema).ThenBy(t => t.TableName))
                {
                    var displayText = $"{table.Schema}.{table.TableName}";
                    if (table.RowCount.HasValue)
                        displayText += $" ({table.RowCount:N0} rows)";
                    
                    tablesCheckedListBox.Items.Add(displayText, true);
                }
                
                statusLabel.Text = $"{filteredTables.Count} tables found";
                UpdateSelectedTablesCount();
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error filtering tables: {ex.Message}";
            }
        }
        
        private void SelectAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var isChecked = selectAllCheckBox.Checked;
            
            for (int i = 0; i < tablesCheckedListBox.Items.Count; i++)
            {
                tablesCheckedListBox.SetItemChecked(i, isChecked);
            }
            
            UpdateSelectedTablesCount();
        }
        
        private void TablesCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Use BeginInvoke to update after the check state has changed
            this.BeginInvoke(new Action(() =>
            {
                UpdateSelectedTablesCount();
                UpdateSelectAllState();
            }));
        }
        
        private void UpdateSelectedTablesCount()
        {
            var selectedCount = tablesCheckedListBox.CheckedItems.Count;
            var totalCount = tablesCheckedListBox.Items.Count;
            
            if (totalCount > 0)
            {
                statusLabel.Text = $"{selectedCount} of {totalCount} tables selected";
            }
        }
        
        private void UpdateSelectAllState()
        {
            var checkedCount = tablesCheckedListBox.CheckedItems.Count;
            var totalCount = tablesCheckedListBox.Items.Count;
            
            if (checkedCount == 0)
            {
                selectAllCheckBox.CheckState = CheckState.Unchecked;
            }
            else if (checkedCount == totalCount)
            {
                selectAllCheckBox.CheckState = CheckState.Checked;
            }
            else
            {
                selectAllCheckBox.CheckState = CheckState.Indeterminate;
            }
        }
        
        private void OkButton_Click(object sender, EventArgs e)
        {
            _selectedTables.Clear();
            
            foreach (var checkedItem in tablesCheckedListBox.CheckedItems)
            {
                var itemText = checkedItem.ToString();
                // Extract schema.tablename from display text (before any row count info)
                var tableName = itemText.Split('(')[0].Trim();
                _selectedTables.Add(tableName);
            }
            
            if (_selectedTables.Count == 0)
            {
                var result = MessageBox.Show(
                    "No tables are selected. Do you want to continue with no table filtering (search all tables)?",
                    "No Tables Selected",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.No)
                {
                    this.DialogResult = DialogResult.None; // Prevent dialog from closing
                    return;
                }
            }
            
            this.DialogResult = DialogResult.OK;
        }
        
        private void ApplyTheme()
        {
            try
            {
                ThemeManager.ApplyThemeToDialog(this);
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error applying theme to Table Selection dialog: {Message}", ex.Message);
            }
        }
        
        private class TableInfo
        {
            public string Schema { get; set; }
            public string TableName { get; set; }
            public long? RowCount { get; set; }
        }
    }
}
