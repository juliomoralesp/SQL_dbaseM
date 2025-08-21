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

namespace SqlServerManager.Core.QueryEngine
{
    /// <summary>
    /// Advanced SQL editor with ScintillaNET, syntax highlighting, IntelliSense, and query execution
    /// </summary>
    public partial class ScintillaSqlEditor : UserControl
    {
        private Scintilla sqlEditor;
        private QueryResultsPanel resultsPanel;
        private SplitContainer mainSplitContainer;
        private ToolStrip toolStrip;
        private ToolStripButton executeButton;
        private ToolStripButton executeSelectedButton;
        private ToolStripButton formatButton;
        private ToolStripComboBox databaseCombo;
        private ToolStripLabel statusLabel;
        
        private readonly ConnectionService _connectionService;
        private readonly SqlIntelliSense _intelliSense;
        private string _currentDatabase;
        private bool _isExecuting;

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

        public ScintillaSqlEditor(ConnectionService connectionService)
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

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Main container
            this.BackColor = Color.White;
            this.Dock = DockStyle.Fill;
            this.Font = FontManager.GetScaledFont("Segoe UI", 9);

            // Create toolbar
            CreateToolbar();

            // Create main split container
            mainSplitContainer = new SplitContainer();
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.Orientation = Orientation.Horizontal;
            mainSplitContainer.SplitterDistance = 250;
            mainSplitContainer.Panel1MinSize = 100;
            mainSplitContainer.Panel2MinSize = 100;

            // Create SQL editor
            sqlEditor = new Scintilla();
            sqlEditor.Dock = DockStyle.Fill;
            mainSplitContainer.Panel1.Controls.Add(sqlEditor);

            // Create results panel
            resultsPanel = new QueryResultsPanel();
            resultsPanel.Dock = DockStyle.Fill;
            mainSplitContainer.Panel2.Controls.Add(resultsPanel);

