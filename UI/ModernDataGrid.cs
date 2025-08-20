using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SqlServerManager.Services;
using SqlServerManager.UI;

namespace SqlServerManager.UI
{
    public class ModernDataGrid : UserControl
    {
        private DataGridView dataGridView;
        private Panel controlPanel;
        private Label statusLabel;
        private Panel paginationPanel;
        private Button firstPageButton;
        private Button previousPageButton;
        private Button nextPageButton;
        private Button lastPageButton;
        private Label pageInfoLabel;
        private TextBox pageTextBox;
        private ComboBox pageSizeComboBox;
        private Label pageSizeLabel;
        private Panel filterPanel;
        private TextBox searchTextBox;
        private Button searchButton;
        private Button clearFilterButton;
        private ToolStrip toolbar;
        private ToolStripButton exportButton;
        private ToolStripButton refreshButton;
        private ToolStripSeparator toolStripSeparator;
        private ToolStripLabel recordCountLabel;

        private DataTable originalDataSource;
        private DataTable filteredDataSource;
        private int currentPage = 1;
        private int pageSize = 1000;
        private int totalRecords = 0;
        private string currentFilter = "";
        private Dictionary<string, string> columnFilters = new Dictionary<string, string>();

        public event EventHandler<PageChangedEventArgs> PageChanged;
        public event EventHandler<FilterChangedEventArgs> FilterChanged;
        public event EventHandler<CellValueChangedEventArgs> CellValueChanged;
        public event EventHandler DataRefreshRequested;
        public event EventHandler DataExportRequested;

        public DataTable DataSource
        {
            get => originalDataSource;
            set => SetDataSource(value);
        }

        public bool AllowPaging { get; set; } = true;
        public bool AllowFiltering { get; set; } = true;
        public bool AllowExport { get; set; } = true;
        public bool ReadOnly { get; set; } = false;
        public bool ShowLineNumbers { get; set; } = true;

        public int CurrentPage => currentPage;
        public int PageSize => pageSize;
        public int TotalRecords => totalRecords;
        public int TotalPages => (int)Math.Ceiling((double)totalRecords / pageSize);

        public ModernDataGrid()
        {
            InitializeComponent();
            ApplyTheme();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary;

            // Create toolbar
            CreateToolbar();

            // Create filter panel
            CreateFilterPanel();

            // Create main data grid
            CreateDataGridView();

            // Create control panel (status and pagination)
            CreateControlPanel();

            // Layout controls
            this.Controls.Add(dataGridView);
            this.Controls.Add(controlPanel);
            this.Controls.Add(filterPanel);
            this.Controls.Add(toolbar);
        }

        private void CreateToolbar()
        {
            toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary
            };

            refreshButton = new ToolStripButton
            {
                Text = "↻ Refresh",
                ToolTipText = "Refresh data"
            };
            refreshButton.Click += RefreshButton_Click;

            exportButton = new ToolStripButton
            {
                Text = "📊 Export",
                ToolTipText = "Export data"
            };
            exportButton.Click += ExportButton_Click;

            toolStripSeparator = new ToolStripSeparator();

            recordCountLabel = new ToolStripLabel
            {
                Text = "No records"
            };

            toolbar.Items.AddRange(new ToolStripItem[]
            {
                refreshButton,
                exportButton,
                toolStripSeparator,
                recordCountLabel
            });
        }

        private void CreateFilterPanel()
        {
            filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                Padding = new Padding(5)
            };

            var searchLabel = new Label
            {
                Text = "Search:",
                Location = new Point(5, 12),
                Size = new Size(50, 20),
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary
            };

            searchTextBox = new TextBox
            {
                Location = new Point(60, 10),
                Size = new Size(200, 20),
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            searchTextBox.KeyPress += SearchTextBox_KeyPress;

            searchButton = new Button
            {
                Text = "🔍",
                Location = new Point(270, 9),
                Size = new Size(30, 22),
                FlatStyle = FlatStyle.Flat,
                BackColor = ModernThemeManager.CurrentColors.AccentColor,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            searchButton.FlatAppearance.BorderSize = 0;
            searchButton.Click += SearchButton_Click;

            clearFilterButton = new Button
            {
                Text = "✖",
                Location = new Point(305, 9),
                Size = new Size(30, 22),
                FlatStyle = FlatStyle.Flat,
                BackColor = ModernThemeManager.CurrentColors.BackgroundTertiary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                UseVisualStyleBackColor = false
            };
            clearFilterButton.FlatAppearance.BorderSize = 0;
            clearFilterButton.Click += ClearFilterButton_Click;

            filterPanel.Controls.AddRange(new Control[]
            {
                searchLabel, searchTextBox, searchButton, clearFilterButton
            });
        }

        private void CreateDataGridView()
        {
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = true,
                BackgroundColor = ModernThemeManager.CurrentColors.BackgroundPrimary,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary,
                    ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                    Font = ModernThemeManager.GetScaledFont(this.Font),
                    SelectionBackColor = ModernThemeManager.CurrentColors.AccentColor,
                    SelectionForeColor = Color.White,
                    WrapMode = DataGridViewTriState.False
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                    ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                    Font = new Font(ModernThemeManager.GetScaledFont(this.Font), FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    WrapMode = DataGridViewTriState.False
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                    ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                    Font = ModernThemeManager.GetScaledFont(this.Font)
                },
                RowHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                    ForeColor = ModernThemeManager.CurrentColors.ForegroundSecondary,
                    Font = ModernThemeManager.GetScaledFont(this.Font)
                },
                EnableHeadersVisualStyles = false,
                GridColor = ModernThemeManager.CurrentColors.BorderColor,
                RowHeadersVisible = ShowLineNumbers,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 32
            };

