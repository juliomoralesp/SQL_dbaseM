using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace SqlServerManager
{
    /// <summary>
    /// Advanced SQL Editor with syntax highlighting, auto-complete, and query execution
    /// </summary>
    public partial class SqlEditorControl : UserControl
    {
        private RichTextBox sqlTextBox;
        private DataGridView resultsDataGridView;
        private TabControl tabControl;
        private TabPage resultsTab;
        private TabPage messagesTab;
        private TextBox messagesTextBox;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripLabel statusLabel;
        private ToolStripProgressBar progressBar;
        
        private SqlConnection currentConnection;
        private string currentDatabase;
        
        // SQL syntax highlighting
        private readonly Dictionary<string, Color> sqlKeywords;
        private readonly Dictionary<string, Color> sqlFunctions;
        
        // Query history
        private readonly List<QueryHistoryItem> queryHistory;
        
        public event EventHandler<QueryExecutedEventArgs> QueryExecuted;
        
        public string SqlText
        {
            get => sqlTextBox.Text;
            set => sqlTextBox.Text = value;
        }
        
        public SqlConnection Connection
        {
            get => currentConnection;
            set
            {
                currentConnection = value;
                UpdateConnectionStatus();
            }
        }
        
        public string Database
        {
            get => currentDatabase;
            set
            {
                currentDatabase = value;
                UpdateConnectionStatus();
            }
        }
        
        public SqlEditorControl()
        {
            sqlKeywords = InitializeSqlKeywords();
            sqlFunctions = InitializeSqlFunctions();
            queryHistory = new List<QueryHistoryItem>();
            
            InitializeComponent();
            SetupSyntaxHighlighting();
            
            LoggingService.LogDebug("SQL Editor control initialized");
        }
        
        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.Dock = DockStyle.Fill;
            
            // Main container
            var splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Horizontal;
            splitContainer.SplitterDistance = 250;
            
            // Top panel - SQL Editor
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;
            
            // Toolbar
            toolStrip = new ToolStrip();
            toolStrip.Items.Add(new ToolStripButton("Execute (F5)", null, ExecuteQuery_Click) { Name = "Execute" });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("New Query", null, NewQuery_Click) { Name = "NewQuery" });
            toolStrip.Items.Add(new ToolStripButton("Open", null, OpenQuery_Click) { Name = "Open" });
            toolStrip.Items.Add(new ToolStripButton("Save", null, SaveQuery_Click) { Name = "Save" });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Comment", null, CommentLines_Click) { Name = "Comment" });
            toolStrip.Items.Add(new ToolStripButton("Uncomment", null, UncommentLines_Click) { Name = "Uncomment" });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Format SQL", null, FormatSql_Click) { Name = "Format" });
            
            // SQL Text Editor
            sqlTextBox = new RichTextBox();
            sqlTextBox.Dock = DockStyle.Fill;
            sqlTextBox.Font = new Font("Consolas", 10);
            sqlTextBox.AcceptsTab = true;
            sqlTextBox.WordWrap = false;
            sqlTextBox.ScrollBars = RichTextBoxScrollBars.Both;
            sqlTextBox.TextChanged += SqlTextBox_TextChanged;
            sqlTextBox.KeyDown += SqlTextBox_KeyDown;
            
            topPanel.Controls.Add(sqlTextBox);
            topPanel.Controls.Add(toolStrip);
            
            // Bottom panel - Results and Messages
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            
            // Results tab
            resultsTab = new TabPage("Results");
            resultsDataGridView = new DataGridView();
            resultsDataGridView.Dock = DockStyle.Fill;
            resultsDataGridView.ReadOnly = true;
            resultsDataGridView.AllowUserToAddRows = false;
            resultsDataGridView.AllowUserToDeleteRows = false;
            resultsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            resultsTab.Controls.Add(resultsDataGridView);
            
            // Messages tab
            messagesTab = new TabPage("Messages");
            messagesTextBox = new TextBox();
            messagesTextBox.Dock = DockStyle.Fill;
            messagesTextBox.Multiline = true;
            messagesTextBox.ScrollBars = ScrollBars.Both;
            messagesTextBox.ReadOnly = true;
            messagesTextBox.Font = new Font("Consolas", 9);
            messagesTab.Controls.Add(messagesTextBox);
            
            tabControl.TabPages.Add(resultsTab);
            tabControl.TabPages.Add(messagesTab);
            
            splitContainer.Panel1.Controls.Add(topPanel);
            splitContainer.Panel2.Controls.Add(tabControl);
            
            // Status bar
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripLabel("Ready");
            progressBar = new ToolStripProgressBar();
            progressBar.Visible = false;
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(progressBar);
            
            // Add controls to main container
            this.Controls.Add(splitContainer);
            this.Controls.Add(statusStrip);
            
            // Apply theme to child controls
            try
            {
                ThemeManager.ApplyTheme(this.FindForm() ?? this.ParentForm ?? new Form() { Controls = { this } }, ThemeManager.CurrentTheme);
            }
            catch
            {
                // If theme application fails, continue without it
            }
        }
        
        private Dictionary<string, Color> InitializeSqlKeywords()
        {
            var keywords = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            var keywordColor = Color.Blue;
            
            string[] sqlKeywordList = {
                "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP",
                "TABLE", "DATABASE", "INDEX", "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER",
                "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS", "NULL",
                "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "ON", "UNION", "ALL",
                "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC", "DISTINCT", "TOP",
                "CASE", "WHEN", "THEN", "ELSE", "END", "IF", "BEGIN", "END", "TRY", "CATCH",
                "DECLARE", "SET", "EXEC", "EXECUTE", "RETURN", "WHILE", "FOR", "CURSOR",
                "INT", "VARCHAR", "NVARCHAR", "CHAR", "NCHAR", "TEXT", "NTEXT",
                "DATETIME", "DATE", "TIME", "TIMESTAMP", "DECIMAL", "NUMERIC", "FLOAT", "REAL",
                "BIT", "BINARY", "VARBINARY", "IMAGE", "UNIQUEIDENTIFIER", "XML"
            };
            
            foreach (var keyword in sqlKeywordList)
            {
                keywords[keyword] = keywordColor;
            }
            
            return keywords;
        }
        
        private Dictionary<string, Color> InitializeSqlFunctions()
        {
            var functions = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            var functionColor = Color.DarkMagenta;
            
            string[] functionList = {
                "COUNT", "SUM", "AVG", "MIN", "MAX", "LEN", "SUBSTRING", "REPLACE", "UPPER", "LOWER",
                "LTRIM", "RTRIM", "CHARINDEX", "PATINDEX", "STUFF", "REVERSE", "REPLICATE",
                "GETDATE", "DATEADD", "DATEDIFF", "DATEPART", "YEAR", "MONTH", "DAY",
                "CAST", "CONVERT", "ISNULL", "COALESCE", "NULLIF", "IIF",
                "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE", "LAG", "LEAD"
            };
            
            foreach (var function in functionList)
            {
                functions[function] = functionColor;
            }
            
            return functions;
        }
        
        private void SetupSyntaxHighlighting()
        {
            // Initial highlighting will be done on text change
        }
        
        private void HighlightSyntax()
        {
            if (sqlTextBox == null || string.IsNullOrEmpty(sqlTextBox.Text)) return;
            
            try
            {
                var originalSelection = sqlTextBox.SelectionStart;
                var originalLength = sqlTextBox.SelectionLength;
                
                sqlTextBox.SelectAll();
                sqlTextBox.SelectionColor = Color.Black;
                
                // Highlight keywords
                foreach (var keyword in sqlKeywords)
                {
                    HighlightPattern($@"\b{Regex.Escape(keyword.Key)}\b", keyword.Value, RegexOptions.IgnoreCase);
                }
                
                // Highlight functions
                foreach (var function in sqlFunctions)
                {
                    HighlightPattern($@"\b{Regex.Escape(function.Key)}\s*\(", function.Value, RegexOptions.IgnoreCase);
                }
                
                // Highlight strings
                HighlightPattern(@"'[^']*'", Color.Red, RegexOptions.None);
                
                // Highlight comments
                HighlightPattern(@"--.*$", Color.Green, RegexOptions.Multiline);
                HighlightPattern(@"/\*[\s\S]*?\*/", Color.Green, RegexOptions.None);
                
                // Highlight numbers
                HighlightPattern(@"\b\d+\.?\d*\b", Color.DarkCyan, RegexOptions.None);
                
                // Restore selection
                sqlTextBox.SelectionStart = originalSelection;
                sqlTextBox.SelectionLength = originalLength;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error during syntax highlighting");
            }
        }
        
        private void HighlightPattern(string pattern, Color color, RegexOptions options)
        {
            try
            {
                var matches = Regex.Matches(sqlTextBox.Text, pattern, options);
                foreach (Match match in matches)
                {
                    sqlTextBox.SelectionStart = match.Index;
                    sqlTextBox.SelectionLength = match.Length;
                    sqlTextBox.SelectionColor = color;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error highlighting pattern: {Pattern}", pattern);
            }
        }
        
        private async void ExecuteQuery_Click(object sender, EventArgs e)
        {
            await ExecuteQueryAsync();
        }
        
        public async System.Threading.Tasks.Task ExecuteQueryAsync()
        {
            if (currentConnection == null)
            {
                messagesTextBox.Text = "No database connection available.";
                tabControl.SelectedTab = messagesTab;
                return;
            }
            
            var sql = GetSelectedTextOrAll();
            if (string.IsNullOrWhiteSpace(sql))
            {
                messagesTextBox.Text = "No SQL statement to execute.";
                tabControl.SelectedTab = messagesTab;
                return;
            }
            
            LoggingService.LogUserAction("Execute Query", "SQL Length: {0} characters", sql.Length);
            
            try
            {
                statusLabel.Text = "Executing query...";
                progressBar.Visible = true;
                toolStrip.Enabled = false;
                
                var results = await ExecuteSqlAsync(sql);
                
                DisplayResults(results);
                AddToHistory(sql, results);
                
                QueryExecuted?.Invoke(this, new QueryExecutedEventArgs(sql, results));
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Query execution failed");
                messagesTextBox.Text = $"Error: {ex.Message}";
                tabControl.SelectedTab = messagesTab;
            }
            finally
            {
                statusLabel.Text = "Ready";
                progressBar.Visible = false;
                toolStrip.Enabled = true;
            }
        }
        
        private async System.Threading.Tasks.Task<QueryResult> ExecuteSqlAsync(string sql)
        {
            var result = new QueryResult();
            var messages = new StringBuilder();
            
            using (var timer = new PerformanceTimer("SQL Query Execution", currentDatabase))
            {
                try
                {
                    using (var command = new SqlCommand(sql, currentConnection))
                    {
                        command.CommandTimeout = 300; // 5 minutes
                        
                        var startTime = DateTime.Now;
                        
                        if (sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                            sql.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                        {
                            // Query returns data
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                var dataSet = new DataSet();
                                adapter.Fill(dataSet);
                                
                                if (dataSet.Tables.Count > 0)
                                {
                                    result.DataTable = dataSet.Tables[0];
                                    result.RowsAffected = result.DataTable.Rows.Count;
                                }
                            }
                        }
                        else
                        {
                            // Command doesn't return data
                            result.RowsAffected = await command.ExecuteNonQueryAsync();
                        }
                        
                        var elapsed = DateTime.Now - startTime;
                        result.ExecutionTime = elapsed;
                        
                        messages.AppendLine($"Command completed successfully.");
                        messages.AppendLine($"Rows affected: {result.RowsAffected}");
                        messages.AppendLine($"Execution time: {elapsed.TotalMilliseconds:F0} ms");
                        
                        result.Messages = messages.ToString();
                        result.Success = true;
                    }
                }
                catch (Exception ex)
                {
                    timer.SetException(ex);
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.Messages = $"Error: {ex.Message}";
                    throw;
                }
            }
            
            return result;
        }
        
        private void DisplayResults(QueryResult result)
        {
            messagesTextBox.Text = result.Messages;
            
            if (result.DataTable != null)
            {
                resultsDataGridView.DataSource = result.DataTable;
                tabControl.SelectedTab = resultsTab;
                
                // Apply theme to results grid - using parent form
                try
                {
                    var parentForm = this.FindForm();
                    if (parentForm != null)
                    {
                        ThemeManager.ApplyTheme(parentForm, ThemeManager.CurrentTheme);
                    }
                }
                catch
                {
                    // Theme application failed, continue without it
                }
            }
            else
            {
                resultsDataGridView.DataSource = null;
                if (!result.Success)
                {
                    tabControl.SelectedTab = messagesTab;
                }
            }
        }
        
        private string GetSelectedTextOrAll()
        {
            return !string.IsNullOrEmpty(sqlTextBox.SelectedText) ? sqlTextBox.SelectedText : sqlTextBox.Text;
        }
        
        private void AddToHistory(string sql, QueryResult result)
        {
            var historyItem = new QueryHistoryItem
            {
                Sql = sql,
                ExecutedAt = DateTime.Now,
                Success = result.Success,
                RowsAffected = result.RowsAffected,
                ExecutionTime = result.ExecutionTime
            };
            
            queryHistory.Add(historyItem);
            
            // Keep only last 50 queries
            if (queryHistory.Count > 50)
            {
                queryHistory.RemoveAt(0);
            }
        }
        
        // Method to update the connection from the main form
        public void UpdateConnection(SqlConnection newConnection)
        {
            currentConnection = newConnection;
            UpdateConnectionStatus();
        }
        
        private void UpdateConnectionStatus()
        {
            if (currentConnection != null && currentConnection.State == ConnectionState.Open)
            {
                statusLabel.Text = $"Connected to {currentConnection.DataSource} - {currentDatabase ?? "No database selected"}";
            }
            else
            {
                statusLabel.Text = "Not connected";
            }
        }
        
        // Event handlers for toolbar buttons
        private void NewQuery_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(sqlTextBox.Text))
            {
                var result = MessageBox.Show("Clear current query?", "New Query", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;
            }
            
            sqlTextBox.Clear();
            resultsDataGridView.DataSource = null;
            messagesTextBox.Clear();
            
            LoggingService.LogUserAction("New Query");
        }
        
        private void OpenQuery_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "SQL files (*.sql)|*.sql|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        sqlTextBox.Text = System.IO.File.ReadAllText(openFileDialog.FileName);
                        LoggingService.LogUserAction("Open Query", "File: {0}", openFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError(ex, "Failed to open query file: {FileName}", openFileDialog.FileName);
                        MessageBox.Show($"Error opening file: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void SaveQuery_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "SQL files (*.sql)|*.sql|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.DefaultExt = "sql";
                
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        System.IO.File.WriteAllText(saveFileDialog.FileName, sqlTextBox.Text);
                        LoggingService.LogUserAction("Save Query", "File: {0}", saveFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError(ex, "Failed to save query file: {FileName}", saveFileDialog.FileName);
                        MessageBox.Show($"Error saving file: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void CommentLines_Click(object sender, EventArgs e)
        {
            var lines = sqlTextBox.Lines;
            var startLine = sqlTextBox.GetLineFromCharIndex(sqlTextBox.SelectionStart);
            var endLine = sqlTextBox.GetLineFromCharIndex(sqlTextBox.SelectionStart + sqlTextBox.SelectionLength);
            
            for (int i = startLine; i <= endLine; i++)
            {
                if (i < lines.Length && !lines[i].TrimStart().StartsWith("--"))
                {
                    lines[i] = "-- " + lines[i];
                }
            }
            
            sqlTextBox.Lines = lines;
            LoggingService.LogUserAction("Comment Lines");
        }
        
        private void UncommentLines_Click(object sender, EventArgs e)
        {
            var lines = sqlTextBox.Lines;
            var startLine = sqlTextBox.GetLineFromCharIndex(sqlTextBox.SelectionStart);
            var endLine = sqlTextBox.GetLineFromCharIndex(sqlTextBox.SelectionStart + sqlTextBox.SelectionLength);
            
            for (int i = startLine; i <= endLine; i++)
            {
                if (i < lines.Length)
                {
                    var trimmed = lines[i].TrimStart();
                    if (trimmed.StartsWith("-- "))
                    {
                        lines[i] = lines[i].Replace("-- ", "");
                    }
                    else if (trimmed.StartsWith("--"))
                    {
                        lines[i] = lines[i].Replace("--", "");
                    }
                }
            }
            
            sqlTextBox.Lines = lines;
            LoggingService.LogUserAction("Uncomment Lines");
        }
        
        private void FormatSql_Click(object sender, EventArgs e)
        {
            var sql = sqlTextBox.Text;
            if (string.IsNullOrWhiteSpace(sql)) return;
            
            // Basic SQL formatting
            sql = Regex.Replace(sql, @"\s+", " "); // Normalize whitespace
            sql = Regex.Replace(sql, @"\bSELECT\b", "\nSELECT", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bFROM\b", "\nFROM", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bWHERE\b", "\nWHERE", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bINNER JOIN\b", "\nINNER JOIN", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bLEFT JOIN\b", "\nLEFT JOIN", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bRIGHT JOIN\b", "\nRIGHT JOIN", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bORDER BY\b", "\nORDER BY", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bGROUP BY\b", "\nGROUP BY", RegexOptions.IgnoreCase);
            
            sqlTextBox.Text = sql.Trim();
            LoggingService.LogUserAction("Format SQL");
        }
        
        private void SqlTextBox_TextChanged(object sender, EventArgs e)
        {
            // Delayed syntax highlighting to avoid performance issues
            if (sqlTextBox.Text.Length < 10000) // Only highlight smaller texts
            {
                HighlightSyntax();
            }
        }
        
        private void SqlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                _ = ExecuteQueryAsync();
            }
            else if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                _ = ExecuteQueryAsync();
            }
        }
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            sqlTextBox.Focus();
        }
    }
    
    // Supporting classes
    public class QueryResult
    {
        public DataTable DataTable { get; set; }
        public int RowsAffected { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string Messages { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    public class QueryHistoryItem
    {
        public string Sql { get; set; }
        public DateTime ExecutedAt { get; set; }
        public bool Success { get; set; }
        public int RowsAffected { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
    
    public class QueryExecutedEventArgs : EventArgs
    {
        public string Sql { get; }
        public QueryResult Result { get; }
        
        public QueryExecutedEventArgs(string sql, QueryResult result)
        {
            Sql = sql;
            Result = result;
        }
    }
}