            this.Controls.Add(mainSplitContainer);
            this.Controls.Add(toolStrip);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CreateToolbar()
        {
            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.BackColor = Color.FromArgb(240, 240, 240);

            executeButton = new ToolStripButton("Execute (F5)", null, ExecuteButton_Click);
            executeButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            executeButton.Font = FontManager.GetScaledFont("Segoe UI", 9, FontStyle.Bold);

            executeSelectedButton = new ToolStripButton("Execute Selected", null, ExecuteSelectedButton_Click);
            executeSelectedButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

            formatButton = new ToolStripButton("Format", null, FormatButton_Click);
            formatButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

            databaseCombo = new ToolStripComboBox("Database");
            databaseCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            databaseCombo.Size = new Size(150, 23);
            databaseCombo.SelectedIndexChanged += DatabaseCombo_SelectedIndexChanged;

            statusLabel = new ToolStripLabel("Ready");
            statusLabel.Alignment = ToolStripItemAlignment.Right;
            statusLabel.Font = FontManager.GetScaledFont("Segoe UI", 8);
            statusLabel.ForeColor = Color.Gray;

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                executeButton,
                executeSelectedButton,
                new ToolStripSeparator(),
                formatButton,
                new ToolStripSeparator(),
                new ToolStripLabel("Database:"),
                databaseCombo,
                statusLabel
            });
        }

        private void SetupSqlEditor()
        {
            // Basic configuration
            sqlEditor.StyleResetDefault();
            sqlEditor.Styles[Style.Default].Font = "Consolas";
            sqlEditor.Styles[Style.Default].Size = 11;
            sqlEditor.StyleClearAll();

            // Configure the lexer
            sqlEditor.Lexer = Lexer.Sql;

            // Set SQL keywords
            var keywordString = string.Join(" ", SQL_KEYWORDS);
            sqlEditor.SetKeywords(0, keywordString.ToLower());
            sqlEditor.SetKeywords(1, keywordString.ToUpper());

            // Style the syntax elements
            sqlEditor.Styles[Style.Sql.Default].ForeColor = Color.Black;
            sqlEditor.Styles[Style.Sql.Comment].ForeColor = Color.Green;
            sqlEditor.Styles[Style.Sql.CommentLine].ForeColor = Color.Green;
            sqlEditor.Styles[Style.Sql.CommentDoc].ForeColor = Color.Green;
            sqlEditor.Styles[Style.Sql.Number].ForeColor = Color.Red;
            sqlEditor.Styles[Style.Sql.Word].ForeColor = Color.Blue;
            sqlEditor.Styles[Style.Sql.Word].Bold = true;
            sqlEditor.Styles[Style.Sql.String].ForeColor = Color.Brown;
            sqlEditor.Styles[Style.Sql.Character].ForeColor = Color.Brown;
            sqlEditor.Styles[Style.Sql.SqlPlus].ForeColor = Color.Magenta;
            sqlEditor.Styles[Style.Sql.SqlPlus].Bold = true;
            sqlEditor.Styles[Style.Sql.User1].ForeColor = Color.Purple; // Functions
            sqlEditor.Styles[Style.Sql.User2].ForeColor = Color.DarkCyan; // System tables

            // Configure indentation
            sqlEditor.IndentationGuides = IndentView.LookBoth;
            sqlEditor.UseTabs = false;
            sqlEditor.TabWidth = 4;

            // Configure margins
            sqlEditor.Margins[0].Type = MarginType.Number;
            sqlEditor.Margins[0].Width = 50;
            sqlEditor.Margins[0].Mask = 0;
            sqlEditor.Margins[1].Type = MarginType.Symbol;
            sqlEditor.Margins[1].Width = 20;
            sqlEditor.Margins[1].Mask = Marker.MaskFolders;
            sqlEditor.Margins[1].Sensitive = true;

            // Configure folding
            sqlEditor.SetProperty("fold", "1");
            sqlEditor.SetProperty("fold.compact", "1");
            sqlEditor.SetProperty("fold.comment", "1");

            // Configure markers for folding
            sqlEditor.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            sqlEditor.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            sqlEditor.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            sqlEditor.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            sqlEditor.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            sqlEditor.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            sqlEditor.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            sqlEditor.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

            // Configure bracket matching
            sqlEditor.Styles[Style.BraceLight].BackColor = Color.LightGray;
            sqlEditor.Styles[Style.BraceLight].ForeColor = Color.BlueViolet;
            sqlEditor.Styles[Style.BraceBad].BackColor = Color.Red;
            sqlEditor.Styles[Style.BraceBad].ForeColor = Color.White;

            // Configure current line highlighting
            sqlEditor.CaretLineVisible = true;
            sqlEditor.CaretLineBackColor = Color.FromArgb(245, 245, 245);

            // Configure selections
            sqlEditor.SetSelectionBackColor(true, Color.FromArgb(173, 214, 255));
            sqlEditor.SetSelectionForeColor(true, Color.Black);

            // Configure word wrapping
            sqlEditor.WrapMode = WrapMode.None; // Can be changed via settings

            // Configure zoom and mouse events handled in SetupEventHandlers

            // Configure auto-completion
            sqlEditor.AutoCMaxWidth = 300;
            sqlEditor.AutoCOrder = Order.PerformSort;
            sqlEditor.AutoCIgnoreCase = true;
            sqlEditor.AutoCSeparator = ' ';

            // Configure brace matching
            sqlEditor.Styles[Style.BraceLight].BackColor = Color.LightGray;
            sqlEditor.Styles[Style.BraceBad].BackColor = Color.Red;

            // Set initial text
            sqlEditor.Text = "-- Welcome to Advanced SQL Editor\n-- Press F5 to execute queries\n-- Type to see IntelliSense suggestions\n\nSELECT ";
            sqlEditor.GotoPosition(sqlEditor.Text.Length);

            // Enable current line highlighting
            sqlEditor.CaretLineVisible = true;
            sqlEditor.CaretLineBackColor = Color.FromArgb(245, 245, 245);
        }

        private void SetupEventHandlers()
        {
            // Key press events for shortcuts
            sqlEditor.KeyDown += SqlEditor_KeyDown;
            
            // Auto-completion events
            sqlEditor.CharAdded += SqlEditor_CharAdded;
            sqlEditor.AutoCCompleted += SqlEditor_AutoCCompleted;
            
            // Selection and cursor events
            sqlEditor.UpdateUI += SqlEditor_UpdateUI;
            
            // Margin events for folding
            sqlEditor.MarginClick += SqlEditor_MarginClick;
        }

        #region Event Handlers

        private async void SqlEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                await ExecuteQuery();
            }
            else if (e.KeyCode == Keys.Space && e.Control)
            {
                e.Handled = true;
                await ShowIntelliSense();
            }
            else if (e.KeyCode == Keys.F && e.Control && e.Shift)
            {
                e.Handled = true;
                FormatSql();
            }
        }

        private async void SqlEditor_CharAdded(object sender, CharAddedEventArgs e)
        {
            // Trigger IntelliSense on certain characters
            if (char.IsLetter((char)e.Char) || e.Char == '.' || e.Char == ' ')
            {
                // Small delay to avoid too many requests
                await Task.Delay(300);
                if (!sqlEditor.AutoCActive)
                {
                    await ShowIntelliSense();
                }
            }

            // Auto-close brackets and quotes
            var currentPos = sqlEditor.CurrentPosition;
            if (e.Char == '(' && sqlEditor.GetCharAt(currentPos) != ')')
            {
                sqlEditor.InsertText(currentPos, ")");
            }
            else if (e.Char == '\'' && sqlEditor.GetCharAt(currentPos) != '\'')
            {
                sqlEditor.InsertText(currentPos, "'");
            }
        }

        private void SqlEditor_AutoCCompleted(object sender, AutoCSelectionEventArgs e)
        {
            // Handle completion selection
            LoggingService.LogDebug("Auto-completion selected: {Text}", e.Text);
        }

        private void SqlEditor_UpdateUI(object sender, UpdateUIEventArgs e)
        {
            // Update brace matching
            var currentPos = sqlEditor.CurrentPosition;
            var bracePos1 = -1;
            var bracePos2 = -1;

            if (currentPos > 0)
            {
                var charBefore = sqlEditor.GetCharAt(currentPos - 1);
                var charAfter = sqlEditor.GetCharAt(currentPos);

                if (IsBrace(charBefore))
                {
                    bracePos1 = currentPos - 1;
                    bracePos2 = sqlEditor.BraceMatch(bracePos1);
                }
                else if (IsBrace(charAfter))
                {
                    bracePos1 = currentPos;
                    bracePos2 = sqlEditor.BraceMatch(bracePos1);
                }
            }

            if (bracePos1 != -1 && bracePos2 != -1)
            {
                sqlEditor.BraceHighlight(bracePos1, bracePos2);
            }
            else
            {
                sqlEditor.BraceBadLight(Scintilla.InvalidPosition);
            }
        }

        private void SqlEditor_MarginClick(object sender, MarginClickEventArgs e)
        {
            if (e.Margin == 1) // Folding margin
            {
                var lineClick = sqlEditor.LineFromPosition(e.Position);
                sqlEditor.FoldAll(FoldAction.Toggle);
            }
        }

        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            await ExecuteQuery();
        }

        private async void ExecuteSelectedButton_Click(object sender, EventArgs e)
        {
            await ExecuteQuery(true);
        }

        private void FormatButton_Click(object sender, EventArgs e)
        {
            FormatSql();
        }

        private async void DatabaseCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (databaseCombo.SelectedItem != null)
            {
                _currentDatabase = databaseCombo.SelectedItem.ToString();
                await _intelliSense.RefreshSchemaCache(_currentDatabase);
            }
        }

        private async void OnConnectionChanged(object sender, ConnectionEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, ConnectionEventArgs>(OnConnectionChanged), sender, e);
                return;
            }

            executeButton.Enabled = e.IsConnected;
            executeSelectedButton.Enabled = e.IsConnected;
            
            if (e.IsConnected)
            {
                await RefreshDatabaseList();
                statusLabel.Text = $"Connected to {e.Server}";
            }
            else
            {
                databaseCombo.Items.Clear();
                statusLabel.Text = "Not connected";
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get the current SQL text in the editor
        /// </summary>
        public string GetSqlText()
        {
            return sqlEditor.Text;
        }

        /// <summary>
        /// Set SQL text in the editor
        /// </summary>
        public void SetSqlText(string sql)
        {
            sqlEditor.Text = sql ?? "";
        }

        /// <summary>
        /// Get selected SQL text
        /// </summary>
        public string GetSelectedText()
        {
            return sqlEditor.SelectedText;
        }

        /// <summary>
        /// Insert text at current cursor position
        /// </summary>
        public void InsertText(string text)
        {
            sqlEditor.ReplaceSelection(text ?? "");
        }

        /// <summary>
        /// Clear all results
        /// </summary>
        public void ClearResults()
        {
            resultsPanel.ClearAllResults();
        }

        /// <summary>
        /// Set focus to the editor
        /// </summary>
        public void FocusEditor()
        {
            sqlEditor.Focus();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Show IntelliSense suggestions
        /// </summary>
        private async Task ShowIntelliSense()
        {
            if (!_connectionService.IsConnected)
                return;

            try
            {
                var currentPos = sqlEditor.CurrentPosition;
                var completions = await _intelliSense.GetCompletionsAsync(
                    sqlEditor.Text, currentPos, _currentDatabase);

                if (completions.Count > 0)
                {
                    var completionText = string.Join(" ", completions.Select(c => c.Text));
                    sqlEditor.AutoCShow(0, completionText);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error showing IntelliSense");
            }
        }

        /// <summary>
        /// Execute SQL query
        /// </summary>
        private async Task ExecuteQuery(bool selectedOnly = false)
        {
            if (_isExecuting || !_connectionService.IsConnected)
                return;

            var sqlText = selectedOnly && !string.IsNullOrEmpty(sqlEditor.SelectedText) 
                ? sqlEditor.SelectedText 
                : sqlEditor.Text;

            if (string.IsNullOrWhiteSpace(sqlText))
            {
                statusLabel.Text = "No query to execute";
                return;
            }

            _isExecuting = true;
            executeButton.Enabled = false;
            executeSelectedButton.Enabled = false;
            statusLabel.Text = "Executing query...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                var result = await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    var command = new SqlCommand(sqlText, conn);
                    command.CommandTimeout = 300; // 5 minutes timeout
                    
                    var dataTable = new DataTable();
                    var messages = new StringBuilder();
                    
                    // Handle info messages
                    conn.InfoMessage += (s, e) =>
                    {
                        messages.AppendLine(e.Message);
                    };

                    try
                    {
                        using var adapter = new SqlDataAdapter(command);
                        adapter.Fill(dataTable);
                    }
                    catch (Exception ex) when (ex.Message.Contains("Invalid column name") == false)
                    {
                        // For non-SELECT statements, we might get an exception
                        // Try to execute as non-query
                        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
                        
                        // Create a result table showing rows affected
                        dataTable = new DataTable();
                        dataTable.Columns.Add("Message", typeof(string));
                        dataTable.Rows.Add($"Command completed successfully. Rows affected: {rowsAffected}");
                    }

                    return new { Data = dataTable, Messages = messages.ToString() };
                });

                stopwatch.Stop();

                var queryResult = new QueryResult
                {
                    Data = result.Data,
                    Messages = result.Messages,
                    ExecutionTime = stopwatch.Elapsed,
                    ExecutedAt = DateTime.Now,
                    QueryText = sqlText
                };

                // Add result to the results panel
                resultsPanel.AddQueryResult(queryResult);

                // Update status
                var statusText = queryResult.RowCount == 1 && queryResult.Data.Rows[0][0].ToString().StartsWith("Command completed")
                    ? queryResult.Data.Rows[0][0].ToString()
                    : $"Query completed. {queryResult.RowCount:N0} rows returned in {queryResult.ExecutionTime.TotalMilliseconds:F2} ms";
                
                statusLabel.Text = statusText;

                // Fire event
                QueryExecuted?.Invoke(this, new QueryExecutedEventArgs(queryResult.QueryText, queryResult.ExecutionTime));

                LoggingService.LogDatabaseOperation("Query Execution", _currentDatabase ?? "Unknown", 
                    "Unknown", stopwatch.ElapsedMilliseconds, true);
            }
            catch (Exception ex)
            {
                var errorResult = new QueryResult
                {
                    Data = new DataTable(),
                    Messages = $"Error: {ex.Message}",
                    ExecutionTime = TimeSpan.Zero,
                    ExecutedAt = DateTime.Now,
                    QueryText = sqlText
                };

                resultsPanel.AddQueryResult(errorResult);
                statusLabel.Text = $"Query failed: {ex.Message}";
                
                LoggingService.LogError(ex, "Error executing SQL query");
                QueryExecuted?.Invoke(this, new QueryExecutedEventArgs(errorResult.QueryText, errorResult.ExecutionTime));
            }
            finally
            {
                _isExecuting = false;
                executeButton.Enabled = true;
                executeSelectedButton.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Format SQL text
        /// </summary>
        private void FormatSql()
        {
            try
            {
                // Basic SQL formatting
                var sql = sqlEditor.Text;
                var formatted = FormatSqlText(sql);
                
                if (formatted != sql)
                {
                    var currentPos = sqlEditor.CurrentPosition;
                    sqlEditor.Text = formatted;
                    
                    // Try to restore cursor position
                    if (currentPos < formatted.Length)
                        sqlEditor.GotoPosition(currentPos);
                    
                    statusLabel.Text = "SQL formatted";
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error formatting SQL");
                statusLabel.Text = "Format failed";
            }
        }

        /// <summary>
        /// Basic SQL formatting implementation
        /// </summary>
        private string FormatSqlText(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            var lines = sql.Split('\n');
            var formatted = new StringBuilder();
            var indentLevel = 0;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    formatted.AppendLine();
                    continue;
                }

                // Decrease indent for closing keywords
                if (IsClosingKeyword(trimmedLine))
                    indentLevel = Math.Max(0, indentLevel - 1);

                // Add indentation
                formatted.Append(new string(' ', indentLevel * 4));
                formatted.AppendLine(trimmedLine);

                // Increase indent for opening keywords
                if (IsOpeningKeyword(trimmedLine))
                    indentLevel++;
            }

            return formatted.ToString();
        }

        private bool IsOpeningKeyword(string line)
        {
            var upper = line.ToUpper();
            return upper.StartsWith("SELECT") || upper.StartsWith("FROM") || 
                   upper.StartsWith("WHERE") || upper.StartsWith("CASE") ||
                   upper.StartsWith("BEGIN");
        }

        private bool IsClosingKeyword(string line)
        {
            var upper = line.ToUpper();
            return upper.StartsWith("END") || upper.EndsWith("END");
        }

        /// <summary>
        /// Check if character is a brace
        /// </summary>
        private bool IsBrace(int character)
        {
            return character == '(' || character == ')' || 
                   character == '[' || character == ']' ||
                   character == '{' || character == '}';
        }

        /// <summary>
        /// Refresh database list
        /// </summary>
        private async Task RefreshDatabaseList()
        {
            try
            {
                var databases = await _connectionService.ExecuteWithConnectionAsync(async (conn, ct) =>
                {
                    var query = "SELECT name FROM sys.databases WHERE state = 0 ORDER BY name";
                    var dbList = new List<string>();
                    
                    using var command = new SqlCommand(query, conn);
                    using var reader = await command.ExecuteReaderAsync(ct);
                    
                    while (await reader.ReadAsync(ct))
                    {
                        dbList.Add(reader.GetString(0));
                    }
                    
                    return dbList;
                });

                databaseCombo.Items.Clear();
                databaseCombo.Items.AddRange(databases.ToArray());
                
                if (databases.Count > 0)
                {
                    // Select current database or first one
                    var currentDb = _connectionService.CurrentDatabase ?? databases[0];
                    var index = databases.IndexOf(currentDb);
                    if (index >= 0)
                    {
                        databaseCombo.SelectedIndex = index;
                        _currentDatabase = currentDb;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error refreshing database list");
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_connectionService != null)
                    _connectionService.ConnectionChanged -= OnConnectionChanged;
                
                sqlEditor?.Dispose();
                resultsPanel?.Dispose();
                mainSplitContainer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
