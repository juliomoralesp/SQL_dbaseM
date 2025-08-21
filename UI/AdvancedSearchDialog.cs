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
    /// Advanced search dialog with global database/table search, regex support, and history
    /// </summary>
    public partial class AdvancedSearchDialog : Form
    {
        private readonly SimpleConnectionService _connectionService;
        private readonly List<SearchResult> _searchResults = new List<SearchResult>();
        private readonly List<string> _searchHistory = new List<string>();
        private CancellationTokenSource _searchCancellation;

        // UI Controls
        private TextBox searchTextBox;
        private ComboBox searchTypeComboBox;
        private ComboBox searchScopeComboBox;
        private CheckBox regexCheckBox;
        private CheckBox caseSensitiveCheckBox;
        private CheckBox includeSystemObjectsCheckBox;
        private Button searchButton;
        private Button cancelButton;
        private DataGridView resultsGridView;
        private TreeView historyTreeView;
        private TabControl mainTabControl;
        private ProgressBar searchProgressBar;
        private Label statusLabel;
        private SplitContainer mainSplitContainer;

        public AdvancedSearchDialog(SimpleConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            InitializeComponent();
            LoadSearchHistory();
            ApplyModernTheme();
        }

        public event EventHandler<SearchResultSelectedEventArgs> SearchResultSelected;

        private void InitializeComponent()
        {
            this.Text = "Advanced Search";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);
            this.ShowIcon = false;
            this.MaximizeBox = true;

            CreateMainLayout();
            CreateSearchPanel();
            CreateResultsPanel();
            CreateHistoryPanel();
            SetupEventHandlers();
        }

        private void CreateMainLayout()
        {
            // Main container
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };

            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // Search panel
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Main content
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Status bar

            this.Controls.Add(mainContainer);

            // Search panel
            var searchPanel = CreateSearchPanel();
            mainContainer.Controls.Add(searchPanel, 0, 0);

            // Main split container for results and history
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400,
                Panel1MinSize = 200,
                Panel2MinSize = 100
            };

            // Results in top panel
            var resultsPanel = CreateResultsPanel();
            mainSplitContainer.Panel1.Controls.Add(resultsPanel);

            // History in bottom panel
            var historyPanel = CreateHistoryPanel();
            mainSplitContainer.Panel2.Controls.Add(historyPanel);

            mainContainer.Controls.Add(mainSplitContainer, 0, 1);

            // Status panel
            var statusPanel = new Panel { Dock = DockStyle.Fill };
            
            searchProgressBar = new ProgressBar
            {
                Dock = DockStyle.Left,
                Width = 200,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ready to search",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            statusPanel.Controls.Add(statusLabel);
            statusPanel.Controls.Add(searchProgressBar);
            mainContainer.Controls.Add(statusPanel, 0, 2);
        }

        private GroupBox CreateSearchPanel()
        {
            var panel = new GroupBox
            {
                Text = "Search Criteria",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3
            };

            // Search text
            var searchLabel = new Label { Text = "Search for:", Anchor = AnchorStyles.Left, AutoSize = true };
            searchTextBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };
            layout.Controls.Add(searchLabel, 0, 0);
            layout.Controls.Add(searchTextBox, 1, 0);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            // Search type
            var typeLabel = new Label { Text = "Search Type:", Anchor = AnchorStyles.Left, AutoSize = true };
            searchTypeComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            searchTypeComboBox.Items.AddRange(new[] {
                "Object Names", "Column Names", "Data Content", "Stored Procedure Code", "All"
            });
            searchTypeComboBox.SelectedIndex = 0;
            layout.Controls.Add(typeLabel, 2, 0);
            layout.Controls.Add(searchTypeComboBox, 3, 0);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            // Search scope
            var scopeLabel = new Label { Text = "Search Scope:", Anchor = AnchorStyles.Left, AutoSize = true };
            searchScopeComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            searchScopeComboBox.Items.AddRange(new[] {
                "Current Database", "All Databases", "Selected Databases"
            });
            searchScopeComboBox.SelectedIndex = 0;
            layout.Controls.Add(scopeLabel, 0, 1);
            layout.Controls.Add(searchScopeComboBox, 1, 1);

            // Options
            var optionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            regexCheckBox = new CheckBox { Text = "Regular Expression", AutoSize = true, Margin = new Padding(0, 0, 10, 0) };
            caseSensitiveCheckBox = new CheckBox { Text = "Case Sensitive", AutoSize = true, Margin = new Padding(0, 0, 10, 0) };
            includeSystemObjectsCheckBox = new CheckBox { Text = "Include System Objects", AutoSize = true };

            optionsPanel.Controls.AddRange(new Control[] { regexCheckBox, caseSensitiveCheckBox, includeSystemObjectsCheckBox });
            layout.Controls.Add(optionsPanel, 2, 1);
            layout.SetColumnSpan(optionsPanel, 2);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            searchButton = new Button
            {
                Text = "Search",
                Size = new Size(80, 30),
                Margin = new Padding(5, 0, 0, 0),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                Margin = new Padding(5, 0, 0, 0)
            };

            buttonPanel.Controls.AddRange(new Control[] { searchButton, cancelButton });
            layout.Controls.Add(buttonPanel, 0, 2);
            layout.SetColumnSpan(buttonPanel, 4);

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateResultsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var label = new Label
            {
                Text = "Search Results",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(5, 5, 0, 0)
            };

            resultsGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                BorderStyle = BorderStyle.None
            };

            // Configure columns
            resultsGridView.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Database", HeaderText = "Database", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "ObjectType", HeaderText = "Type", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Schema", HeaderText = "Schema", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "ObjectName", HeaderText = "Object Name", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "ColumnName", HeaderText = "Column", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "DataType", HeaderText = "Data Type", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "MatchContext", HeaderText = "Match Context", Width = 200, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            });

            panel.Controls.Add(resultsGridView);
            panel.Controls.Add(label);
            return panel;
        }

        private Panel CreateHistoryPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var label = new Label
            {
                Text = "Search History",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(5, 5, 0, 0)
            };

            historyTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ShowLines = true,
                ShowRootLines = false,
                FullRowSelect = true
            };

            panel.Controls.Add(historyTreeView);
            panel.Controls.Add(label);
            return panel;
        }

        private void SetupEventHandlers()
        {
            searchButton.Click += SearchButton_Click;
            cancelButton.Click += (s, e) => this.Close();
            searchTextBox.KeyDown += SearchTextBox_KeyDown;
            resultsGridView.CellDoubleClick += ResultsGridView_CellDoubleClick;
            historyTreeView.NodeMouseDoubleClick += HistoryTreeView_NodeMouseDoubleClick;
            regexCheckBox.CheckedChanged += (s, e) => ValidateSearchCriteria();
            searchTextBox.TextChanged += (s, e) => ValidateSearchCriteria();

            this.FormClosed += (s, e) => SaveSearchHistory();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && searchButton.Enabled)
            {
                e.Handled = true;
                PerformSearch();
            }
        }

        private void ResultsGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _searchResults.Count)
            {
                var result = _searchResults[e.RowIndex];
                SearchResultSelected?.Invoke(this, new SearchResultSelectedEventArgs(result));
            }
        }

        private void HistoryTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is SearchHistoryItem historyItem)
            {
                // Load search criteria from history
                searchTextBox.Text = historyItem.SearchText;
                searchTypeComboBox.SelectedIndex = Math.Max(0, searchTypeComboBox.Items.IndexOf(historyItem.SearchType));
                searchScopeComboBox.SelectedIndex = Math.Max(0, searchScopeComboBox.Items.IndexOf(historyItem.SearchScope));
                regexCheckBox.Checked = historyItem.IsRegex;
                caseSensitiveCheckBox.Checked = historyItem.IsCaseSensitive;
                includeSystemObjectsCheckBox.Checked = historyItem.IncludeSystemObjects;
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            PerformSearch();
        }

        private void ValidateSearchCriteria()
        {
            var isValid = !string.IsNullOrWhiteSpace(searchTextBox.Text);

            if (regexCheckBox.Checked)
            {
                try
                {
                    new Regex(searchTextBox.Text, caseSensitiveCheckBox.Checked ? RegexOptions.None : RegexOptions.IgnoreCase);
                }
                catch
                {
                    isValid = false;
                }
            }

            searchButton.Enabled = isValid && _connectionService.IsConnected;
            searchTextBox.BackColor = isValid ? SystemColors.Window : Color.LightPink;
        }

        private async void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text) || !_connectionService.IsConnected)
                return;

            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();

            var searchCriteria = new SearchCriteria
            {
                SearchText = searchTextBox.Text,
                SearchType = searchTypeComboBox.SelectedItem?.ToString() ?? "Object Names",
                SearchScope = searchScopeComboBox.SelectedItem?.ToString() ?? "Current Database",
                IsRegex = regexCheckBox.Checked,
                IsCaseSensitive = caseSensitiveCheckBox.Checked,
                IncludeSystemObjects = includeSystemObjectsCheckBox.Checked
            };

            try
            {
                searchButton.Enabled = false;
                searchProgressBar.Visible = true;
                statusLabel.Text = "Searching...";
                _searchResults.Clear();
                resultsGridView.Rows.Clear();

                var results = await PerformDatabaseSearch(searchCriteria, _searchCancellation.Token);
                
                _searchResults.AddRange(results);
                DisplaySearchResults(results);
                AddToSearchHistory(searchCriteria, results.Count);

                statusLabel.Text = $"Found {results.Count} results";
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Search cancelled";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Search failed: {ex.Message}";
                LoggingService.LogError(ex, "Error performing advanced search");
            }
            finally
            {
                searchButton.Enabled = true;
                searchProgressBar.Visible = false;
            }
        }

        private async Task<List<SearchResult>> PerformDatabaseSearch(SearchCriteria criteria, CancellationToken cancellationToken)
        {
            var results = new List<SearchResult>();

            await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
            {
                var databases = await GetDatabasesForSearch(conn, criteria, ct);

                foreach (var database in databases)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    try
                    {
                        var dbResults = await SearchInDatabase(conn, database, criteria, ct);
                        results.AddRange(dbResults);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogWarning("Error searching database {Database}: {Message}", database, ex.Message);
                    }
                }

                return Task.CompletedTask;
            }, cancellationToken);

            return results;
        }

        private async Task<List<string>> GetDatabasesForSearch(SqlConnection connection, SearchCriteria criteria, CancellationToken cancellationToken)
        {
            var databases = new List<string>();

            switch (criteria.SearchScope)
            {
                case "Current Database":
                    databases.Add(_connectionService.CurrentDatabase ?? "master");
                    break;

                case "All Databases":
                    using (var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE state = 0 ORDER BY name", connection))
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            databases.Add(reader.GetString(0));
                        }
                    }
                    break;

                default: // Selected Databases - for now, just use current
                    databases.Add(_connectionService.CurrentDatabase ?? "master");
                    break;
            }

            return databases;
        }

        private async Task<List<SearchResult>> SearchInDatabase(SqlConnection connection, string database, SearchCriteria criteria, CancellationToken cancellationToken)
        {
            var results = new List<SearchResult>();

            // Switch to the target database
            using (var cmd = new SqlCommand($"USE [{database}]", connection))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Search based on type
            switch (criteria.SearchType)
            {
                case "Object Names":
                    results.AddRange(await SearchObjectNames(connection, database, criteria, cancellationToken));
                    break;
                case "Column Names":
                    results.AddRange(await SearchColumnNames(connection, database, criteria, cancellationToken));
                    break;
                case "Data Content":
                    results.AddRange(await SearchDataContent(connection, database, criteria, cancellationToken));
                    break;
                case "Stored Procedure Code":
                    results.AddRange(await SearchStoredProcedureCode(connection, database, criteria, cancellationToken));
                    break;
                case "All":
                    results.AddRange(await SearchObjectNames(connection, database, criteria, cancellationToken));
                    results.AddRange(await SearchColumnNames(connection, database, criteria, cancellationToken));
                    results.AddRange(await SearchStoredProcedureCode(connection, database, criteria, cancellationToken));
                    break;
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchObjectNames(SqlConnection connection, string database, SearchCriteria criteria, CancellationToken cancellationToken)
        {
            var results = new List<SearchResult>();
            var systemFilter = criteria.IncludeSystemObjects ? "" : "AND s.name NOT IN ('sys', 'information_schema')";
            
            var sql = $@"
                SELECT s.name AS SchemaName, o.name AS ObjectName, o.type_desc AS ObjectType
                FROM sys.objects o
                INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                WHERE o.is_ms_shipped = 0 {systemFilter}
                ORDER BY s.name, o.name";

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var schema = reader.GetString("SchemaName");
                var objectName = reader.GetString("ObjectName");
                var objectType = reader.GetString("ObjectType");

                if (MatchesSearchCriteria(objectName, criteria))
                {
                    results.Add(new SearchResult
                    {
                        Database = database,
                        Schema = schema,
                        ObjectName = objectName,
                        ObjectType = objectType,
                        MatchContext = $"Object name matches '{criteria.SearchText}'"
                    });
                }
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchColumnNames(SqlConnection connection, string database, SearchCriteria criteria, CancellationToken cancellationToken)
        {
            var results = new List<SearchResult>();
            var systemFilter = criteria.IncludeSystemObjects ? "" : "AND s.name NOT IN ('sys', 'information_schema')";
            
            var sql = $@"
                SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName, 
                       typ.name AS DataType, c.max_length, c.precision, c.scale
                FROM sys.columns c
                INNER JOIN sys.tables t ON c.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.types typ ON c.user_type_id = typ.user_type_id
                WHERE t.is_ms_shipped = 0 {systemFilter}
                ORDER BY s.name, t.name, c.column_id";

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var schema = reader.GetString("SchemaName");
                var tableName = reader.GetString("TableName");
                var columnName = reader.GetString("ColumnName");
                var dataType = reader.GetString("DataType");

                if (MatchesSearchCriteria(columnName, criteria))
                {
                    results.Add(new SearchResult
                    {
                        Database = database,
                        Schema = schema,
                        ObjectName = tableName,
                        ObjectType = "TABLE",
                        ColumnName = columnName,
                        DataType = dataType,
                        MatchContext = $"Column name matches '{criteria.SearchText}'"
                    });
                }
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchStoredProcedureCode(SqlConnection connection, string database, SearchCriteria criteria, CancellationToken cancellationToken)
        {
            var results = new List<SearchResult>();
            var systemFilter = criteria.IncludeSystemObjects ? "" : "AND s.name NOT IN ('sys', 'information_schema')";
            
            var sql = $@"
                SELECT s.name AS SchemaName, o.name AS ObjectName, o.type_desc AS ObjectType, m.definition
                FROM sys.objects o
                INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                INNER JOIN sys.sql_modules m ON o.object_id = m.object_id
                WHERE o.type IN ('P', 'FN', 'TF', 'IF') AND o.is_ms_shipped = 0 {systemFilter}
                ORDER BY s.name, o.name";

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var schema = reader.GetString("SchemaName");
                var objectName = reader.GetString("ObjectName");
                var objectType = reader.GetString("ObjectType");
                var definition = reader.GetString("definition");

                if (MatchesSearchCriteria(definition, criteria))
                {
                    // Find the specific line that matches
                    var lines = definition.Split('\n');
                    var matchingLine = lines.FirstOrDefault(line => MatchesSearchCriteria(line.Trim(), criteria));
                    var context = matchingLine?.Trim() ?? "Match found in code";
                    
                    if (context.Length > 100)
                        context = context.Substring(0, 100) + "...";

                    results.Add(new SearchResult
                    {
                        Database = database,
                        Schema = schema,
                        ObjectName = objectName,
                        ObjectType = objectType,
                        MatchContext = context
                    });
                }
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchDataContent(SqlConnection connection, string database, SearchCriteria criteria, CancellationToken cancellationToken)
        {
            var results = new List<SearchResult>();
            
            // This is a basic implementation - searching string columns only
            var systemFilter = criteria.IncludeSystemObjects ? "" : "AND s.name NOT IN ('sys', 'information_schema')";
            
            var sql = $@"
                SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
                FROM sys.columns c
                INNER JOIN sys.tables t ON c.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.types typ ON c.user_type_id = typ.user_type_id
                WHERE t.is_ms_shipped = 0 {systemFilter}
                AND typ.name IN ('varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext')
                ORDER BY s.name, t.name, c.column_id";

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var searchableTables = new List<(string Schema, string Table, string Column)>();
            while (await reader.ReadAsync(cancellationToken))
            {
                searchableTables.Add((
                    reader.GetString("SchemaName"),
                    reader.GetString("TableName"),
                    reader.GetString("ColumnName")
                ));
            }

            // Search in each table/column combination (limit to prevent excessive queries)
            var maxTables = Math.Min(searchableTables.Count, 50);
            for (int i = 0; i < maxTables; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var (schema, table, column) = searchableTables[i];
                
                try
                {
                    var searchSql = criteria.IsRegex 
                        ? $"SELECT TOP 10 [{column}] FROM [{schema}].[{table}] WHERE [{column}] IS NOT NULL"
                        : $"SELECT TOP 10 [{column}] FROM [{schema}].[{table}] WHERE [{column}] LIKE '%{criteria.SearchText.Replace("'", "''")}%'";

                    using var searchCmd = new SqlCommand(searchSql, connection);
                    searchCmd.CommandTimeout = 30;
                    
                    using var dataReader = await searchCmd.ExecuteReaderAsync(cancellationToken);
                    while (await dataReader.ReadAsync(cancellationToken))
                    {
                        var value = dataReader.GetValue(0)?.ToString() ?? "";
                        
                        if (criteria.IsRegex)
                        {
                            if (MatchesSearchCriteria(value, criteria))
                            {
                                results.Add(new SearchResult
                                {
                                    Database = database,
                                    Schema = schema,
                                    ObjectName = table,
                                    ObjectType = "TABLE",
                                    ColumnName = column,
                                    MatchContext = value.Length > 100 ? value.Substring(0, 100) + "..." : value
                                });
                            }
                        }
                        else
                        {
                            results.Add(new SearchResult
                            {
                                Database = database,
                                Schema = schema,
                                ObjectName = table,
                                ObjectType = "TABLE",
                                ColumnName = column,
                                MatchContext = value.Length > 100 ? value.Substring(0, 100) + "..." : value
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning("Error searching data in {Schema}.{Table}.{Column}: {Message}", 
                        schema, table, column, ex.Message);
                }
            }

            return results;
        }

        private bool MatchesSearchCriteria(string text, SearchCriteria criteria)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                if (criteria.IsRegex)
                {
                    var options = criteria.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    return Regex.IsMatch(text, criteria.SearchText, options);
                }
                else
                {
                    var comparison = criteria.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    return text.IndexOf(criteria.SearchText, comparison) >= 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void DisplaySearchResults(List<SearchResult> results)
        {
            resultsGridView.Rows.Clear();
            
            foreach (var result in results)
            {
                resultsGridView.Rows.Add(
                    result.Database,
                    result.ObjectType,
                    result.Schema,
                    result.ObjectName,
                    result.ColumnName ?? "",
                    result.DataType ?? "",
                    result.MatchContext
                );
            }
        }

        private void AddToSearchHistory(SearchCriteria criteria, int resultCount)
        {
            var historyItem = new SearchHistoryItem
            {
                SearchText = criteria.SearchText,
                SearchType = criteria.SearchType,
                SearchScope = criteria.SearchScope,
                IsRegex = criteria.IsRegex,
                IsCaseSensitive = criteria.IsCaseSensitive,
                IncludeSystemObjects = criteria.IncludeSystemObjects,
                Timestamp = DateTime.Now,
                ResultCount = resultCount
            };

            // Remove duplicate if exists
            _searchHistory.RemoveAll(h => h == criteria.SearchText);
            _searchHistory.Insert(0, criteria.SearchText);
            
            // Keep only recent 50 searches
            if (_searchHistory.Count > 50)
                _searchHistory.RemoveRange(50, _searchHistory.Count - 50);

            RefreshHistoryTree(historyItem);
        }

        private void RefreshHistoryTree(SearchHistoryItem latestItem = null)
        {
            historyTreeView.Nodes.Clear();

            var recentNode = new TreeNode("Recent Searches");
            var todayNode = new TreeNode("Today");
            var weekNode = new TreeNode("This Week");
            var olderNode = new TreeNode("Older");

            var now = DateTime.Now;
            var todayStart = now.Date;
            var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);

            if (latestItem != null)
            {
                var node = new TreeNode($"{latestItem.SearchText} ({latestItem.ResultCount} results)")
                {
                    Tag = latestItem
                };
                
                if (latestItem.Timestamp >= todayStart)
                    todayNode.Nodes.Add(node);
                else if (latestItem.Timestamp >= weekStart)
                    weekNode.Nodes.Add(node);
                else
                    olderNode.Nodes.Add(node);
            }

            // Add nodes with content
            if (todayNode.Nodes.Count > 0)
                recentNode.Nodes.Add(todayNode);
            if (weekNode.Nodes.Count > 0)
                recentNode.Nodes.Add(weekNode);
            if (olderNode.Nodes.Count > 0)
                recentNode.Nodes.Add(olderNode);

            historyTreeView.Nodes.Add(recentNode);
            recentNode.Expand();
        }

        private void LoadSearchHistory()
        {
            try
            {
                // Load from user settings if implemented
                RefreshHistoryTree();
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error loading search history: {Message}", ex.Message);
            }
        }

        private void SaveSearchHistory()
        {
            try
            {
                // Save to user settings if implemented
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error saving search history: {Message}", ex.Message);
            }
        }

        private void ApplyModernTheme()
        {
            // Apply the current theme using the existing ThemeManager
            try
            {
                ThemeManager.ApplyThemeToDialog(this);
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Error applying theme to Advanced Search dialog: {Message}", ex.Message);
            }
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

    public class SearchCriteria
    {
        public string SearchText { get; set; } = string.Empty;
        public string SearchType { get; set; } = "Object Names";
        public string SearchScope { get; set; } = "Current Database";
        public bool IsRegex { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool IncludeSystemObjects { get; set; }
    }

    public class SearchResult
    {
        public string Database { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string ObjectName { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty;
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string MatchContext { get; set; } = string.Empty;
    }

    public class SearchHistoryItem
    {
        public string SearchText { get; set; } = string.Empty;
        public string SearchType { get; set; } = string.Empty;
        public string SearchScope { get; set; } = string.Empty;
        public bool IsRegex { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool IncludeSystemObjects { get; set; }
        public DateTime Timestamp { get; set; }
        public int ResultCount { get; set; }
    }

    public class SearchResultSelectedEventArgs : EventArgs
    {
        public SearchResult Result { get; }

        public SearchResultSelectedEventArgs(SearchResult result)
        {
            Result = result;
        }
    }

    #endregion
}
