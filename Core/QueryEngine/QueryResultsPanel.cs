using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OfficeOpenXml;
using System.Threading.Tasks;
using SqlServerManager.UI;

namespace SqlServerManager.Core.QueryEngine
{
    /// <summary>
    /// Panel for displaying SQL query results with tabbed interface and export capabilities
    /// </summary>
    public partial class QueryResultsPanel : UserControl
    {
        private TabControl resultsTabControl;
        private ContextMenuStrip tabContextMenu;
        private ContextMenuStrip gridContextMenu;
        private ToolStrip toolStrip;
        private ToolStripButton exportButton;
        private ToolStripButton clearAllButton;
        private ToolStripLabel statusLabel;
        
        private int nextTabNumber = 1;
        private readonly Dictionary<TabPage, QueryResult> tabResults;

        public QueryResultsPanel()
        {
            tabResults = new Dictionary<TabPage, QueryResult>();
            InitializeComponent();
            SetupContextMenus();
        }

        public event EventHandler<QueryResultEventArgs> ResultAdded;
        public event EventHandler<TabClosedEventArgs> TabClosed;

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Main container
            this.BackColor = Color.White;
            this.Dock = DockStyle.Fill;
            this.Font = FontManager.GetScaledFont("Segoe UI", 9);

            // Create toolbar
            CreateToolbar();

            // Create tab control
            resultsTabControl = new TabControl();
            resultsTabControl.Dock = DockStyle.Fill;
            resultsTabControl.Appearance = TabAppearance.Normal;
            resultsTabControl.Multiline = false;
            resultsTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            resultsTabControl.DrawItem += ResultsTabControl_DrawItem;
            resultsTabControl.MouseDown += ResultsTabControl_MouseDown;
            resultsTabControl.SelectedIndexChanged += ResultsTabControl_SelectedIndexChanged;

            // Add welcome tab
            AddWelcomeTab();

            this.Controls.Add(resultsTabControl);
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

            exportButton = new ToolStripButton("Export", null, ExportButton_Click);
            exportButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            exportButton.Enabled = false;

            clearAllButton = new ToolStripButton("Clear All", null, ClearAllButton_Click);
            clearAllButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

