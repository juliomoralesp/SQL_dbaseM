using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using SqlServerManager.Services;
using SqlServerManager.UI;

namespace SqlServerManager.UI
{
    /// <summary>
    /// Modern Advanced Search Dialog with comprehensive database/table/field selection and editing capabilities
    /// </summary>
    public partial class AdvancedSearchDialog : Form
    {
        private readonly IConnectionService _connectionService;
        private readonly List<SearchResult> _searchResults = new List<SearchResult>();
        private readonly Dictionary<string, PendingChange> _pendingChanges = new Dictionary<string, PendingChange>();
        private CancellationTokenSource _searchCancellation;

        // Modern UI Controls
        private Panel headerPanel;
        private Label titleLabel;
        private Button closeButton;
        
        private TabControl mainTabControl;
        private TabPage searchTabPage;
        private TabPage resultsTabPage;
        
        // Search Controls
        private ComboBox databaseComboBox;
        private TreeView tableTreeView;
        private ListView fieldListView;
        private TextBox searchTextBox;
        private ComboBox searchTypeComboBox;
        private CheckBox caseSensitiveCheckBox;
        private CheckBox regexCheckBox;
        private Button searchButton;
        private Button clearButton;
        
        // Results Controls
        private DataGridView resultsGridView;
private ToolStrip resultsToolStrip;
        private ToolStripButton saveButton;
        private ToolStripButton editButton;
        private ToolStripButton deleteButton;
        private ToolStripButton exportButton;
        private ToolStripSeparator separator1;
        private ToolStripLabel statusLabel;
        private ToolStripLabel modifiedLabel;
        
        // Record Detail Controls
        private SplitContainer resultsSplitContainer;
        private GroupBox recordDetailsGroupBox;
        private DataGridView recordDetailsGridView;
        private Label recordInfoLabel;
        
        // Progress and Status
        private ProgressBar progressBar;
        private Label statusInfoLabel;

        public AdvancedSearchDialog(IConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            InitializeComponent();
            _ = LoadDatabases(); // Fire and forget - databases will load asynchronously
            ApplyModernTheme();
            
            // Let Windows Forms handle the SplitContainer automatically
            // No manual SplitterDistance setting to avoid constraint violations
        }

        public event EventHandler<SearchResultSelectedEventArgs> SearchResultSelected;
        public event EventHandler<SearchResultEditEventArgs> SearchResultEdit;

        private void InitializeComponent()
        {
            this.Text = "Advanced Database Search";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(1000, 600);
            this.ShowIcon = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            CreateHeaderPanel();
            CreateMainContent();
            SetupEventHandlers();
            
            // Safe way to set splitter after form is fully shown and sized
            this.Shown += (s, e) => SetupSplitterAfterShow();
        }

        private void SetupSplitterAfterShow()
        {
            if (resultsSplitContainer == null) return;
            
            try
            {
                // Use BeginInvoke to ensure form is fully rendered and sized
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var totalHeight = resultsSplitContainer.Height;
                        var splitterWidth = resultsSplitContainer.SplitterWidth;
                        var panel1MinSize = resultsSplitContainer.Panel1MinSize;
                        var panel2MinSize = resultsSplitContainer.Panel2MinSize;
                        
                        // Calculate available space for splitting
                        var availableHeight = totalHeight - splitterWidth;
                        
                        if (availableHeight > (panel1MinSize + panel2MinSize))
                        {
                            // Set splitter to approximately 60% for results, 40% for details
                            var desiredPanel1Height = (int)(availableHeight * 0.6);
                            
                            // Ensure splitter distance is within valid bounds
                            var minDistance = panel1MinSize;
                            var maxDistance = totalHeight - panel2MinSize - splitterWidth;
                            
                            var splitterDistance = Math.Max(minDistance, Math.Min(maxDistance, desiredPanel1Height));
                            
                            LoggingService.LogDebug("Setting SplitterDistance: TotalHeight={TotalHeight}, AvailableHeight={AvailableHeight}, SplitterDistance={SplitterDistance}, MinDistance={MinDistance}, MaxDistance={MaxDistance}",
                                totalHeight, availableHeight, splitterDistance, minDistance, maxDistance);
                            
                            resultsSplitContainer.SplitterDistance = splitterDistance;
                            LoggingService.LogDebug("Successfully set SplitterDistance to {SplitterDistance}", splitterDistance);
                        }
                        else
                        {
                            LoggingService.LogWarning("Insufficient space for splitter: TotalHeight={TotalHeight}, Required={Required}", 
                                totalHeight, panel1MinSize + panel2MinSize + splitterWidth);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        LoggingService.LogWarning("Inner error setting SplitterDistance: {Message}", innerEx.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error setting SplitterDistance: {Message}", ex.Message);
            }
        }

        private void CreateHeaderPanel()
        {
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(20, 15, 20, 15)
            };

            titleLabel = new Label
            {
                Text = "Advanced Database Search",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 18)
            };

            closeButton = new Button
            {
                Text = "✕",
                Size = new Size(30, 30),
                Location = new Point(headerPanel.Width - 50, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 255, 255, 30);

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(closeButton);
            this.Controls.Add(headerPanel);
        }

        private void CreateMainContent()
        {
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Appearance = TabAppearance.FlatButtons
            };

            // Search Tab
            searchTabPage = new TabPage("Search Setup")
            {
                BackColor = Color.FromArgb(245, 245, 245)
            };
            CreateSearchTab();
            mainTabControl.TabPages.Add(searchTabPage);

            // Results Tab
            resultsTabPage = new TabPage("Search Results")
            {
                BackColor = Color.FromArgb(245, 245, 245)
            };
            CreateResultsTab();
            mainTabControl.TabPages.Add(resultsTabPage);

            this.Controls.Add(mainTabControl);

            // Status Panel
            var statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                BackColor = Color.FromArgb(230, 230, 230),
                Padding = new Padding(10, 5, 10, 5)
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Left,
                Width = 200,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            statusInfoLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ready to search",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };

            statusPanel.Controls.Add(statusInfoLabel);
            statusPanel.Controls.Add(progressBar);
            this.Controls.Add(statusPanel);
        }

        private void CreateSearchTab()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(20)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            // Database Selection Panel
            var databasePanel = CreateDatabasePanel();
            mainLayout.Controls.Add(databasePanel, 0, 0);

