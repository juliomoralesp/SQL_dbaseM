using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ScintillaNET;
using SqlServerManager.Services;
using SqlServerManager.Core.Controls;

namespace SqlServerManager.Core.QueryEngine
{
    /// <summary>
    /// Advanced SQL editor with syntax highlighting, IntelliSense, and query execution
    /// </summary>
    public partial class AdvancedSqlEditor : UserControl
    {
        private SimpleTextEditor sqlEditor;
        
        // Results UI components
        private TabControl resultsTabControl;
        private DataGridView resultsGrid;
        private TextBox messagesTextBox;
        private TreeView executionPlanTree;
        private Panel performancePanel;
        private Label performanceLabel;
        private ProgressBar executionProgress;
        private Timer intelliSenseTimer;
        private Label statusLabel;
        
        // Data fields
        private string connectionString;
        private List<string> databaseObjects;
        private Dictionary<string, List<string>> tableColumns;
        
        private readonly ConnectionService _connectionService;
        private readonly SqlIntelliSense _intelliSense;
        
        // SQL Keywords for highlighting
        private readonly string[] SQL_KEYWORDS = {
            "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER",
            "TABLE", "DATABASE", "INDEX", "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER",
            "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS", "NULL",
            "ORDER", "BY", "GROUP", "HAVING", "DISTINCT", "TOP", "LIMIT",
            "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "JOIN", "ON", "CROSS",
            "UNION", "INTERSECT", "EXCEPT", "ALL", "AS", "INTO", "VALUES",
            "BEGIN", "END", "IF", "ELSE", "WHILE", "FOR", "CASE", "WHEN", "THEN"
        };

        public AdvancedSqlEditor(ConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _intelliSense = new SqlIntelliSense(connectionService);
            
            InitializeComponent();
            SetupSqlEditor();
            SetupEventHandlers();
            
            // Subscribe to connection changes
            _connectionService.ConnectionChanged += OnConnectionChanged;
        }

        public event EventHandler<QueryExecutedEventArgs> QueryExecuted;
        public event EventHandler<string> StatusChanged;
        
        private void SetupEventHandlers()
        {
            // Setup IntelliSense timer
            intelliSenseTimer = new Timer { Interval = 500 };
            intelliSenseTimer.Tick += IntelliSenseTimer_Tick;
            
            // Additional event handler setup can be added here
        }
        
        private void OnConnectionChanged(object sender, ConnectionEventArgs e)
        {
            connectionString = _connectionService.IsConnected ? "placeholder" : null; // We'll get actual connection string from service
            if (!string.IsNullOrEmpty(connectionString))
            {
                LoadDatabaseSchema();
            }
            StatusChanged?.Invoke(this, e?.IsConnected == true ? "Connected" : "Disconnected");
        }
        
        private void InitializeComponent()
        {
            this.Size = new Size(1000, 600);
            this.Dock = DockStyle.Fill;
            
            // Create main split container
            var mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };
            
            // SQL Editor setup
            sqlEditor = new SimpleTextEditor
            {
                Dock = DockStyle.Fill
            };
            
            SetupSqlEditor();
            
            // Toolbar
            var toolbar = new ToolStrip();
            var executeButton = new ToolStripButton("Execute", null, ExecuteQuery_Click);
            executeButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            var formatButton = new ToolStripButton("Format SQL", null, FormatSql_Click);
            formatButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            var saveButton = new ToolStripButton("Save Query", null, SaveQuery_Click);
            saveButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            executionProgress = new ProgressBar
            {
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };
            
            var progressHost = new ToolStripControlHost(executionProgress);
            
            toolbar.Items.AddRange(new ToolStripItem[] {
                executeButton,
                new ToolStripSeparator(),
                formatButton,
                saveButton,
                new ToolStripSeparator(),
                progressHost
            });
            
            // Editor panel
            var editorPanel = new Panel { Dock = DockStyle.Fill };
            editorPanel.Controls.Add(sqlEditor);
            
            mainSplitter.Panel1.Controls.Add(editorPanel);
            mainSplitter.Panel1.Controls.Add(toolbar);
            