            statusLabel = new ToolStripLabel("Ready");
            statusLabel.Alignment = ToolStripItemAlignment.Right;
            statusLabel.Font = FontManager.GetScaledFont("Segoe UI", 8);
            statusLabel.ForeColor = Color.Gray;

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                exportButton,
                new ToolStripSeparator(),
                clearAllButton,
                statusLabel
            });
        }

        private void SetupContextMenus()
        {
            // Tab context menu
            tabContextMenu = new ContextMenuStrip();
            tabContextMenu.Items.Add("Close Tab", null, CloseTab_Click);
            tabContextMenu.Items.Add("Close Other Tabs", null, CloseOtherTabs_Click);
            tabContextMenu.Items.Add("Close All Tabs", null, CloseAllTabs_Click);
            tabContextMenu.Items.Add(new ToolStripSeparator());
            tabContextMenu.Items.Add("Export Results...", null, ExportResults_Click);

            // Grid context menu
            gridContextMenu = new ContextMenuStrip();
            gridContextMenu.Items.Add("Copy", null, CopyCell_Click);
            gridContextMenu.Items.Add("Copy Row", null, CopyRow_Click);
            gridContextMenu.Items.Add("Copy All Data", null, CopyAllData_Click);
            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add("Select All", null, SelectAll_Click);
        }

        private void AddWelcomeTab()
        {
            var welcomeTab = new TabPage("Welcome");
            var welcomeLabel = new Label();
            welcomeLabel.Text = "Execute a SQL query to see results here.\n\nTips:\n• Each query result opens in a new tab\n• Right-click tabs for more options\n• Export results to CSV or Excel";
            welcomeLabel.Dock = DockStyle.Fill;
            welcomeLabel.TextAlign = ContentAlignment.MiddleCenter;
            welcomeLabel.Font = FontManager.GetScaledFont("Segoe UI", 10);
            welcomeLabel.ForeColor = Color.Gray;
            
            welcomeTab.Controls.Add(welcomeLabel);
            resultsTabControl.TabPages.Add(welcomeTab);
        }

        /// <summary>
        /// Add query result to a new tab
        /// </summary>
        public TabPage AddQueryResult(QueryResult result)
        {
            // Remove welcome tab if it exists
            if (resultsTabControl.TabPages.Count == 1 && resultsTabControl.TabPages[0].Text == "Welcome")
            {
                resultsTabControl.TabPages.RemoveAt(0);
            }

            var tabPage = new TabPage($"Results {nextTabNumber++}");
            tabPage.Tag = "closable"; // Mark as closable
            
            // Create split container for results and messages
            var splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Horizontal;
            splitContainer.SplitterDistance = (int)(this.Height * 0.7);
            splitContainer.Panel2Collapsed = string.IsNullOrEmpty(result.Messages);

            // Results grid
            var dataGridView = CreateResultsGrid(result.Data);
            splitContainer.Panel1.Controls.Add(dataGridView);

            // Messages text box (if there are messages)
            if (!string.IsNullOrEmpty(result.Messages))
            {
                var messagesTextBox = new TextBox();
                messagesTextBox.Multiline = true;
                messagesTextBox.ReadOnly = true;
                messagesTextBox.ScrollBars = ScrollBars.Both;
                messagesTextBox.Dock = DockStyle.Fill;
                messagesTextBox.Text = result.Messages;
                messagesTextBox.Font = FontManager.GetScaledFont("Consolas", 9);
                splitContainer.Panel2.Controls.Add(messagesTextBox);
            }

            tabPage.Controls.Add(splitContainer);

            // Add timing and row count info
            var infoText = $"Rows: {result.RowCount:N0} | Execution Time: {result.ExecutionTime.TotalMilliseconds:F2} ms";
            if (result.ExecutionTime.TotalSeconds > 1)
            {
                infoText = $"Rows: {result.RowCount:N0} | Execution Time: {result.ExecutionTime.TotalSeconds:F2} seconds";
            }
            
            tabPage.ToolTipText = infoText;

            resultsTabControl.TabPages.Add(tabPage);
            resultsTabControl.SelectedTab = tabPage;

            // Store result data
            tabResults[tabPage] = result;

            // Update toolbar
            exportButton.Enabled = true;
            statusLabel.Text = infoText;

            ResultAdded?.Invoke(this, new QueryResultEventArgs(result, tabPage));

            return tabPage;
        }

        private ModernDataGrid CreateResultsGrid(DataTable data)
        {
            var grid = new ModernDataGrid();
            grid.Dock = DockStyle.Fill;
            grid.DataSource = data;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = true;
            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            grid.ContextMenuStrip = gridContextMenu;

            // Auto-resize columns but limit width
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    if (column.Width > 300)
                        column.Width = 300;
                }
            };

            return grid;
        }

        private void ResultsTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = sender as TabControl;
            var tabPage = tabControl.TabPages[e.Index];
            var tabRect = tabControl.GetTabRect(e.Index);

            // Draw background
            var backColor = e.Index == tabControl.SelectedIndex ? Color.White : Color.LightGray;
            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, tabRect);
            }

            // Draw text
            var textRect = tabRect;
            textRect.Width -= 20; // Leave space for close button

            TextRenderer.DrawText(e.Graphics, tabPage.Text, tabControl.Font, textRect,
                Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Draw close button for closable tabs
            if (tabPage.Tag?.ToString() == "closable")
            {
                var closeRect = new Rectangle(tabRect.Right - 18, tabRect.Top + 4, 14, 14);
                e.Graphics.DrawString("×", new Font("Arial", 9, FontStyle.Bold), Brushes.Gray, closeRect);
            }

            // Draw border
            e.Graphics.DrawRectangle(Pens.Gray, tabRect);
        }

        private void ResultsTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Check if close button was clicked
                for (int i = 0; i < resultsTabControl.TabPages.Count; i++)
                {
                    var tabRect = resultsTabControl.GetTabRect(i);
                    var closeRect = new Rectangle(tabRect.Right - 18, tabRect.Top + 4, 14, 14);
                    
                    if (closeRect.Contains(e.Location) && resultsTabControl.TabPages[i].Tag?.ToString() == "closable")
                    {
                        CloseTab(resultsTabControl.TabPages[i]);
                        return;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Show context menu for tab
                for (int i = 0; i < resultsTabControl.TabPages.Count; i++)
                {
                    var tabRect = resultsTabControl.GetTabRect(i);
                    if (tabRect.Contains(e.Location))
                    {
                        resultsTabControl.SelectedIndex = i;
                        tabContextMenu.Show(resultsTabControl, e.Location);
                        return;
                    }
                }
            }
        }

        private void ResultsTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (resultsTabControl.SelectedTab != null && tabResults.TryGetValue(resultsTabControl.SelectedTab, out var result))
            {
                var infoText = $"Rows: {result.RowCount:N0} | Execution Time: {result.ExecutionTime.TotalMilliseconds:F2} ms";
                if (result.ExecutionTime.TotalSeconds > 1)
                {
                    infoText = $"Rows: {result.RowCount:N0} | Execution Time: {result.ExecutionTime.TotalSeconds:F2} seconds";
                }
                statusLabel.Text = infoText;
            }
        }

        #region Event Handlers

        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (resultsTabControl.SelectedTab != null && tabResults.TryGetValue(resultsTabControl.SelectedTab, out var result))
            {
                ExportResults(result);
            }
        }

        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            ClearAllResults();
        }

        private void CloseTab_Click(object sender, EventArgs e)
        {
            if (resultsTabControl.SelectedTab != null && resultsTabControl.SelectedTab.Tag?.ToString() == "closable")
            {
                CloseTab(resultsTabControl.SelectedTab);
            }
        }

        private void CloseOtherTabs_Click(object sender, EventArgs e)
        {
            var selectedTab = resultsTabControl.SelectedTab;
            var tabsToClose = resultsTabControl.TabPages.Cast<TabPage>()
                .Where(t => t != selectedTab && t.Tag?.ToString() == "closable")
                .ToList();

            foreach (var tab in tabsToClose)
            {
                CloseTab(tab);
            }
        }

        private void CloseAllTabs_Click(object sender, EventArgs e)
        {
            ClearAllResults();
        }

        private void ExportResults_Click(object sender, EventArgs e)
        {
            if (resultsTabControl.SelectedTab != null && tabResults.TryGetValue(resultsTabControl.SelectedTab, out var result))
            {
                ExportResults(result);
            }
        }

        private void CopyCell_Click(object sender, EventArgs e)
        {
            var grid = GetActiveGrid();
            if (grid?.CurrentCell != null)
            {
                var value = grid.CurrentCell.Value?.ToString() ?? "";
                Clipboard.SetText(value);
            }
        }

        private void CopyRow_Click(object sender, EventArgs e)
        {
            var grid = GetActiveGrid();
            if (grid?.SelectedRows.Count > 0)
            {
                grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
                var data = grid.GetClipboardContent();
                if (data != null)
                {
                    Clipboard.SetDataObject(data);
                }
            }
        }

        private void CopyAllData_Click(object sender, EventArgs e)
        {
            var grid = GetActiveGrid();
            if (grid != null)
            {
                grid.SelectAll();
                grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
                var data = grid.GetClipboardContent();
                if (data != null)
                {
                    Clipboard.SetDataObject(data);
                }
            }
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {
            var grid = GetActiveGrid();
            grid?.SelectAll();
        }

        #endregion

        #region Public Methods

        public void ClearAllResults()
        {
            var tabsToClose = resultsTabControl.TabPages.Cast<TabPage>()
                .Where(t => t.Tag?.ToString() == "closable")
                .ToList();

            foreach (var tab in tabsToClose)
            {
                CloseTab(tab);
            }

            // Add welcome tab if no tabs left
            if (resultsTabControl.TabPages.Count == 0)
            {
                AddWelcomeTab();
            }

            exportButton.Enabled = false;
            statusLabel.Text = "Ready";
        }

        public void CloseTab(TabPage tab)
        {
            if (tab != null && resultsTabControl.TabPages.Contains(tab))
            {
                tabResults.Remove(tab);
                resultsTabControl.TabPages.Remove(tab);
                
                TabClosed?.Invoke(this, new TabClosedEventArgs(tab));

                if (resultsTabControl.TabPages.Count == 0)
                {
                    AddWelcomeTab();
                    exportButton.Enabled = false;
                    statusLabel.Text = "Ready";
                }
            }
        }

        public QueryResult GetCurrentResult()
        {
            if (resultsTabControl.SelectedTab != null && tabResults.TryGetValue(resultsTabControl.SelectedTab, out var result))
            {
                return result;
            }
            return null;
        }

        #endregion

        #region Private Methods

        private ModernDataGrid GetActiveGrid()
        {
            if (resultsTabControl.SelectedTab?.Controls.Count > 0)
            {
                var splitContainer = resultsTabControl.SelectedTab.Controls[0] as SplitContainer;
                return splitContainer?.Panel1.Controls.OfType<ModernDataGrid>().FirstOrDefault();
            }
            return null;
        }

        private async void ExportResults(QueryResult result)
        {
            using var saveDialog = new SaveFileDialog();
            saveDialog.Title = "Export Query Results";
            saveDialog.Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt";
            saveDialog.DefaultExt = "xlsx";
            saveDialog.FileName = $"QueryResults_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    statusLabel.Text = "Exporting...";
                    this.Cursor = Cursors.WaitCursor;

                    await Task.Run(() =>
                    {
                        var extension = Path.GetExtension(saveDialog.FileName).ToLower();
                        switch (extension)
                        {
                            case ".xlsx":
                                ExportToExcel(result.Data, saveDialog.FileName);
                                break;
                            case ".csv":
                                ExportToCsv(result.Data, saveDialog.FileName);
                                break;
                            case ".txt":
                                ExportToText(result.Data, saveDialog.FileName);
                                break;
                        }
                    });

                    statusLabel.Text = $"Exported to {Path.GetFileName(saveDialog.FileName)}";
                    
                    // Show success message
                    MessageBox.Show($"Results exported successfully to:\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    statusLabel.Text = "Export failed";
                    MessageBox.Show($"Error exporting results: {ex.Message}", 
                        "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LoggingService.LogError(ex, "Error exporting query results");
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void ExportToExcel(DataTable data, string fileName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Query Results");
            
            // Add headers
            for (int col = 0; col < data.Columns.Count; col++)
            {
                worksheet.Cells[1, col + 1].Value = data.Columns[col].ColumnName;
                worksheet.Cells[1, col + 1].Style.Font.Bold = true;
            }

            // Add data
            for (int row = 0; row < data.Rows.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    worksheet.Cells[row + 2, col + 1].Value = data.Rows[row][col];
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            package.SaveAs(new FileInfo(fileName));
        }

        private void ExportToCsv(DataTable data, string fileName)
        {
            var csv = new StringBuilder();
            
            // Add headers
            csv.AppendLine(string.Join(",", data.Columns.Cast<DataColumn>()
                .Select(column => $"\"{column.ColumnName}\"")));

            // Add data rows
            foreach (DataRow row in data.Rows)
            {
                csv.AppendLine(string.Join(",", row.ItemArray
                    .Select(field => $"\"{field?.ToString()?.Replace("\"", "\"\"")}\"")));
            }

            File.WriteAllText(fileName, csv.ToString(), Encoding.UTF8);
        }

        private void ExportToText(DataTable data, string fileName)
        {
            var text = new StringBuilder();
            
            // Calculate column widths
            var columnWidths = new int[data.Columns.Count];
            for (int i = 0; i < data.Columns.Count; i++)
            {
                columnWidths[i] = Math.Max(data.Columns[i].ColumnName.Length, 
                    data.Rows.Cast<DataRow>().Max(row => row[i]?.ToString()?.Length ?? 0));
                columnWidths[i] = Math.Min(columnWidths[i], 50); // Limit column width
            }

            // Add headers
            for (int i = 0; i < data.Columns.Count; i++)
            {
                text.Append(data.Columns[i].ColumnName.PadRight(columnWidths[i] + 2));
            }
            text.AppendLine();

            // Add separator
            for (int i = 0; i < data.Columns.Count; i++)
            {
                text.Append(new string('-', columnWidths[i] + 2));
            }
            text.AppendLine();

            // Add data rows
            foreach (DataRow row in data.Rows)
            {
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    var value = row[i]?.ToString() ?? "";
                    if (value.Length > columnWidths[i])
                        value = value.Substring(0, columnWidths[i] - 3) + "...";
                    text.Append(value.PadRight(columnWidths[i] + 2));
                }
                text.AppendLine();
            }

            File.WriteAllText(fileName, text.ToString(), Encoding.UTF8);
        }

        #endregion
    }

    #region Supporting Classes

    public class QueryResult
    {
        public DataTable Data { get; set; }
        public string Messages { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int RowCount => Data?.Rows.Count ?? 0;
        public DateTime ExecutedAt { get; set; }
        public string QueryText { get; set; }
    }

    public class QueryResultEventArgs : EventArgs
    {
        public QueryResult Result { get; }
        public TabPage TabPage { get; }

        public QueryResultEventArgs(QueryResult result, TabPage tabPage)
        {
            Result = result;
            TabPage = tabPage;
        }
    }

    public class TabClosedEventArgs : EventArgs
    {
        public TabPage TabPage { get; }

        public TabClosedEventArgs(TabPage tabPage)
        {
            TabPage = tabPage;
        }
    }

    #endregion
}