            // Table Selection Panel
            var tablePanel = CreateTablePanel();
            mainLayout.Controls.Add(tablePanel, 1, 0);

            // Field Selection and Search Options Panel
            var searchPanel = CreateSearchOptionsPanel();
            mainLayout.Controls.Add(searchPanel, 2, 0);

            // Search Action Panel
            var actionPanel = CreateSearchActionPanel();
            mainLayout.Controls.Add(actionPanel, 0, 1);
            mainLayout.SetColumnSpan(actionPanel, 3);

            searchTabPage.Controls.Add(mainLayout);
        }

        private GroupBox CreateDatabasePanel()
        {
            var panel = new GroupBox
            {
                Text = "Database Selection",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(10)
            };

            databaseComboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                Height = 30
            };

            var refreshDbButton = new Button
            {
                Text = "Refresh Databases",
                Dock = DockStyle.Bottom,
                Height = 35,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(240, 240, 240),
                FlatStyle = FlatStyle.Flat
            };
            refreshDbButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);

            panel.Controls.Add(databaseComboBox);
            panel.Controls.Add(refreshDbButton);

            refreshDbButton.Click += async (s, e) => await LoadDatabases();

            return panel;
        }

        private GroupBox CreateTablePanel()
        {
            var panel = new GroupBox
            {
                Text = "Table Selection",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(10)
            };

            tableTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };

            var tableToolStrip = new ToolStrip
            {
                Dock = DockStyle.Bottom,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var selectAllButton = new ToolStripButton("Select All")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            var selectNoneButton = new ToolStripButton("Select None")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            var refreshTablesButton = new ToolStripButton("Refresh")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };

            tableToolStrip.Items.AddRange(new ToolStripItem[] 
            { 
                selectAllButton, 
                new ToolStripSeparator(), 
                selectNoneButton,
                new ToolStripSeparator(),
                refreshTablesButton
            });

            panel.Controls.Add(tableTreeView);
            panel.Controls.Add(tableToolStrip);

            // Event handlers
            selectAllButton.Click += (s, e) => SetAllTablesChecked(true);
            selectNoneButton.Click += (s, e) => SetAllTablesChecked(false);
            refreshTablesButton.Click += async (s, e) => await LoadTables();
            tableTreeView.AfterCheck += TableTreeView_AfterCheck;

            return panel;
        }

        private GroupBox CreateSearchOptionsPanel()
        {
            var panel = new GroupBox
            {
                Text = "Search Configuration",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };

            // Field Selection
            var fieldLabel = new Label
            {
                Text = "Fields to Search:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true
            };

            fieldListView = new ListView
            {
                View = View.List,
                CheckBoxes = true,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle,
                Height = 120
            };

            // Search Text
            var searchLabel = new Label
            {
                Text = "Search Text:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true
            };

            searchTextBox = new TextBox
            {
                Font = new Font("Consolas", 10),
                Height = 25,
                Dock = DockStyle.Top
            };

            // Search Type
            var typeLabel = new Label
            {
                Text = "Search Type:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true
            };

            searchTypeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                Dock = DockStyle.Top
            };
            searchTypeComboBox.Items.AddRange(new[] 
            { 
                "Contains", "Starts With", "Ends With", "Exact Match", "Regular Expression" 
            });
            searchTypeComboBox.SelectedIndex = 0;

            // Options
            var optionsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false
            };

            caseSensitiveCheckBox = new CheckBox
            {
                Text = "Case Sensitive",
                Font = new Font("Segoe UI", 9),
                AutoSize = true
            };

            regexCheckBox = new CheckBox
            {
                Text = "Use Regular Expression",
                Font = new Font("Segoe UI", 9),
                AutoSize = true
            };

            optionsPanel.Controls.AddRange(new Control[] { caseSensitiveCheckBox, regexCheckBox });

            layout.Controls.Add(fieldLabel, 0, 0);
            layout.Controls.Add(fieldListView, 0, 1);
            layout.Controls.Add(searchLabel, 0, 2);
            layout.Controls.Add(searchTextBox, 0, 3);
            layout.Controls.Add(typeLabel, 0, 4);
            layout.Controls.Add(searchTypeComboBox, 0, 5);
            layout.Controls.Add(optionsPanel, 0, 6);

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateSearchActionPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 60
            };

            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 300,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10)
            };

            searchButton = new Button
            {
                Text = "Search",
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5)
            };
            searchButton.FlatAppearance.BorderSize = 0;

            clearButton = new Button
            {
                Text = "Clear",
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(120, 120, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5)
            };
            clearButton.FlatAppearance.BorderSize = 0;

            buttonLayout.Controls.AddRange(new Control[] { clearButton, searchButton });
            panel.Controls.Add(buttonLayout);

            return panel;
        }

        private void CreateResultsTab()
        {
            // Results ToolStrip
            resultsToolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(240, 240, 240),
                Height = 35
            };

            saveButton = new ToolStripButton("Save Changes")
            {
                Text = "Save Changes",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White
            };
            
            editButton = new ToolStripButton("Edit Selected")
            {
                Text = "Edit Selected",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false
            };

            deleteButton = new ToolStripButton("Delete Selected")
            {
                Text = "Delete Selected",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false
            };

            exportButton = new ToolStripButton("Export Results")
            {
                Text = "Export Results",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            separator1 = new ToolStripSeparator();

            statusLabel = new ToolStripLabel("No results")
            {
                TextAlign = ContentAlignment.MiddleRight
            };
            
            modifiedLabel = new ToolStripLabel("")
            {
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.OrangeRed,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            resultsToolStrip.Items.AddRange(new ToolStripItem[]
            {
                saveButton, new ToolStripSeparator(), editButton, new ToolStripSeparator(), deleteButton, new ToolStripSeparator(), exportButton,
                new ToolStripSeparator(), modifiedLabel, statusLabel
            });

            // Create split container for results and record details
            // Use completely default settings to avoid any SplitterDistance constraint issues
            resultsSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                // Use minimal minimum sizes to prevent constraint violations
                Panel1MinSize = 25,
                Panel2MinSize = 25,
                SplitterWidth = 4
                // Let Windows Forms calculate SplitterDistance automatically
            };

            // Results DataGridView (top panel)
            resultsGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoGenerateColumns = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Segoe UI", 9),
                RowHeadersWidth = 25
            };

            // Configure columns
            resultsGridView.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn 
                { 
                    Name = "Database", 
                    HeaderText = "Database", 
                    Width = 120,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn 
                { 
                    Name = "Table", 
                    HeaderText = "Table", 
                    Width = 150,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn 
                { 
                    Name = "Field", 
                    HeaderText = "Field", 
                    Width = 120,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn 
                { 
                    Name = "Value", 
                    HeaderText = "Value", 
                    Width = 200,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                },
                new DataGridViewTextBoxColumn 
                { 
                    Name = "DataType", 
                    HeaderText = "Data Type", 
                    Width = 100,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn 
                { 
                    Name = "RowId", 
                    HeaderText = "Row ID", 
                    Width = 80,
                    ReadOnly = true,
                    Visible = false
                }
            });

            // Make Value column editable
            resultsGridView.Columns["Value"].ReadOnly = false;

            // Record Details Panel (bottom panel)
            recordDetailsGroupBox = new GroupBox
            {
                Text = "Complete Record Details",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(10)
            };

            recordInfoLabel = new Label
            {
                Text = "Select a search result to view complete record details",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            recordDetailsGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Segoe UI", 9),
                RowHeadersWidth = 25,
                ColumnHeadersHeight = 30
            };

            // Configure record details columns
            recordDetailsGridView.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "FieldName",
                    HeaderText = "Field Name",
                    Width = 150,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 245, 245) }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "FieldValue",
                    HeaderText = "Value",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    ReadOnly = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "FieldType",
                    HeaderText = "Data Type",
                    Width = 100,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 245, 245) }
                }
            });

            recordDetailsGroupBox.Controls.Add(recordDetailsGridView);
            recordDetailsGroupBox.Controls.Add(recordInfoLabel);

            // Add panels to split container
            resultsSplitContainer.Panel1.Controls.Add(resultsGridView);
            resultsSplitContainer.Panel2.Controls.Add(recordDetailsGroupBox);

            // Add controls to results tab (toolbar first, then split container)
            resultsTabPage.Controls.Add(resultsToolStrip);
            resultsTabPage.Controls.Add(resultsSplitContainer);
        }

        private void SetupEventHandlers()
        {
            closeButton.Click += (s, e) => this.Close();
            databaseComboBox.SelectedIndexChanged += DatabaseComboBox_SelectedIndexChanged;
            searchButton.Click += SearchButton_Click;
            clearButton.Click += ClearButton_Click;
            searchTextBox.KeyDown += SearchTextBox_KeyDown;
            regexCheckBox.CheckedChanged += RegexCheckBox_CheckedChanged;

            // Results events
            saveButton.Click += SaveButton_Click;
            editButton.Click += EditButton_Click;
            deleteButton.Click += DeleteButton_Click;
            exportButton.Click += ExportButton_Click;
            resultsGridView.CellDoubleClick += ResultsGridView_CellDoubleClick;
            resultsGridView.CellValueChanged += ResultsGridView_CellValueChanged;
            resultsGridView.SelectionChanged += ResultsGridView_SelectionChanged;
            
            // Record details events
            recordDetailsGridView.CellValueChanged += RecordDetailsGridView_CellValueChanged;
            recordDetailsGridView.KeyDown += RecordDetailsGridView_KeyDown;
            
            // Let Windows Forms automatically manage the splitter distance
            // No manual setting to avoid constraint violations
        }

        private async Task LoadDatabases()
        {
            if (!_connectionService.IsConnected)
            {
                statusInfoLabel.Text = "Not connected to database";
                return;
            }

            try
            {
                databaseComboBox.Items.Clear();
                statusInfoLabel.Text = "Loading databases...";

                await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    var sql = "SELECT name FROM sys.databases WHERE state = 0 ORDER BY name";
                    using var cmd = new SqlCommand(sql, conn);
                    using var reader = await cmd.ExecuteReaderAsync(ct);

                    var databases = new List<string>();
                    while (await reader.ReadAsync(ct))
                    {
                        databases.Add(reader.GetString(0));
                    }

                    this.Invoke(() =>
                    {
                        foreach (var db in databases)
                        {
                            databaseComboBox.Items.Add(db);
                        }

                        if (databases.Contains(_connectionService.CurrentDatabase))
                        {
                            databaseComboBox.SelectedItem = _connectionService.CurrentDatabase;
                        }
                        else if (databaseComboBox.Items.Count > 0)
                        {
                            databaseComboBox.SelectedIndex = 0;
                        }

                        statusInfoLabel.Text = $"Loaded {databases.Count} databases";
                    });

                    return Task.CompletedTask;
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                statusInfoLabel.Text = $"Error loading databases: {ex.Message}";
                LoggingService.LogError(ex, "Error loading databases for advanced search");
            }
        }

        private async Task LoadTables()
        {
            if (databaseComboBox.SelectedItem == null) return;

            var selectedDatabase = databaseComboBox.SelectedItem.ToString();
            
            try
            {
                tableTreeView.Nodes.Clear();
                fieldListView.Items.Clear();
                statusInfoLabel.Text = "Loading tables...";

                await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    // Switch to selected database
                    using (var switchCmd = new SqlCommand($"USE [{selectedDatabase}]", conn))
                    {
                        await switchCmd.ExecuteNonQueryAsync(ct);
                    }

                    // Load tables grouped by schema
                    var sql = @"
                        SELECT s.name AS SchemaName, t.name AS TableName, 
                               COUNT(c.column_id) as ColumnCount
                        FROM sys.tables t
                        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                        LEFT JOIN sys.columns c ON t.object_id = c.object_id
                        WHERE t.is_ms_shipped = 0
                        GROUP BY s.name, t.name
                        ORDER BY s.name, t.name";

                    using var cmd = new SqlCommand(sql, conn);
                    using var reader = await cmd.ExecuteReaderAsync(ct);

                    var schemaGroups = new Dictionary<string, List<(string TableName, int ColumnCount)>>();

                    while (await reader.ReadAsync(ct))
                    {
                        var schema = reader.GetString("SchemaName");
                        var table = reader.GetString("TableName");
                        var columnCount = reader.GetInt32("ColumnCount");

                        if (!schemaGroups.ContainsKey(schema))
                        {
                            schemaGroups[schema] = new List<(string, int)>();
                        }

                        schemaGroups[schema].Add((table, columnCount));
                    }

                    this.Invoke(() =>
                    {
                        foreach (var schema in schemaGroups)
                        {
                            var schemaNode = new TreeNode($"{schema.Key} ({schema.Value.Count} tables)")
                            {
                                Tag = schema.Key,
                                ImageIndex = 0
                            };

                            foreach (var (tableName, columnCount) in schema.Value)
                            {
                                var tableNode = new TreeNode($"{tableName} ({columnCount} columns)")
                                {
                                    Tag = new { Schema = schema.Key, Table = tableName },
                                    Checked = true
                                };
                                schemaNode.Nodes.Add(tableNode);
                            }

                            schemaNode.Checked = true;
                            schemaNode.Expand();
                            tableTreeView.Nodes.Add(schemaNode);
                        }

                        statusInfoLabel.Text = $"Loaded {schemaGroups.Values.Sum(v => v.Count)} tables";
                    });

                    return Task.CompletedTask;
                }, CancellationToken.None);

                // Load fields for selected tables
                await LoadFields();
            }
            catch (Exception ex)
            {
                statusInfoLabel.Text = $"Error loading tables: {ex.Message}";
                LoggingService.LogError(ex, "Error loading tables for advanced search");
            }
        }

        private async Task LoadFields()
        {
            var selectedTables = GetSelectedTables();
            if (!selectedTables.Any()) return;

            var selectedDatabase = databaseComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedDatabase)) return;

            try
            {
                fieldListView.Items.Clear();
                statusInfoLabel.Text = "Loading fields...";

                await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    // Switch to selected database
                    using (var switchCmd = new SqlCommand($"USE [{selectedDatabase}]", conn))
                    {
                        await switchCmd.ExecuteNonQueryAsync(ct);
                    }

                    var fields = new List<(string Schema, string Table, string Column, string DataType)>();

                    foreach (var (schema, table) in selectedTables)
                    {
                        var sql = @"
                            SELECT c.COLUMN_NAME, c.DATA_TYPE
                            FROM INFORMATION_SCHEMA.COLUMNS c
                            WHERE c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @Table
                            ORDER BY c.ORDINAL_POSITION";

                        using var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@Schema", schema);
                        cmd.Parameters.AddWithValue("@Table", table);

                        using var reader = await cmd.ExecuteReaderAsync(ct);
                        while (await reader.ReadAsync(ct))
                        {
                            fields.Add((schema, table, reader.GetString(0), reader.GetString(1)));
                        }
                    }

                    this.Invoke(() =>
                    {
                        foreach (var (schema, table, column, dataType) in fields)
                        {
                            var item = new ListViewItem($"{schema}.{table}.{column}")
                            {
                                Tag = new { Schema = schema, Table = table, Column = column, DataType = dataType },
                                Checked = true
                            };
                            item.SubItems.Add(dataType);
                            fieldListView.Items.Add(item);
                        }

                        statusInfoLabel.Text = $"Loaded {fields.Count} fields from {selectedTables.Count()} tables";
                    });

                    return Task.CompletedTask;
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                statusInfoLabel.Text = $"Error loading fields: {ex.Message}";
                LoggingService.LogError(ex, "Error loading fields for advanced search");
            }
        }

        private IEnumerable<(string Schema, string Table)> GetSelectedTables()
        {
            var selectedTables = new List<(string, string)>();

            foreach (TreeNode schemaNode in tableTreeView.Nodes)
            {
                foreach (TreeNode tableNode in schemaNode.Nodes)
                {
                    if (tableNode.Checked && tableNode.Tag is object tag)
                    {
                        var schemaName = ((dynamic)tag).Schema;
                        var tableName = ((dynamic)tag).Table;
                        selectedTables.Add((schemaName, tableName));
                    }
                }
            }

            return selectedTables;
        }

        private IEnumerable<(string Schema, string Table, string Column, string DataType)> GetSelectedFields()
        {
            return fieldListView.CheckedItems.Cast<ListViewItem>()
                .Select(item => 
                {
                    var tag = (dynamic)item.Tag;
                    return ((string)tag.Schema, (string)tag.Table, (string)tag.Column, (string)tag.DataType);
                });
        }

        private void SetAllTablesChecked(bool isChecked)
        {
            foreach (TreeNode schemaNode in tableTreeView.Nodes)
            {
                schemaNode.Checked = isChecked;
                foreach (TreeNode tableNode in schemaNode.Nodes)
                {
                    tableNode.Checked = isChecked;
                }
            }
        }

        private async void DatabaseComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadTables();
        }

        private async void TableTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Update child nodes when parent is checked
            if (e.Node.Nodes.Count > 0)
            {
                foreach (TreeNode child in e.Node.Nodes)
                {
                    child.Checked = e.Node.Checked;
                }
            }

            // Update parent node when child is checked
            if (e.Node.Parent != null)
            {
                var allChecked = e.Node.Parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
                var anyChecked = e.Node.Parent.Nodes.Cast<TreeNode>().Any(n => n.Checked);
                
                if (allChecked)
                    e.Node.Parent.Checked = true;
                else if (!anyChecked)
                    e.Node.Parent.Checked = false;
            }

            await LoadFields();
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            await PerformSearch();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            searchTextBox.Clear();
            _searchResults.Clear();
            resultsGridView.Rows.Clear();
            statusLabel.Text = "No results";
            statusInfoLabel.Text = "Ready to search";
        }

        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await PerformSearch();
            }
        }

        private void RegexCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (regexCheckBox.Checked)
            {
                searchTypeComboBox.SelectedIndex = 4; // Regular Expression
            }
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                MessageBox.Show("Please enter search text.", "Search Text Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedFields = GetSelectedFields().ToList();
            if (!selectedFields.Any())
            {
                MessageBox.Show("Please select at least one field to search.", "Field Selection Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedDatabase = databaseComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedDatabase))
            {
                MessageBox.Show("Please select a database.", "Database Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();

            try
            {
                searchButton.Enabled = false;
                progressBar.Visible = true;
                statusInfoLabel.Text = "Searching...";
                _searchResults.Clear();
                resultsGridView.Rows.Clear();

                var results = await ExecuteSearch(selectedDatabase, selectedFields, _searchCancellation.Token);

                _searchResults.AddRange(results);
                DisplayResults(results);

                mainTabControl.SelectedTab = resultsTabPage;
                statusInfoLabel.Text = $"Found {results.Count} results";
                statusLabel.Text = $"{results.Count} results found";
            }
            catch (OperationCanceledException)
            {
                statusInfoLabel.Text = "Search cancelled";
            }
            catch (Exception ex)
            {
                statusInfoLabel.Text = $"Search failed: {ex.Message}";
                MessageBox.Show($"Search failed: {ex.Message}", "Search Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError(ex, "Error performing advanced search");
            }
            finally
            {
                searchButton.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private async Task<List<SearchResult>> ExecuteSearch(
            string database, 
            IEnumerable<(string Schema, string Table, string Column, string DataType)> fields, 
            CancellationToken cancellationToken)
        {
            var results = new List<SearchResult>();
            var searchText = searchTextBox.Text;
            var searchType = searchTypeComboBox.SelectedItem?.ToString() ?? "Contains";
            var caseSensitive = caseSensitiveCheckBox.Checked;

            await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
            {
                // Switch to selected database
                using (var switchCmd = new SqlCommand($"USE [{database}]", conn))
                {
                    await switchCmd.ExecuteNonQueryAsync(ct);
                }

                // Group fields by table for efficient querying
                var tableGroups = fields.GroupBy(f => new { f.Schema, f.Table });

                foreach (var tableGroup in tableGroups)
                {
                    ct.ThrowIfCancellationRequested();

                    var schema = tableGroup.Key.Schema;
                    var table = tableGroup.Key.Table;
                    var columns = tableGroup.Select(f => f.Column).ToList();

                    try
                    {
                        // Build dynamic search query
                        var conditions = new List<string>();
                        var parameters = new List<SqlParameter>();

                        for (int i = 0; i < columns.Count; i++)
                        {
                            var column = columns[i];
                            var paramName = $"@searchText{i}";
                            
                            string condition = searchType switch
                            {
                                "Starts With" => $"[{column}] LIKE {paramName} + '%'",
                                "Ends With" => $"[{column}] LIKE '%' + {paramName}",
                                "Exact Match" => caseSensitive ? 
                                    $"[{column}] = {paramName}" : 
                                    $"LOWER([{column}]) = LOWER({paramName})",
                                "Regular Expression" => $"[{column}] LIKE {paramName}", // Simplified regex
                                _ => caseSensitive ? 
                                    $"[{column}] LIKE '%' + {paramName} + '%'" : 
                                    $"LOWER([{column}]) LIKE LOWER('%' + {paramName} + '%')"
                            };

                            conditions.Add($"({condition} AND [{column}] IS NOT NULL)");
                            parameters.Add(new SqlParameter(paramName, searchText));
                        }

                        if (conditions.Any())
                        {
                            var whereClause = string.Join(" OR ", conditions);
                            var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
                            
                            var sql = $@"
                                SELECT TOP 1000 {columnList}
                                FROM [{schema}].[{table}]
                                WHERE {whereClause}";

                            using var cmd = new SqlCommand(sql, conn);
                            cmd.Parameters.AddRange(parameters.ToArray());

                            using var reader = await cmd.ExecuteReaderAsync(ct);
                            while (await reader.ReadAsync(ct))
                            {
                                for (int i = 0; i < columns.Count; i++)
                                {
                                    var value = reader.GetValue(i)?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(value) && MatchesSearchCriteria(value, searchText, searchType, caseSensitive))
                                    {
                                        var fieldInfo = fields.First(f => f.Schema == schema && f.Table == table && f.Column == columns[i]);
                                        
                                        results.Add(new SearchResult
                                        {
                                            Database = database,
                                            Schema = schema,
                                            Table = table,
                                            Field = columns[i],
                                            Value = value,
                                            DataType = fieldInfo.DataType,
                                            RowIdentifier = GenerateRowId(reader, columns[i])
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogWarning("Error searching {Schema}.{Table}: {Message}", 
                            schema, table, ex.Message);
                    }
                }

                return Task.CompletedTask;
            }, cancellationToken);

            return results;
        }

        private bool MatchesSearchCriteria(string value, string searchText, string searchType, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(value)) return false;

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            return searchType switch
            {
                "Starts With" => value.StartsWith(searchText, comparison),
                "Ends With" => value.EndsWith(searchText, comparison),
                "Exact Match" => value.Equals(searchText, comparison),
                "Regular Expression" => IsRegexMatch(value, searchText, caseSensitive),
                _ => value.Contains(searchText, comparison)
            };
        }

        private bool IsRegexMatch(string value, string pattern, bool caseSensitive)
        {
            try
            {
                var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                return Regex.IsMatch(value, pattern, options);
            }
            catch
            {
                return false;
            }
        }

        private string GenerateRowId(SqlDataReader reader, string primaryColumn)
        {
            // Simple row identification - in real implementation, use actual primary keys
            return $"{reader.GetHashCode()}";
        }

        private void DisplayResults(List<SearchResult> results)
        {
            resultsGridView.Rows.Clear();

            foreach (var result in results)
            {
                resultsGridView.Rows.Add(
                    result.Database,
                    $"{result.Schema}.{result.Table}",
                    result.Field,
                    result.Value,
                    result.DataType,
                    result.RowIdentifier
                );
            }

            resultsGridView.AutoResizeColumns();
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (resultsGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to edit.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = resultsGridView.SelectedRows[0];
            var resultIndex = selectedRow.Index;
            
            if (resultIndex < _searchResults.Count)
            {
                var result = _searchResults[resultIndex];
                SearchResultEdit?.Invoke(this, new SearchResultEditEventArgs(result));
            }
        }

        private async void DeleteButton_Click(object sender, EventArgs e)
        {
            if (resultsGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select rows to delete.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get selected search results
            var selectedResults = new List<(SearchResult result, DataGridViewRow row)>();
            foreach (DataGridViewRow row in resultsGridView.SelectedRows)
            {
                if (row.Index < _searchResults.Count)
                {
                    selectedResults.Add((_searchResults[row.Index], row));
                }
            }

            if (selectedResults.Count == 0) return;

            // Show detailed confirmation dialog
            var confirmationMessage = "Are you sure you want to delete the following records?\n\n";
            foreach (var (result, _) in selectedResults.Take(5)) // Show up to 5 examples
            {
                confirmationMessage += $"• {result.Schema}.{result.Table} where {result.Field} = '{result.Value}'\n";
            }
            if (selectedResults.Count > 5)
            {
                confirmationMessage += $"... and {selectedResults.Count - 5} more records\n";
            }
            confirmationMessage += "\n⚠️ This operation cannot be undone!";

            var dialogResult = MessageBox.Show(
                confirmationMessage,
                "Confirm Delete Operation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (dialogResult != DialogResult.Yes) return;

            // Execute deletions
            deleteButton.Enabled = false;
            progressBar.Visible = true;
            statusInfoLabel.Text = "Deleting records...";

            try
            {
                int successCount = 0;
                int errorCount = 0;
                var errorMessages = new List<string>();

                foreach (var (result, row) in selectedResults)
                {
                    try
                    {
                        var success = await ExecuteRecordDelete(result);
                        if (success)
                        {
                            successCount++;
                            // Remove from search results and UI
                            _searchResults.Remove(result);
                            resultsGridView.Rows.Remove(row);
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errorMessages.Add($"{result.Schema}.{result.Table}: {ex.Message}");
                        LoggingService.LogError(ex, "Error deleting record from {Schema}.{Table}", result.Schema, result.Table);
                    }
                }

                // Update status and show results
                var message = $"Delete operation completed:\n• {successCount} records deleted successfully";
                if (errorCount > 0)
                {
                    message += $"\n• {errorCount} records failed to delete";
                    if (errorMessages.Any())
                    {
                        message += "\n\nErrors:\n" + string.Join("\n", errorMessages.Take(5));
                        if (errorMessages.Count > 5)
                        {
                            message += $"\n... and {errorMessages.Count - 5} more errors";
                        }
                    }
                }

                MessageBox.Show(message, "Delete Results", 
                    MessageBoxButtons.OK, 
                    errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                statusInfoLabel.Text = $"Deleted {successCount} records. {_searchResults.Count} results remaining";
                statusLabel.Text = $"{_searchResults.Count} results";
                
                // Clear record details if no selection
                if (resultsGridView.SelectedRows.Count == 0)
                {
                    ClearRecordDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during delete operation: {ex.Message}", "Delete Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusInfoLabel.Text = "Delete operation failed";
            }
            finally
            {
                deleteButton.Enabled = resultsGridView.SelectedRows.Count > 0;
                progressBar.Visible = false;
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (_searchResults.Count == 0)
            {
                MessageBox.Show("No results to export.", "No Data", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"SearchResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ExportToCsv(dialog.FileName);
                    MessageBox.Show($"Results exported to {dialog.FileName}", "Export Complete", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToCsv(string fileName)
        {
            using var writer = new System.IO.StreamWriter(fileName);
            
            // Write header
            writer.WriteLine("Database,Table,Field,Value,DataType");

            // Write data
            foreach (var result in _searchResults)
            {
                var line = $"{EscapeCsvField(result.Database)}," +
                          $"{EscapeCsvField($"{result.Schema}.{result.Table}")}," +
                          $"{EscapeCsvField(result.Field)}," +
                          $"{EscapeCsvField(result.Value)}," +
                          $"{EscapeCsvField(result.DataType)}";
                writer.WriteLine(line);
            }
        }

        private string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        private void ResultsGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _searchResults.Count)
            {
                var result = _searchResults[e.RowIndex];
                SearchResultSelected?.Invoke(this, new SearchResultSelectedEventArgs(result));
            }
        }

        private async void ResultsGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _searchResults.Count && e.ColumnIndex == 3) // Value column
            {
                var result = _searchResults[e.RowIndex];
                var newValue = resultsGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
                
                // Don't update if value hasn't changed
                if (newValue == result.Value) return;
                
                // Show confirmation for direct edits in results grid
                var confirmResult = MessageBox.Show(
                    $"Update {result.Schema}.{result.Table}.{result.Field} to '{newValue}'?\n\nThis will modify the database record immediately.",
                    "Confirm Field Update",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (confirmResult == DialogResult.Yes)
                {
                    var success = await ExecuteRecordUpdate(result, result.Field, newValue);
                    if (success)
                    {
                        result.Value = newValue;
                        statusInfoLabel.Text = $"Updated {result.Schema}.{result.Table}.{result.Field}";
                        
                        // Refresh the record details if this record is selected
                        if (resultsGridView.SelectedRows.Count == 1 && resultsGridView.SelectedRows[0].Index == e.RowIndex)
                        {
                            await LoadCompleteRecord(result);
                        }
                    }
                    else
                    {
                        // Revert the cell value on failure
                        resultsGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = result.Value;
                    }
                }
                else
                {
                    // Revert the cell value if cancelled
                    resultsGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = result.Value;
                }
            }
        }

        private async void ResultsGridView_SelectionChanged(object sender, EventArgs e)
        {
            editButton.Enabled = resultsGridView.SelectedRows.Count > 0;
            deleteButton.Enabled = resultsGridView.SelectedRows.Count > 0;
            
            // Load complete record details for the selected result
            if (resultsGridView.SelectedRows.Count == 1)
            {
                var selectedRow = resultsGridView.SelectedRows[0];
                var resultIndex = selectedRow.Index;
                
                if (resultIndex < _searchResults.Count)
                {
                    var result = _searchResults[resultIndex];
                    await LoadCompleteRecord(result);
                }
            }
            else
            {
                ClearRecordDetails();
            }
        }


        private void ApplyModernTheme()
        {
            try
            {
                ThemeManager.ApplyThemeToDialog(this);
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error applying theme to Advanced Search dialog: {Message}", ex.Message);
            }
        }

        private async Task LoadCompleteRecord(SearchResult result)
        {
            try
            {
                recordInfoLabel.Text = $"Loading complete record from {result.Schema}.{result.Table}...";
                recordDetailsGridView.Rows.Clear();

                await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    // Switch to the correct database
                    using (var switchCmd = new SqlCommand($"USE [{result.Database}]", conn))
                    {
                        await switchCmd.ExecuteNonQueryAsync(ct);
                    }

                    // Get all columns for the table
                    var columnsQuery = @"
                        SELECT c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE, c.COLUMN_DEFAULT
                        FROM INFORMATION_SCHEMA.COLUMNS c
                        WHERE c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @Table
                        ORDER BY c.ORDINAL_POSITION";

                    var columns = new List<(string Name, string Type, bool IsNullable, string DefaultValue)>();
                    using (var colCmd = new SqlCommand(columnsQuery, conn))
                    {
                        colCmd.Parameters.AddWithValue("@Schema", result.Schema);
                        colCmd.Parameters.AddWithValue("@Table", result.Table);
                        
                        using var colReader = await colCmd.ExecuteReaderAsync(ct);
                        while (await colReader.ReadAsync(ct))
                        {
                            columns.Add((
                                colReader.GetString("COLUMN_NAME"),
                                colReader.GetString("DATA_TYPE"),
                                colReader.GetString("IS_NULLABLE") == "YES",
                                colReader.IsDBNull("COLUMN_DEFAULT") ? "" : colReader.GetString("COLUMN_DEFAULT")
                            ));
                        }
                    }

                    // Build query to find the specific record
                    // For now, we'll try to find records matching the search field and value
                    var recordQuery = $@"
                        SELECT TOP 1 *
                        FROM [{result.Schema}].[{result.Table}]
                        WHERE [{result.Field}] = @SearchValue";

                    using var recordCmd = new SqlCommand(recordQuery, conn);
                    recordCmd.Parameters.AddWithValue("@SearchValue", result.Value);
                    
                    using var recordReader = await recordCmd.ExecuteReaderAsync(ct);
                    if (await recordReader.ReadAsync(ct))
                    {
                        this.Invoke(() =>
                        {
                            recordDetailsGridView.Rows.Clear();
                            
                            for (int i = 0; i < recordReader.FieldCount; i++)
                            {
                                var fieldName = recordReader.GetName(i);
                                var fieldValue = recordReader.IsDBNull(i) ? "[NULL]" : recordReader.GetValue(i)?.ToString() ?? "";
                                var columnInfo = columns.FirstOrDefault(c => c.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                                var dataType = columnInfo.Name != null ? columnInfo.Type : "unknown";

                                var rowIndex = recordDetailsGridView.Rows.Add(
                                    fieldName,
                                    fieldValue,
                                    dataType
                                );

                                // Highlight the search field that was found
                                if (fieldName.Equals(result.Field, StringComparison.OrdinalIgnoreCase))
                                {
                                    recordDetailsGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
                                }
                            }
                            
                            recordInfoLabel.Text = $"Record details for {result.Schema}.{result.Table} (matched on {result.Field})";
                        });
                    }
                    else
                    {
                        this.Invoke(() =>
                        {
                            recordInfoLabel.Text = "No matching record found";
                            recordDetailsGridView.Rows.Clear();
                        });
                    }
                    
                    return Task.CompletedTask;
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                recordInfoLabel.Text = $"Error loading record: {ex.Message}";
                LoggingService.LogError(ex, "Error loading complete record details");
            }
        }

        private void ClearRecordDetails()
        {
            recordDetailsGridView.Rows.Clear();
            recordInfoLabel.Text = "Select a search result to view complete record details";
        }

        private async Task<bool> ExecuteRecordUpdate(SearchResult result, string fieldName, string newValue)
        {
            try
            {
                await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    // Switch to the correct database
                    using (var switchCmd = new SqlCommand($"USE [{result.Database}]", conn))
                    {
                        await switchCmd.ExecuteNonQueryAsync(ct);
                    }

                    // Build update query - using the search field as identifier
                    var updateQuery = $@"
                        UPDATE [{result.Schema}].[{result.Table}]
                        SET [{fieldName}] = @NewValue
                        WHERE [{result.Field}] = @SearchValue";

                    using var updateCmd = new SqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@NewValue", string.IsNullOrEmpty(newValue) ? DBNull.Value : newValue);
                    updateCmd.Parameters.AddWithValue("@SearchValue", result.Value);
                    
                    var rowsAffected = await updateCmd.ExecuteNonQueryAsync(ct);
                    
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException("No rows were updated. The record may have been deleted or modified.");
                    }
                    
                    return Task.CompletedTask;
                }, CancellationToken.None);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating record: {ex.Message}", "Update Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError(ex, "Error updating record");
                return false;
            }
        }

        private async Task<bool> ExecuteRecordDelete(SearchResult result)
        {
            try
            {
                await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    // Switch to the correct database
                    using (var switchCmd = new SqlCommand($"USE [{result.Database}]", conn))
                    {
                        await switchCmd.ExecuteNonQueryAsync(ct);
                    }

                    // Build delete query - using the search field as identifier
                    var deleteQuery = $@"
                        DELETE FROM [{result.Schema}].[{result.Table}]
                        WHERE [{result.Field}] = @SearchValue";

                    using var deleteCmd = new SqlCommand(deleteQuery, conn);
                    deleteCmd.Parameters.AddWithValue("@SearchValue", result.Value);
                    
                    var rowsAffected = await deleteCmd.ExecuteNonQueryAsync(ct);
                    
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException("No rows were deleted. The record may have already been deleted or modified.");
                    }
                    
                    return Task.CompletedTask;
                }, CancellationToken.None);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting record: {ex.Message}", "Delete Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError(ex, "Error deleting record");
                return false;
            }
        }

        private async void RecordDetailsGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1) // Value column in record details
            {
                var fieldName = recordDetailsGridView.Rows[e.RowIndex].Cells[0].Value?.ToString();
                var newValue = recordDetailsGridView.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";
                
                if (string.IsNullOrEmpty(fieldName)) return;
                
                // Get the current search result to update
                if (resultsGridView.SelectedRows.Count == 1)
                {
                    var resultIndex = resultsGridView.SelectedRows[0].Index;
                    if (resultIndex < _searchResults.Count)
                    {
                        var result = _searchResults[resultIndex];
                        
                        // Show confirmation for record details edits
                        var confirmResult = MessageBox.Show(
                            $"Update field '{fieldName}' to '{newValue}' in {result.Schema}.{result.Table}?\n\nThis will modify the database record immediately.",
                            "Confirm Field Update",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        
                        if (confirmResult == DialogResult.Yes)
                        {
                            var success = await ExecuteRecordUpdate(result, fieldName, newValue);
                            if (success)
                            {
                                statusInfoLabel.Text = $"Updated {result.Schema}.{result.Table}.{fieldName}";
                                
                                // Update the main results grid if we updated the search field
                                if (fieldName.Equals(result.Field, StringComparison.OrdinalIgnoreCase))
                                {
                                    result.Value = newValue;
                                    resultsGridView.Rows[resultIndex].Cells[3].Value = newValue; // Update Value column
                                }
                                
                                // Highlight the updated cell
                                recordDetailsGridView.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                                
                                // Reset highlight after a delay
                                var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                                timer.Tick += (s, args) =>
                                {
                                    if (e.RowIndex < recordDetailsGridView.Rows.Count)
                                    {
                                        var originalColor = fieldName.Equals(result.Field, StringComparison.OrdinalIgnoreCase) 
                                            ? Color.LightYellow 
                                            : Color.White;
                                        recordDetailsGridView.Rows[e.RowIndex].DefaultCellStyle.BackColor = originalColor;
                                    }
                                    timer.Stop();
                                    timer.Dispose();
                                };
                                timer.Start();
                            }
                            else
                            {
                                // Revert the cell value on failure - reload the complete record
                                await LoadCompleteRecord(result);
                            }
                        }
                        else
                        {
                            // Revert the cell value if cancelled - reload the complete record
                            await LoadCompleteRecord(result);
                        }
                    }
                }
            }
        }
        
        private async void RecordDetailsGridView_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle Ctrl+S to save all pending edits
            if (e.KeyCode == Keys.S && e.Control)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                
                MessageBox.Show(
                    "💡 Tip: Changes are automatically saved when you finish editing a cell.\n\nPress Enter or Tab to confirm your edit, or Escape to cancel.",
                    "Auto-Save Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            
            // Handle F5 to refresh record details
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                
                if (resultsGridView.SelectedRows.Count == 1)
                {
                    var resultIndex = resultsGridView.SelectedRows[0].Index;
                    if (resultIndex < _searchResults.Count)
                    {
                        var result = _searchResults[resultIndex];
                        await LoadCompleteRecord(result);
                        statusInfoLabel.Text = "Record details refreshed";
                    }
                }
            }
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            if (_pendingChanges.Count == 0)
            {
                MessageBox.Show("No pending changes to save.", "No Changes", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"Save {_pendingChanges.Count} pending changes to the database?\n\nThis operation will commit all changes at once.",
                "Confirm Save Changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes) return;

            saveButton.Enabled = false;
            progressBar.Visible = true;
            statusInfoLabel.Text = "Saving changes...";

            try
            {
                int successCount = 0;
                int errorCount = 0;
                var errorMessages = new List<string>();
                var processedChanges = new List<string>();

                foreach (var kvp in _pendingChanges)
                {
                    var changeKey = kvp.Key;
                    var pendingChange = kvp.Value;

                    try
                    {
                        var success = await ExecuteRecordUpdate(pendingChange.Result, pendingChange.FieldName, pendingChange.NewValue);
                        if (success)
                        {
                            successCount++;
                            processedChanges.Add(changeKey);
                            
                            // Update the search result with the new value
                            pendingChange.Result.Value = pendingChange.NewValue;
                            
                            // Update UI to remove pending visual indicators
                            UpdateVisualIndicators(changeKey, false);
                        }
                        else
                        {
                            errorCount++;
                            errorMessages.Add($"{pendingChange.Result.Schema}.{pendingChange.Result.Table}.{pendingChange.FieldName}: Update failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errorMessages.Add($"{pendingChange.Result.Schema}.{pendingChange.Result.Table}.{pendingChange.FieldName}: {ex.Message}");
                        LoggingService.LogError(ex, "Error saving pending change for {Schema}.{Table}.{Field}", 
                            pendingChange.Result.Schema, pendingChange.Result.Table, pendingChange.FieldName);
                    }
                }

                // Remove successfully processed changes
                foreach (var key in processedChanges)
                {
                    _pendingChanges.Remove(key);
                }

                // Update save button state
                UpdateSaveButtonState();

                // Show results
                var message = $"Save operation completed:\n• {successCount} changes saved successfully";
                if (errorCount > 0)
                {
                    message += $"\n• {errorCount} changes failed to save";
                    if (errorMessages.Any())
                    {
                        message += "\n\nErrors:\n" + string.Join("\n", errorMessages.Take(5));
                        if (errorMessages.Count > 5)
                        {
                            message += $"\n... and {errorMessages.Count - 5} more errors";
                        }
                    }
                }

                MessageBox.Show(message, "Save Results", 
                    MessageBoxButtons.OK, 
                    errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                statusInfoLabel.Text = $"Saved {successCount} changes. {_pendingChanges.Count} changes remaining";
                
                // Refresh the current record details if there's a selection
                if (resultsGridView.SelectedRows.Count == 1)
                {
                    var resultIndex = resultsGridView.SelectedRows[0].Index;
                    if (resultIndex < _searchResults.Count)
                    {
                        await LoadCompleteRecord(_searchResults[resultIndex]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during save operation: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusInfoLabel.Text = "Save operation failed";
            }
            finally
            {
                progressBar.Visible = false;
                UpdateSaveButtonState();
            }
        }

        private void UpdateSaveButtonState()
        {
            saveButton.Enabled = _pendingChanges.Count > 0;
            modifiedLabel.Text = _pendingChanges.Count > 0 ? $"*{_pendingChanges.Count} unsaved changes" : "";
        }

        private void UpdateVisualIndicators(string changeKey, bool hasPendingChanges)
        {
            // Update visual indicators in both grids
            // This would be called to add/remove asterisks or color coding
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchCancellation?.Cancel();
                _searchCancellation?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #region Supporting Classes

    public class SearchResult
    {
        public string Database { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string RowIdentifier { get; set; } = string.Empty;
    }

    public class SearchResultSelectedEventArgs : EventArgs
    {
        public SearchResult Result { get; }

        public SearchResultSelectedEventArgs(SearchResult result)
        {
            Result = result;
        }
    }

    public class SearchResultEditEventArgs : EventArgs
    {
        public SearchResult Result { get; }

        public SearchResultEditEventArgs(SearchResult result)
        {
            Result = result;
        }
    }

    public class PendingChange
    {
        public SearchResult Result { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string OriginalValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public DateTime ModifiedTime { get; set; }

        public PendingChange(SearchResult result, string fieldName, string originalValue, string newValue)
        {
            Result = result;
            FieldName = fieldName;
            OriginalValue = originalValue;
            NewValue = newValue;
            ModifiedTime = DateTime.Now;
        }
    }

    #endregion
}