            // Results area
            CreateResultsArea(mainSplitter.Panel2);
            
            this.Controls.Add(mainSplitter);
        }
        
        private void SetupSqlEditor()
        {
            // Basic setup for our SimpleTextEditor
            sqlEditor.LexerName = "sql";
            sqlEditor.ApplySqlStyling();
            sqlEditor.BackColor = Color.FromArgb(30, 30, 30);
            sqlEditor.ForeColor = Color.White;
            
            // Event handlers
            sqlEditor.KeyDown += SqlEditor_KeyDown;
        }
        
        private void CreateResultsArea(Control parent)
        {
            resultsTabControl = new TabControl { Dock = DockStyle.Fill };
            
            // Results tab
            var resultsTab = new TabPage("Results");
            resultsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            resultsTab.Controls.Add(resultsGrid);
            
            // Messages tab
            var messagesTab = new TabPage("Messages");
            messagesTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White
            };
            messagesTab.Controls.Add(messagesTextBox);
            
            // Execution Plan tab
            var planTab = new TabPage("Execution Plan");
            executionPlanTree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White
            };
            planTab.Controls.Add(executionPlanTree);
            
            // Performance tab
            var performanceTab = new TabPage("Performance");
            performancePanel = new Panel { Dock = DockStyle.Fill };
            performanceLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            performancePanel.Controls.Add(performanceLabel);
            performanceTab.Controls.Add(performancePanel);
            
            resultsTabControl.TabPages.AddRange(new TabPage[] {
                resultsTab, messagesTab, planTab, performanceTab
            });
            
            // Status bar
            statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                Text = "Ready",
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            parent.Controls.Add(resultsTabControl);
            parent.Controls.Add(statusLabel);
        }
        
        private async void LoadDatabaseSchema()
        {
            if (string.IsNullOrEmpty(connectionString)) return;
            
            try
            {
                databaseObjects = new List<string>();
                tableColumns = new Dictionary<string, List<string>>();
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Load tables
                    var tablesQuery = @"
                        SELECT TABLE_NAME 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_TYPE = 'BASE TABLE'
                        ORDER BY TABLE_NAME";
                    
                    using (var cmd = new SqlCommand(tablesQuery, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tableName = reader.GetString(0);
                            databaseObjects.Add(tableName);
                        }
                    }
                    
                    // Load columns for each table
                    foreach (var table in databaseObjects.ToList())
                    {
                        var columnsQuery = @"
                            SELECT COLUMN_NAME 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_NAME = @TableName
                            ORDER BY ORDINAL_POSITION";
                        
                        using (var cmd = new SqlCommand(columnsQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@TableName", table);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                var columns = new List<string>();
                                while (await reader.ReadAsync())
                                {
                                    columns.Add(reader.GetString(0));
                                }
                                tableColumns[table] = columns;
                            }
                        }
                    }
                }
                
                messagesTextBox.AppendText($"Loaded schema: {databaseObjects.Count} tables\r\n");
            }
            catch (Exception ex)
            {
                messagesTextBox.AppendText($"Error loading schema: {ex.Message}\r\n");
            }
        }
        
        // Simplified event handlers for our basic text editor
        // IntelliSense and brace matching features are temporarily disabled
        
        private void SqlEditor_KeyDown(object sender, KeyEventArgs e)
        {
            // Execute query on Ctrl+Enter or F5
            if ((e.Control && e.KeyCode == Keys.Enter) || e.KeyCode == Keys.F5)
            {
                ExecuteQuery_Click(null, null);
                e.Handled = true;
            }
            // Show IntelliSense on Ctrl+Space
            else if (e.Control && e.KeyCode == Keys.Space)
            {
                ShowIntelliSense();
                e.Handled = true;
            }
        }
        
        private void IntelliSenseTimer_Tick(object sender, EventArgs e)
        {
            intelliSenseTimer.Stop();
            ShowIntelliSense();
        }
        
        private void ShowIntelliSense()
        {
            // IntelliSense is temporarily disabled for the SimpleTextEditor
            // This feature would require a more sophisticated text editor control
        }
        
        private async void ExecuteQuery_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("No connection established.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var sql = sqlEditor.SelectedText;
            if (string.IsNullOrWhiteSpace(sql))
                sql = sqlEditor.Text;
                
            if (string.IsNullOrWhiteSpace(sql))
                return;
            
            await ExecuteQuery(sql);
        }
        
        public async Task ExecuteQuery(string sql)
        {
            var stopwatch = Stopwatch.StartNew();
            executionProgress.Style = ProgressBarStyle.Marquee;
            executionProgress.Visible = true;
            statusLabel.Text = "Executing query...";
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.CommandTimeout = 30; // 30 seconds timeout
                        
                        if (IsSelectQuery(sql))
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                var dataTable = new DataTable();
                                await Task.Run(() => adapter.Fill(dataTable));
                                
                                resultsGrid.DataSource = dataTable;
                                resultsTabControl.SelectedIndex = 0; // Show Results tab
                                
                                stopwatch.Stop();
                                var message = $"Query executed successfully. {dataTable.Rows.Count:N0} rows returned in {stopwatch.ElapsedMilliseconds:N0}ms";
                                messagesTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
                                statusLabel.Text = message;
                                
                                performanceLabel.Text = $"Execution Time: {stopwatch.ElapsedMilliseconds}ms | Rows: {dataTable.Rows.Count:N0} | Memory: {GC.GetTotalMemory(false) / 1024 / 1024:N0}MB";
                            }
                        }
                        else
                        {
                            var rowsAffected = await command.ExecuteNonQueryAsync();
                            stopwatch.Stop();
                            
                            var message = $"Query executed successfully. {rowsAffected} rows affected in {stopwatch.ElapsedMilliseconds:N0}ms";
                            messagesTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
                            statusLabel.Text = message;
                            resultsTabControl.SelectedIndex = 1; // Show Messages tab
                        }
                        
                        QueryExecuted?.Invoke(this, new QueryExecutedEventArgs(sql, stopwatch.Elapsed));
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                messagesTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - Error: {ex.Message}\r\n");
                statusLabel.Text = "Query execution failed";
                resultsTabControl.SelectedIndex = 1; // Show Messages tab
            }
            finally
            {
                executionProgress.Visible = false;
                executionProgress.Style = ProgressBarStyle.Continuous;
            }
        }
        
        private bool IsSelectQuery(string sql)
        {
            return sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
        }
        
        private void FormatSql_Click(object sender, EventArgs e)
        {
            // Basic SQL formatting
            var sql = sqlEditor.Text;
            if (string.IsNullOrWhiteSpace(sql)) return;
            
            var formatted = FormatSqlText(sql);
            sqlEditor.Text = formatted;
        }
        
        private string FormatSqlText(string sql)
        {
            // Simple SQL formatter
            var keywords = new[] { "SELECT", "FROM", "WHERE", "ORDER BY", "GROUP BY", "HAVING", "INSERT", "UPDATE", "DELETE" };
            
            foreach (var keyword in keywords)
            {
                sql = System.Text.RegularExpressions.Regex.Replace(sql, 
                    $@"\b{keyword}\b", $"\r\n{keyword}", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            
            return sql.Trim();
        }
        
        private void SaveQuery_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*";
                saveDialog.DefaultExt = "sql";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, sqlEditor.Text);
                    messagesTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - Query saved to {saveDialog.FileName}\r\n");
                }
            }
        }
        
        public void SetConnectionString(string connectionString)
        {
            this.connectionString = connectionString;
            LoadDatabaseSchema();
        }
        
        public string GetQueryText()
        {
            return sqlEditor.Text;
        }
        
        public void SetQueryText(string text)
        {
            sqlEditor.Text = text;
        }
    }
    
    public class QueryExecutedEventArgs : EventArgs
    {
        public string Query { get; }
        public TimeSpan ExecutionTime { get; }
        
        public QueryExecutedEventArgs(string query, TimeSpan executionTime)
        {
            Query = query;
            ExecutionTime = executionTime;
        }
    }
}