            // Enable sorting
            dataGridView.SortCompare += DataGridView_SortCompare;
        }

        private void CreateControlPanel()
        {
            controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = ModernThemeManager.CurrentColors.BackgroundSecondary,
                Padding = new Padding(10, 5, 10, 5)
            };

            // Status label
            statusLabel = new Label
            {
                Dock = DockStyle.Left,
                Width = 200,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                Font = ModernThemeManager.GetScaledFont(this.Font)
            };

            // Create pagination panel
            CreatePaginationControls();

            controlPanel.Controls.Add(paginationPanel);
            controlPanel.Controls.Add(statusLabel);
        }

        private void CreatePaginationControls()
        {
            paginationPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 400,
                BackColor = Color.Transparent
            };

            // Page size controls
            pageSizeLabel = new Label
            {
                Text = "Page Size:",
                Location = new Point(5, 18),
                Size = new Size(65, 20),
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary
            };

            pageSizeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(75, 16),
                Size = new Size(60, 20),
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary
            };
            pageSizeComboBox.Items.AddRange(new object[] { 100, 500, 1000, 2000, 5000 });
            pageSizeComboBox.SelectedItem = pageSize;
            pageSizeComboBox.SelectedIndexChanged += PageSizeComboBox_SelectedIndexChanged;

            // Navigation buttons
            firstPageButton = CreateNavButton("⏮", 145, FirstPage_Click);
            previousPageButton = CreateNavButton("◀", 175, PreviousPage_Click);
            nextPageButton = CreateNavButton("▶", 235, NextPage_Click);
            lastPageButton = CreateNavButton("⏭", 265, LastPage_Click);

            // Page info
            pageTextBox = new TextBox
            {
                Location = new Point(200, 16),
                Size = new Size(30, 20),
                TextAlign = HorizontalAlignment.Center,
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            pageTextBox.KeyPress += PageTextBox_KeyPress;

            pageInfoLabel = new Label
            {
                Location = new Point(300, 18),
                Size = new Size(80, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary
            };

            paginationPanel.Controls.AddRange(new Control[]
            {
                pageSizeLabel, pageSizeComboBox,
                firstPageButton, previousPageButton, pageTextBox, nextPageButton, lastPageButton,
                pageInfoLabel
            });
        }

        private Button CreateNavButton(string text, int x, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(x, 14),
                Size = new Size(25, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary,
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ModernThemeManager.CurrentColors.BorderColor;
            button.Click += clickHandler;
            return button;
        }

        private void SetupEventHandlers()
        {
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            dataGridView.ColumnHeaderMouseClick += DataGridView_ColumnHeaderMouseClick;
            dataGridView.DataError += DataGridView_DataError;
        }

        private void ApplyTheme()
        {
            this.BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary;
            this.ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary;
        }

        public void SetDataSource(DataTable dataTable)
        {
            originalDataSource = dataTable;
            filteredDataSource = dataTable?.Copy();
            totalRecords = dataTable?.Rows.Count ?? 0;
            currentPage = 1;
            
            ApplyFiltersAndPaging();
            UpdateUI();
        }

        private void ApplyFiltersAndPaging()
        {
            if (originalDataSource == null) return;

            // Apply filters
            filteredDataSource = originalDataSource.Copy();
            
            if (!string.IsNullOrEmpty(currentFilter))
            {
                var filteredRows = originalDataSource.AsEnumerable()
                    .Where(row => row.ItemArray.Any(field => 
                        field?.ToString().IndexOf(currentFilter, StringComparison.OrdinalIgnoreCase) >= 0));
                
                filteredDataSource = filteredRows.Any() 
                    ? filteredRows.CopyToDataTable() 
                    : originalDataSource.Clone();
            }

            totalRecords = filteredDataSource.Rows.Count;

            // Apply paging
            if (AllowPaging && totalRecords > pageSize)
            {
                var startIndex = (currentPage - 1) * pageSize;
                var endIndex = Math.Min(startIndex + pageSize - 1, totalRecords - 1);
                
                var pagedRows = filteredDataSource.AsEnumerable()
                    .Skip(startIndex)
                    .Take(pageSize);
                
                var pagedTable = pagedRows.Any() 
                    ? pagedRows.CopyToDataTable() 
                    : filteredDataSource.Clone();
                
                dataGridView.DataSource = pagedTable;
            }
            else
            {
                dataGridView.DataSource = filteredDataSource;
            }

            // Add line numbers if enabled
            if (ShowLineNumbers)
            {
                AddLineNumbers();
            }
        }

        private void AddLineNumbers()
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (!row.IsNewRow)
                {
                    var lineNumber = AllowPaging 
                        ? ((currentPage - 1) * pageSize) + row.Index + 1
                        : row.Index + 1;
                    row.HeaderCell.Value = lineNumber.ToString();
                }
            }
        }

        private void UpdateUI()
        {
            // Update status
            var recordText = totalRecords == 1 ? "record" : "records";
            statusLabel.Text = $"Showing {dataGridView.RowCount} of {totalRecords} {recordText}";
            
            recordCountLabel.Text = $"{totalRecords:N0} records";

            if (AllowPaging)
            {
                // Update pagination
                var totalPages = TotalPages;
                pageTextBox.Text = currentPage.ToString();
                pageInfoLabel.Text = $"of {totalPages}";
                
                firstPageButton.Enabled = currentPage > 1;
                previousPageButton.Enabled = currentPage > 1;
                nextPageButton.Enabled = currentPage < totalPages;
                lastPageButton.Enabled = currentPage < totalPages;
                
                paginationPanel.Visible = totalPages > 1;
            }
            else
            {
                paginationPanel.Visible = false;
            }
            
            filterPanel.Visible = AllowFiltering;
            exportButton.Enabled = AllowExport && totalRecords > 0;
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            ApplyFilter(searchTextBox.Text);
        }

        private void ClearFilterButton_Click(object sender, EventArgs e)
        {
            searchTextBox.Text = "";
            ApplyFilter("");
        }

        private void SearchTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                ApplyFilter(searchTextBox.Text);
                e.Handled = true;
            }
        }

        private void ApplyFilter(string filter)
        {
            currentFilter = filter;
            currentPage = 1;
            ApplyFiltersAndPaging();
            UpdateUI();
            
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(filter));
        }

        private void PageSizeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(pageSizeComboBox.SelectedItem?.ToString(), out var newPageSize))
            {
                pageSize = newPageSize;
                currentPage = 1;
                ApplyFiltersAndPaging();
                UpdateUI();
            }
        }

        private void FirstPage_Click(object sender, EventArgs e) => GoToPage(1);
        private void PreviousPage_Click(object sender, EventArgs e) => GoToPage(currentPage - 1);
        private void NextPage_Click(object sender, EventArgs e) => GoToPage(currentPage + 1);
        private void LastPage_Click(object sender, EventArgs e) => GoToPage(TotalPages);

        private void PageTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (int.TryParse(pageTextBox.Text, out var page))
                {
                    GoToPage(page);
                }
                e.Handled = true;
            }
        }

        private void GoToPage(int page)
        {
            var totalPages = TotalPages;
            if (page >= 1 && page <= totalPages && page != currentPage)
            {
                currentPage = page;
                ApplyFiltersAndPaging();
                UpdateUI();
                
                PageChanged?.Invoke(this, new PageChangedEventArgs(currentPage, totalPages));
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            DataRefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            DataExportRequested?.Invoke(this, EventArgs.Empty);
        }

        private void DataGridView_CellValueChanged(object sender, EventArgs e)
        {
            // Note: We can't get the specific cell info from the generic EventArgs
            // This would need to be handled differently if cell-specific info is needed
            // For now, just fire the event without specific cell details
        }

        private void DataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Custom sorting logic can be implemented here
        }

        private void DataGridView_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            // Handle numeric sorting
            if (double.TryParse(e.CellValue1?.ToString(), out var d1) && 
                double.TryParse(e.CellValue2?.ToString(), out var d2))
            {
                e.SortResult = d1.CompareTo(d2);
                e.Handled = true;
            }
        }

        private void DataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            LoggingService.LogWarning("Data grid error at [{Row}, {Column}]: {Message}", 
                e.RowIndex, e.ColumnIndex, e.Exception?.Message);
            e.ThrowException = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme();
        }
    }

    public class PageChangedEventArgs : EventArgs
    {
        public int CurrentPage { get; }
        public int TotalPages { get; }

        public PageChangedEventArgs(int currentPage, int totalPages)
        {
            CurrentPage = currentPage;
            TotalPages = totalPages;
        }
    }

    public class FilterChangedEventArgs : EventArgs
    {
        public string Filter { get; }

        public FilterChangedEventArgs(string filter)
        {
            Filter = filter;
        }
    }

    public class CellValueChangedEventArgs : EventArgs
    {
        public int RowIndex { get; }
        public int ColumnIndex { get; }
        public object NewValue { get; }

        public CellValueChangedEventArgs(int rowIndex, int columnIndex, object newValue)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            NewValue = newValue;
        }
    }
}
