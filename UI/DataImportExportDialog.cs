using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace SqlServerManager.UI
{
    public partial class DataImportExportDialog : Form
    {
        public enum OperationType
        {
            Import,
            Export
        }

        public enum DialogMode
        {
            DatabaseLevel,  // Import/Export to any table in database
            TableLevel,     // Import/Export to specific table
            Wizard          // Full wizard with guidance
        }

        #region Private Fields
        private SqlConnection connection;
        private string databaseName;
        private string schemaName;
        private string tableName;
        private OperationType operation;
        private DialogMode mode;
        
        // UI Controls
        private TabControl mainTabControl;
        private TabPage fileSettingsTab;
        private TabPage advancedOptionsTab;
        private TabPage previewTab;
        
        private ComboBox formatComboBox;
        private TextBox filePathTextBox;
        private Button browseButton;
        private CheckBox includeHeadersCheckBox;
        private ComboBox encodingComboBox;
        private ComboBox delimiterComboBox;
        private DataGridView previewGridView;
        private Button executeButton;
        private Button cancelButton;
        private ProgressBar progressBar;
        private Label statusLabel;
        private Panel optionsPanel;
        private ComboBox targetTableComboBox;
        private Label targetTableLabel;
        private TextBox tableSearchTextBox;
        private CheckBox truncateTableCheckBox;
        private CheckBox skipErrorsCheckBox;
        private NumericUpDown batchSizeNumeric;
        private CheckBox validateDataCheckBox;
        private Label rowCountLabel;
        private Label estimatedTimeLabel;
        #endregion

        #region Constructors
        // Main constructor with all parameters
        public DataImportExportDialog(SqlConnection connection, string databaseName, string schemaName, string tableName, OperationType operation, DialogMode mode = DialogMode.TableLevel)
        {
            Initialize(connection, databaseName, schemaName, tableName, operation, mode);
        }

        // Database-level constructor (for database context menu)
        public static DataImportExportDialog CreateDatabaseDialog(SqlConnection connection, string databaseName, OperationType operation)
        {
            return new DataImportExportDialog(connection, databaseName, null, null, operation, DialogMode.DatabaseLevel);
        }

        // Table-level constructor (for table context menu)
        public static DataImportExportDialog CreateTableDialog(SqlConnection connection, string databaseName, string schemaName, string tableName, OperationType operation)
        {
            return new DataImportExportDialog(connection, databaseName, schemaName, tableName, operation, DialogMode.TableLevel);
        }

        // Wizard constructor (for tools menu)
        public static DataImportExportDialog CreateWizard(SqlConnection connection, string databaseName, OperationType operation)
        {
            return new DataImportExportDialog(connection, databaseName, null, null, operation, DialogMode.Wizard);
        }

        private void Initialize(SqlConnection connection, string databaseName, string schemaName, string tableName, OperationType operation, DialogMode mode)
        {
            this.connection = connection;
            this.databaseName = databaseName;
            this.schemaName = schemaName;
            this.tableName = tableName;
            this.operation = operation;
            this.mode = mode;
            
            InitializeComponent();
            SetupForOperation();
        }
        #endregion

        private void InitializeComponent()
        {
            UpdateDialogTitle();
            this.Size = new Size(900, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(700, 500);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));

            // Create tabbed interface
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };

            // Create tabs
            CreateFileSettingsTab();
            CreateAdvancedOptionsTab();
            CreatePreviewTab();

            // Status and buttons panel
            var bottomPanel = new Panel
            {
                Height = 80,
                Dock = DockStyle.Fill
            };

            progressBar = new ProgressBar
            {
                Location = new Point(10, 10),
                Size = new Size(350, 20),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            statusLabel = new Label
            {
                Location = new Point(10, 35),
                Size = new Size(500, 20),
                Text = "Ready",
                Font = new Font("Segoe UI", 9)
            };

            rowCountLabel = new Label
            {
                Location = new Point(10, 55),
                Size = new Size(200, 15),
                Text = "",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            executeButton = new Button
            {
                Text = operation == OperationType.Import ? "Import Data" : "Export Data",
                Size = new Size(120, 35),
                Location = new Point(600, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            executeButton.Click += ExecuteButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 35),
                Location = new Point(730, 25),
                DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 9)
            };

            bottomPanel.Controls.AddRange(new Control[] 
            { 
                progressBar, 
                statusLabel,
                rowCountLabel,
                executeButton, 
                cancelButton 
            });

            mainLayout.Controls.Add(mainTabControl, 0, 0);
            mainLayout.Controls.Add(bottomPanel, 0, 1);
            this.Controls.Add(mainLayout);
        }

        private void UpdateDialogTitle()
        {
            var modeText = mode switch
            {
                DialogMode.Wizard => "Wizard",
                DialogMode.DatabaseLevel => "Database",
                DialogMode.TableLevel => "Table",
                _ => ""
            };
            
            var tableText = !string.IsNullOrEmpty(schemaName) && !string.IsNullOrEmpty(tableName) 
                ? $" - {schemaName}.{tableName}"
                : !string.IsNullOrEmpty(databaseName) ? $" - {databaseName}" : "";
                
            this.Text = $"{operation} Data {modeText}{tableText}";
        }

        private void CreateFileSettingsTab()
        {
            fileSettingsTab = new TabPage("File & Format")
            {
                UseVisualStyleBackColor = true
            };

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            
            // File Format Section
            var formatGroup = new GroupBox
            {
                Text = "File Format",
                Height = 120,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(10)
            };

            var formatLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(10)
            };
            formatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            formatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            formatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            var formatLabel = new Label { Text = "Format:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9) };
            formatComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };
            formatComboBox.Items.AddRange(new[] { "CSV", "Excel (.xlsx)", "JSON", "XML", "SQL Script" });
            formatComboBox.SelectedIndex = 0;
            formatComboBox.SelectedIndexChanged += FormatComboBox_SelectedIndexChanged;

            var encodingLabel = new Label { Text = "Encoding:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9) };
            encodingComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };
            encodingComboBox.Items.AddRange(new[] { "UTF-8", "UTF-16", "ASCII", "Windows-1252" });
            encodingComboBox.SelectedIndex = 0;

            formatLayout.Controls.Add(formatLabel, 0, 0);
            formatLayout.Controls.Add(formatComboBox, 1, 0);
            formatLayout.Controls.Add(encodingLabel, 0, 1);
            formatLayout.Controls.Add(encodingComboBox, 1, 1);
            formatGroup.Controls.Add(formatLayout);

            // File Path Section
            var fileGroup = new GroupBox
            {
                Text = "File Location",
                Height = 80,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(10)
            };

            var fileLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10)
            };
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            var fileLabel = new Label { Text = "File Path:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9) };
            filePathTextBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };
            browseButton = new Button 
            { 
                Text = "Browse...", 
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };
            browseButton.Click += BrowseButton_Click;

            fileLayout.Controls.Add(fileLabel, 0, 0);
            fileLayout.Controls.Add(filePathTextBox, 1, 0);
            fileLayout.Controls.Add(browseButton, 2, 0);
            fileGroup.Controls.Add(fileLayout);

            // Target Table Section
            var tableGroup = new GroupBox
            {
                Text = operation == OperationType.Import ? "Target Table" : "Source Table",
                Height = 120,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(10)
            };

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            targetTableLabel = new Label { Text = "Select Table:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9) };
            
            var tableSelectionPanel = new Panel { Dock = DockStyle.Fill };
            targetTableComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9),
                Height = 25
            };
            targetTableComboBox.SelectedIndexChanged += TargetTableComboBox_SelectedIndexChanged;
            
            tableSearchTextBox = new TextBox
            {
                PlaceholderText = "Search tables...",
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI", 8),
                Height = 22
            };
            tableSearchTextBox.TextChanged += TableSearchTextBox_TextChanged;
            
            tableSelectionPanel.Controls.Add(targetTableComboBox);
            tableSelectionPanel.Controls.Add(tableSearchTextBox);

            tableLayout.Controls.Add(targetTableLabel, 0, 0);
            tableLayout.Controls.Add(tableSelectionPanel, 1, 0);
            tableGroup.Controls.Add(tableLayout);

            mainPanel.Controls.Add(tableGroup);
            mainPanel.Controls.Add(new Splitter { Dock = DockStyle.Top, Height = 5 });
            mainPanel.Controls.Add(fileGroup);
            mainPanel.Controls.Add(new Splitter { Dock = DockStyle.Top, Height = 5 });
            mainPanel.Controls.Add(formatGroup);
            
            fileSettingsTab.Controls.Add(mainPanel);
            mainTabControl.TabPages.Add(fileSettingsTab);
        }

        private void CreateAdvancedOptionsTab()
        {
            advancedOptionsTab = new TabPage("Advanced Options")
            {
                UseVisualStyleBackColor = true
            };

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            
            // CSV-specific options
            var csvGroup = new GroupBox
            {
                Text = "CSV Options",
                Height = 120,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(10)
            };

            var csvLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };
            csvLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            csvLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var delimiterLabel = new Label { Text = "Delimiter:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9) };
            delimiterComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };
            delimiterComboBox.Items.AddRange(new[] { ",", ";", "\\t", "|" });
            delimiterComboBox.SelectedIndex = 0;

            includeHeadersCheckBox = new CheckBox
            {
                Text = "Include Headers",
                Checked = true,
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };

            csvLayout.Controls.Add(delimiterLabel, 0, 0);
            csvLayout.Controls.Add(delimiterComboBox, 1, 0);
            csvLayout.Controls.Add(includeHeadersCheckBox, 0, 1);
            csvGroup.Controls.Add(csvLayout);

            // Import-specific options
            if (operation == OperationType.Import)
            {
                var importGroup = new GroupBox
                {
                    Text = "Import Options",
                    Height = 150,
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Padding = new Padding(10)
                };

                var importLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 4,
                    Padding = new Padding(10)
                };
                importLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                importLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                truncateTableCheckBox = new CheckBox
                {
                    Text = "Truncate table before import",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9)
                };

                skipErrorsCheckBox = new CheckBox
                {
                    Text = "Skip rows with errors",
                    Checked = true,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9)
                };

                validateDataCheckBox = new CheckBox
                {
                    Text = "Validate data before import",
                    Checked = true,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9)
                };

                var batchLabel = new Label { Text = "Batch Size:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 9) };
                batchSizeNumeric = new NumericUpDown
                {
                    Minimum = 100,
                    Maximum = 10000,
                    Value = 1000,
                    Increment = 100,
                    Width = 100,
                    Font = new Font("Segoe UI", 9)
                };

                importLayout.Controls.Add(truncateTableCheckBox, 0, 0);
                importLayout.Controls.Add(skipErrorsCheckBox, 0, 1);
                importLayout.Controls.Add(validateDataCheckBox, 0, 2);
                importLayout.Controls.Add(batchLabel, 0, 3);
                importLayout.Controls.Add(batchSizeNumeric, 1, 3);
                importGroup.Controls.Add(importLayout);
                
                mainPanel.Controls.Add(importGroup);
                mainPanel.Controls.Add(new Splitter { Dock = DockStyle.Top, Height = 5 });
            }

            mainPanel.Controls.Add(csvGroup);
            advancedOptionsTab.Controls.Add(mainPanel);
            mainTabControl.TabPages.Add(advancedOptionsTab);
        }

        private void CreatePreviewTab()
        {
            previewTab = new TabPage("Preview")
            {
                UseVisualStyleBackColor = true
            };

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Info panel
            var infoPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top
            };

            var infoLabel = new Label
            {
                Text = operation == OperationType.Import 
                    ? "Preview of data to be imported (showing first 100 rows):"
                    : "Preview of table data to be exported (showing first 100 rows):",
                Location = new Point(5, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };

            rowCountLabel = new Label
            {
                Location = new Point(5, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            estimatedTimeLabel = new Label
            {
                Location = new Point(5, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            infoPanel.Controls.AddRange(new Control[] { infoLabel, rowCountLabel, estimatedTimeLabel });

            // Preview grid
            previewGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                Font = new Font("Segoe UI", 8)
            };

            mainPanel.Controls.Add(previewGridView);
            mainPanel.Controls.Add(infoPanel);
            
            previewTab.Controls.Add(mainPanel);
            mainTabControl.TabPages.Add(previewTab);
        }

        private void TableSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            FilterTableList();
        }

        private void FilterTableList()
        {
            if (targetTableComboBox == null || tableSearchTextBox == null)
                return;

            var searchText = tableSearchTextBox.Text.ToLower();
            var selectedItem = targetTableComboBox.SelectedItem?.ToString();
            
            targetTableComboBox.Items.Clear();

            var allTables = GetAllAvailableTables();
            var filteredTables = string.IsNullOrWhiteSpace(searchText) 
                ? allTables 
                : allTables.Where(t => t.ToLower().Contains(searchText)).ToList();

            foreach (var table in filteredTables)
            {
                targetTableComboBox.Items.Add(table);
            }

            // Try to restore selection
            if (!string.IsNullOrEmpty(selectedItem) && targetTableComboBox.Items.Contains(selectedItem))
            {
                targetTableComboBox.SelectedItem = selectedItem;
            }
            else if (targetTableComboBox.Items.Count > 0)
            {
                targetTableComboBox.SelectedIndex = 0;
            }
        }

        private List<string> allTablesList = new List<string>();

        private List<string> GetAllAvailableTables()
        {
            return allTablesList;
        }

        private void SetupForOperation()
        {
            LoadAvailableTables();
            UpdateDialogTitle();
            
            // Set default tab based on mode
            if (mode == DialogMode.Wizard)
            {
                mainTabControl.SelectedTab = fileSettingsTab;
            }
        }

        private void FormatComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedFormat = formatComboBox.SelectedItem.ToString();
            
            // Show/hide delimiter options based on format
            delimiterComboBox.Enabled = selectedFormat == "CSV";
            
            // Update file extension in path if there's already a path
            if (!string.IsNullOrEmpty(filePathTextBox.Text))
            {
                var directory = Path.GetDirectoryName(filePathTextBox.Text);
                var fileName = Path.GetFileNameWithoutExtension(filePathTextBox.Text);
                
                var extension = GetFileExtension(selectedFormat);
                filePathTextBox.Text = Path.Combine(directory, fileName + extension);
            }
        }

        private string GetFileExtension(string format)
        {
            return format switch
            {
                "CSV" => ".csv",
                "Excel (.xlsx)" => ".xlsx",
                "JSON" => ".json",
                "XML" => ".xml",
                "SQL Script" => ".sql",
                _ => ".txt"
            };
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            var selectedFormat = formatComboBox.SelectedItem.ToString();
            var extension = GetFileExtension(selectedFormat);
            var filter = GetFileFilter(selectedFormat);

            if (operation == OperationType.Export)
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = filter;
                    saveDialog.Title = "Select export location";
                    saveDialog.FileName = $"{schemaName}_{tableName}{extension}";
                    
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        filePathTextBox.Text = saveDialog.FileName;
                    }
                }
            }
            else // Import operation
            {
                using (var openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = filter;
                    openDialog.Title = "Select file to import";
                    
                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        filePathTextBox.Text = openDialog.FileName;
                        LoadFilePreview(openDialog.FileName);
                    }
                }
            }
        }

        private string GetFileFilter(string format)
        {
            return format switch
            {
                "CSV" => "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                "Excel (.xlsx)" => "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                "JSON" => "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                "XML" => "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                "SQL Script" => "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                _ => "All Files (*.*)|*.*"
            };
        }

        private void LoadTableDataPreview()
        {
            try
            {
                var query = $"SELECT TOP 100 * FROM [{schemaName}].[{tableName}]";
                using (var command = new SqlCommand(query, connection))
                {
                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    
                    previewGridView.DataSource = dataTable;
                    statusLabel.Text = $"Loaded {dataTable.Rows.Count} rows for preview";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error loading preview: {ex.Message}";
            }
        }


        private Encoding GetSelectedEncoding()
        {
            return encodingComboBox.SelectedItem.ToString() switch
            {
                "UTF-8" => Encoding.UTF8,
                "UTF-16" => Encoding.Unicode,
                "ASCII" => Encoding.ASCII,
                "Windows-1252" => Encoding.GetEncoding("Windows-1252"),
                _ => Encoding.UTF8
            };
        }

        private char GetSelectedDelimiter()
        {
            return delimiterComboBox.SelectedItem.ToString() switch
            {
                "," => ',',
                ";" => ';',
                "\\t" => '\t',
                "|" => '|',
                _ => ','
            };
        }




        private void LoadFilePreview(string filePath)
        {
            try
            {
                var selectedFormat = formatComboBox.SelectedItem.ToString();
                DataTable previewData = null;
                
                switch (selectedFormat)
                {
                    case "CSV":
                        previewData = LoadCsvPreview(filePath);
                        break;
                    case "Excel (.xlsx)":
                        previewData = LoadExcelPreview(filePath);
                        break;
                    case "JSON":
                        previewData = LoadJsonPreview(filePath);
                        break;
                    case "XML":
                        previewData = LoadXmlPreview(filePath);
                        break;
                    default:
                        statusLabel.Text = $"Preview not supported for {selectedFormat} format";
                        return;
                }
                
                if (previewData != null)
                {
                    previewGridView.DataSource = previewData;
                    statusLabel.Text = $"File preview loaded: {previewData.Rows.Count} rows";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error loading file preview: {ex.Message}";
                MessageBox.Show($"Error loading file preview: {ex.Message}", "Preview Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private DataTable LoadCsvPreview(string filePath)
        {
            var dataTable = new DataTable();
            var encoding = GetSelectedEncoding();
            var delimiter = GetSelectedDelimiter();
            
            using (var reader = new StreamReader(filePath, encoding))
            {
                var firstLine = true;
                var rowCount = 0;
                
                while (!reader.EndOfStream && rowCount < 100) // Limit preview to 100 rows
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var values = line.Split(delimiter);
                    
                    if (firstLine)
                    {
                        // Create columns
                        for (int i = 0; i < values.Length; i++)
                        {
                            var columnName = includeHeadersCheckBox.Checked ? values[i].Trim('"') : $"Column{i + 1}";
                            dataTable.Columns.Add(columnName);
                        }
                        
                        firstLine = false;
                        if (includeHeadersCheckBox.Checked) continue; // Skip header row for data
                    }
                    
                    // Add data row
                    var dataRow = dataTable.NewRow();
                    for (int i = 0; i < Math.Min(values.Length, dataTable.Columns.Count); i++)
                    {
                        dataRow[i] = values[i].Trim('"');
                    }
                    dataTable.Rows.Add(dataRow);
                    rowCount++;
                }
            }
            
            return dataTable;
        }
        
        private DataTable LoadExcelPreview(string filePath)
        {
            var dataTable = new DataTable();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // First worksheet
                var startRow = includeHeadersCheckBox.Checked ? 1 : 0;
                var endRow = Math.Min(worksheet.Dimension?.End.Row ?? 0, startRow + 100); // Limit to 100 rows
                var endCol = worksheet.Dimension?.End.Column ?? 0;
                
                // Create columns
                for (int col = 1; col <= endCol; col++)
                {
                    var columnName = includeHeadersCheckBox.Checked ? 
                        worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}" :
                        $"Column{col}";
                    dataTable.Columns.Add(columnName);
                }
                
                // Add data rows
                var dataStartRow = includeHeadersCheckBox.Checked ? 2 : 1;
                for (int row = dataStartRow; row <= endRow; row++)
                {
                    var dataRow = dataTable.NewRow();
                    for (int col = 1; col <= endCol; col++)
                    {
                        dataRow[col - 1] = worksheet.Cells[row, col].Value?.ToString() ?? "";
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }
            
            return dataTable;
        }
        
        private DataTable LoadJsonPreview(string filePath)
        {
            var dataTable = new DataTable();
            var encoding = GetSelectedEncoding();
            var jsonText = File.ReadAllText(filePath, encoding);
            
            var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonText);
            
            if (jsonArray != null && jsonArray.Count > 0)
            {
                // Create columns from first object
                foreach (var key in jsonArray[0].Keys)
                {
                    dataTable.Columns.Add(key);
                }
                
                // Add data rows (limit to 100 for preview)
                var rowCount = Math.Min(jsonArray.Count, 100);
                for (int i = 0; i < rowCount; i++)
                {
                    var dataRow = dataTable.NewRow();
                    foreach (var key in jsonArray[i].Keys)
                    {
                        dataRow[key] = jsonArray[i][key]?.ToString() ?? "";
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }
            
            return dataTable;
        }
        
        private DataTable LoadXmlPreview(string filePath)
        {
            var dataTable = new DataTable();
            var doc = new XmlDocument();
            doc.Load(filePath);
            
            var rootNode = doc.DocumentElement;
            if (rootNode?.FirstChild != null)
            {
                // Create columns from first data row
                foreach (XmlNode childNode in rootNode.FirstChild.ChildNodes)
                {
                    dataTable.Columns.Add(childNode.Name);
                }
                
                // Add data rows (limit to 100 for preview)
                var rowCount = 0;
                foreach (XmlNode rowNode in rootNode.ChildNodes)
                {
                    if (rowCount >= 100) break;
                    
                    var dataRow = dataTable.NewRow();
                    foreach (XmlNode cellNode in rowNode.ChildNodes)
                    {
                        if (dataTable.Columns.Contains(cellNode.Name))
                        {
                            dataRow[cellNode.Name] = cellNode.InnerText;
                        }
                    }
                    dataTable.Rows.Add(dataRow);
                    rowCount++;
                }
            }
            
            return dataTable;
        }
        
        private async Task ImportData()
        {
            var selectedFormat = formatComboBox.SelectedItem.ToString();
            statusLabel.Text = "Loading file data...";
            
            DataTable importData = null;
            
            switch (selectedFormat)
            {
                case "CSV":
                    importData = await LoadFullCsvData();
                    break;
                case "Excel (.xlsx)":
                    importData = await LoadFullExcelData();
                    break;
                case "JSON":
                    importData = await LoadFullJsonData();
                    break;
                case "XML":
                    importData = await LoadFullXmlData();
                    break;
                default:
                    throw new NotSupportedException($"Import not supported for {selectedFormat} format");
            }
            
            if (importData == null || importData.Rows.Count == 0)
            {
                statusLabel.Text = "No data found to import";
                return;
            }
            
            statusLabel.Text = $"Importing {importData.Rows.Count} rows...";
            
            // Bulk insert the data
            await BulkInsertData(importData);
            
            statusLabel.Text = $"Import completed: {importData.Rows.Count} rows imported";
        }
        
        private async Task<DataTable> LoadFullCsvData()
        {
            return await Task.Run(() =>
            {
                var dataTable = new DataTable();
                var encoding = GetSelectedEncoding();
                var delimiter = GetSelectedDelimiter();
                
                using (var reader = new StreamReader(filePathTextBox.Text, encoding))
                {
                    var firstLine = true;
                    
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var values = line.Split(delimiter);
                        
                        if (firstLine)
                        {
                            // Create columns
                            for (int i = 0; i < values.Length; i++)
                            {
                                var columnName = includeHeadersCheckBox.Checked ? values[i].Trim('"') : $"Column{i + 1}";
                                dataTable.Columns.Add(columnName);
                            }
                            
                            firstLine = false;
                            if (includeHeadersCheckBox.Checked) continue; // Skip header row for data
                        }
                        
                        // Add data row
                        var dataRow = dataTable.NewRow();
                        for (int i = 0; i < Math.Min(values.Length, dataTable.Columns.Count); i++)
                        {
                            dataRow[i] = values[i].Trim('"');
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }
                
                return dataTable;
            });
        }
        
        private async Task<DataTable> LoadFullExcelData()
        {
            return await Task.Run(() =>
            {
                var dataTable = new DataTable();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using (var package = new ExcelPackage(new FileInfo(filePathTextBox.Text)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // First worksheet
                    var endRow = worksheet.Dimension?.End.Row ?? 0;
                    var endCol = worksheet.Dimension?.End.Column ?? 0;
                    
                    // Create columns
                    for (int col = 1; col <= endCol; col++)
                    {
                        var columnName = includeHeadersCheckBox.Checked ? 
                            worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}" :
                            $"Column{col}";
                        dataTable.Columns.Add(columnName);
                    }
                    
                    // Add data rows
                    var dataStartRow = includeHeadersCheckBox.Checked ? 2 : 1;
                    for (int row = dataStartRow; row <= endRow; row++)
                    {
                        var dataRow = dataTable.NewRow();
                        for (int col = 1; col <= endCol; col++)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Value?.ToString() ?? "";
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }
                
                return dataTable;
            });
        }
        
        private async Task<DataTable> LoadFullJsonData()
        {
            return await Task.Run(() =>
            {
                var dataTable = new DataTable();
                var encoding = GetSelectedEncoding();
                var jsonText = File.ReadAllText(filePathTextBox.Text, encoding);
                
                var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonText);
                
                if (jsonArray != null && jsonArray.Count > 0)
                {
                    // Create columns from first object
                    foreach (var key in jsonArray[0].Keys)
                    {
                        dataTable.Columns.Add(key);
                    }
                    
                    // Add all data rows
                    foreach (var jsonObject in jsonArray)
                    {
                        var dataRow = dataTable.NewRow();
                        foreach (var key in jsonObject.Keys)
                        {
                            dataRow[key] = jsonObject[key]?.ToString() ?? "";
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }
                
                return dataTable;
            });
        }
        
        private async Task<DataTable> LoadFullXmlData()
        {
            return await Task.Run(() =>
            {
                var dataTable = new DataTable();
                var doc = new XmlDocument();
                doc.Load(filePathTextBox.Text);
                
                var rootNode = doc.DocumentElement;
                if (rootNode?.FirstChild != null)
                {
                    // Create columns from first data row
                    foreach (XmlNode childNode in rootNode.FirstChild.ChildNodes)
                    {
                        dataTable.Columns.Add(childNode.Name);
                    }
                    
                    // Add all data rows
                    foreach (XmlNode rowNode in rootNode.ChildNodes)
                    {
                        var dataRow = dataTable.NewRow();
                        foreach (XmlNode cellNode in rowNode.ChildNodes)
                        {
                            if (dataTable.Columns.Contains(cellNode.Name))
                            {
                                dataRow[cellNode.Name] = cellNode.InnerText;
                            }
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }
                
                return dataTable;
            });
        }
        
        private async Task BulkInsertData(DataTable sourceData)
        {
            // Get target table columns with nullability info
            var targetColumns = await GetTableColumns();
            var missingRequiredColumns = new List<string>();
            
            // Create a compatible DataTable for bulk insert
            var targetDataTable = new DataTable();
            foreach (var column in targetColumns)
            {
                targetDataTable.Columns.Add(column.Name, column.ClrType);
            }
            
            // Map and convert data
            foreach (DataRow sourceRow in sourceData.Rows)
            {
                var targetRow = targetDataTable.NewRow();
                
                foreach (var targetColumn in targetColumns)
                {
                    var columnName = targetColumn.Name;
                    
                    // Try to find matching column in source data (case-insensitive)
                    var sourceColumn = sourceData.Columns.Cast<DataColumn>()
                        .FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
                    
                    if (sourceColumn != null)
                    {
                        var sourceValue = sourceRow[sourceColumn];
                        targetRow[columnName] = ConvertValue(sourceValue, targetColumn.ClrType, targetColumn.IsNullable, columnName);
                    }
                    else
                    {
                        // Column not found in source data
                        if (!targetColumn.IsNullable)
                        {
                            // Non-nullable column missing from source - provide default value
                            var defaultValue = GetDefaultValueForType(targetColumn.ClrType);
                            if (defaultValue == null)
                            {
                                missingRequiredColumns.Add(columnName);
                                targetRow[columnName] = DBNull.Value; // This will cause constraint violation
                            }
                            else
                            {
                                targetRow[columnName] = defaultValue;
                            }
                        }
                        else
                        {
                            // Nullable column - set to null
                            targetRow[columnName] = DBNull.Value;
                        }
                    }
                }
                
                targetDataTable.Rows.Add(targetRow);
            }
            
            // Report missing required columns
            if (missingRequiredColumns.Count > 0)
            {
                var message = $"The following non-nullable columns are missing from the source data and cannot be populated with default values:\n{string.Join(", ", missingRequiredColumns)}\n\nThe import may fail due to constraint violations.";
                if (!skipErrorsCheckBox.Checked)
                {
                    throw new InvalidOperationException(message);
                }
                else
                {
                    statusLabel.Text = $"Warning: Missing required columns - {missingRequiredColumns.Count} columns";
                }
            }
            
            // Use SqlBulkCopy for efficient insert
            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = $"[{schemaName}].[{tableName}]";
                bulkCopy.BatchSize = (int)batchSizeNumeric.Value;
                bulkCopy.BulkCopyTimeout = 300; // 5 minutes
                
                // Map columns
                foreach (var column in targetColumns)
                {
                    bulkCopy.ColumnMappings.Add(column.Name, column.Name);
                }
                
                await bulkCopy.WriteToServerAsync(targetDataTable);
            }
        }
        
        private class ColumnInfo
        {
            public string Name { get; set; }
            public string SqlType { get; set; }
            public Type ClrType { get; set; }
            public bool IsNullable { get; set; }
        }

        private async Task<List<ColumnInfo>> GetTableColumns()
        {
            var columns = new List<ColumnInfo>();
            
            var query = @"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
                ORDER BY c.ORDINAL_POSITION";
            
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@schema", schemaName);
                command.Parameters.AddWithValue("@table", tableName);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader["COLUMN_NAME"].ToString();
                        var dataType = reader["DATA_TYPE"].ToString().ToLower();
                        var isNullable = reader["IS_NULLABLE"].ToString().Equals("YES", StringComparison.OrdinalIgnoreCase);
                        
                        var clrType = MapSqlTypeToClrType(dataType);
                        columns.Add(new ColumnInfo
                        {
                            Name = columnName,
                            SqlType = dataType,
                            ClrType = clrType,
                            IsNullable = isNullable
                        });
                    }
                }
            }
            
            return columns;
        }
        
        private Type MapSqlTypeToClrType(string sqlType)
        {
            return sqlType.ToLower() switch
            {
                "int" or "integer" => typeof(int),
                "bigint" => typeof(long),
                "smallint" => typeof(short),
                "tinyint" => typeof(byte),
                "bit" => typeof(bool),
                "decimal" or "numeric" or "money" or "smallmoney" => typeof(decimal),
                "float" or "real" => typeof(double),
                "datetime" or "datetime2" or "smalldatetime" or "date" or "time" => typeof(DateTime),
                "uniqueidentifier" => typeof(Guid),
                _ => typeof(string) // Default to string for text types
            };
        }
        
        private object ConvertValue(object sourceValue, Type targetType, bool isNullable, string columnName)
        {
            if (sourceValue == null || sourceValue == DBNull.Value)
            {
                if (isNullable)
                {
                    return DBNull.Value;
                }
                else
                {
                    // Non-nullable column with null value - provide default
                    var defaultValue = GetDefaultValueForType(targetType);
                    return defaultValue ?? DBNull.Value;
                }
            }
            
            var sourceString = sourceValue.ToString();
            if (string.IsNullOrWhiteSpace(sourceString))
            {
                if (isNullable)
                {
                    return DBNull.Value;
                }
                else
                {
                    // Non-nullable column with empty value - provide default
                    var defaultValue = GetDefaultValueForType(targetType);
                    return defaultValue ?? DBNull.Value;
                }
            }
            
            try
            {
                if (targetType == typeof(int))
                    return int.Parse(sourceString);
                else if (targetType == typeof(long))
                    return long.Parse(sourceString);
                else if (targetType == typeof(short))
                    return short.Parse(sourceString);
                else if (targetType == typeof(byte))
                    return byte.Parse(sourceString);
                else if (targetType == typeof(bool))
                {
                    if (sourceString.Equals("true", StringComparison.OrdinalIgnoreCase) || sourceString == "1")
                        return true;
                    if (sourceString.Equals("false", StringComparison.OrdinalIgnoreCase) || sourceString == "0")
                        return false;
                    return bool.Parse(sourceString);
                }
                else if (targetType == typeof(decimal))
                    return decimal.Parse(sourceString);
                else if (targetType == typeof(double))
                    return double.Parse(sourceString);
                else if (targetType == typeof(DateTime))
                    return DateTime.Parse(sourceString);
                else if (targetType == typeof(Guid))
                    return Guid.Parse(sourceString);
                else
                    return sourceString; // String type
            }
            catch
            {
                // Conversion failed - use appropriate fallback based on nullability
                if (isNullable)
                {
                    return DBNull.Value;
                }
                else
                {
                    var defaultValue = GetDefaultValueForType(targetType);
                    if (defaultValue != null)
                    {
                        return defaultValue;
                    }
                    else
                    {
                        // Log the conversion error for debugging
                        statusLabel.Text = $"Warning: Failed to convert '{sourceString}' to {targetType.Name} for column '{columnName}'";
                        return DBNull.Value; // This will likely cause constraint violation
                    }
                }
            }
        }
        
        private object GetDefaultValueForType(Type targetType)
        {
            // Return appropriate default values for non-nullable types
            if (targetType == typeof(int) || targetType == typeof(long) || targetType == typeof(short) || targetType == typeof(byte))
            {
                return 0;
            }
            else if (targetType == typeof(bool))
            {
                return false;
            }
            else if (targetType == typeof(decimal))
            {
                return 0.0m;
            }
            else if (targetType == typeof(double))
            {
                return 0.0d;
            }
            else if (targetType == typeof(DateTime))
            {
                return DateTime.Now; // Use current date for non-nullable DateTime columns
            }
            else if (targetType == typeof(Guid))
            {
                return Guid.NewGuid(); // Generate a new GUID instead of Empty
            }
            else if (targetType == typeof(string))
            {
                return ""; // Empty string for non-nullable string columns
            }
            else
            {
                return null; // No suitable default available
            }
        }
        
        private async Task ExportData()
        {
            var selectedFormat = formatComboBox.SelectedItem.ToString();

            statusLabel.Text = "Loading table data...";

            // Get table data
            var query = $"SELECT * FROM [{schemaName}].[{tableName}]";
            var dataTable = new DataTable();

            using (var command = new SqlCommand(query, connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(dataTable);
            }

            statusLabel.Text = $"Exporting {dataTable.Rows.Count} rows...";

            switch (selectedFormat)
            {
                case "CSV":
                    await ExportToCsv(dataTable);
                    break;
                case "Excel (.xlsx)":
                    await ExportToExcel(dataTable);
                    break;
                case "JSON":
                    await ExportToJson(dataTable);
                    break;
                case "XML":
                    await ExportToXml(dataTable);
                    break;
                case "SQL Script":
                    await ExportToSql(dataTable);
                    break;
            }

            statusLabel.Text = $"Export completed: {dataTable.Rows.Count} rows exported";
        }

        private async Task ExportToCsv(DataTable dataTable)
        {
            await Task.Run(() =>
            {
                var encoding = GetSelectedEncoding();
                var delimiter = GetSelectedDelimiter();

                using (var writer = new StreamWriter(filePathTextBox.Text, false, encoding))
                {
                    // Write headers if requested
                    if (includeHeadersCheckBox.Checked)
                    {
                        var headers = dataTable.Columns.Cast<DataColumn>()
                            .Select(c => $"\"{c.ColumnName}\"");
                        writer.WriteLine(string.Join(delimiter, headers));
                    }

                    // Write data rows
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var values = row.ItemArray.Select(field => $"\"{field}\"");
                        writer.WriteLine(string.Join(delimiter, values));
                    }
                }
            });
        }

        private async Task ExportToExcel(DataTable dataTable)
        {
            await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Data");
                    
                    var startRow = 1;

                    // Write headers if requested
                    if (includeHeadersCheckBox.Checked)
                    {
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            worksheet.Cells[1, col + 1].Value = dataTable.Columns[col].ColumnName;
                        }
                        startRow = 2;
                    }

                    // Write data
                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            worksheet.Cells[startRow + row, col + 1].Value = dataTable.Rows[row][col];
                        }
                    }

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    package.SaveAs(new FileInfo(filePathTextBox.Text));
                }
            });
        }

        private async Task ExportToJson(DataTable dataTable)
        {
            await Task.Run(() =>
            {
                var encoding = GetSelectedEncoding();
                var jsonArray = new List<Dictionary<string, object>>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var jsonObject = new Dictionary<string, object>();
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        jsonObject[dataTable.Columns[i].ColumnName] = row[i] ?? "";
                    }
                    jsonArray.Add(jsonObject);
                }

                var jsonString = JsonConvert.SerializeObject(jsonArray, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePathTextBox.Text, jsonString, encoding);
            });
        }

        private async Task ExportToXml(DataTable dataTable)
        {
            await Task.Run(() =>
            {
                var encoding = GetSelectedEncoding();
                
                using (var writer = new StreamWriter(filePathTextBox.Text, false, encoding))
                using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
                {
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteStartElement("data");

                    foreach (DataRow row in dataTable.Rows)
                    {
                        xmlWriter.WriteStartElement("row");
                        
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            xmlWriter.WriteElementString(dataTable.Columns[i].ColumnName, row[i]?.ToString() ?? "");
                        }
                        
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                }
            });
        }

        private async Task ExportToSql(DataTable dataTable)
        {
            await Task.Run(async () =>
            {
                var encoding = GetSelectedEncoding();
                var identityColumns = await GetIdentityColumns();
                
                using (var writer = new StreamWriter(filePathTextBox.Text, false, encoding))
                {
                    writer.WriteLine($"-- Data export for {schemaName}.{tableName}");
                    writer.WriteLine($"-- Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();

                    if (dataTable.Rows.Count > 0)
                    {
                        var columnNames = dataTable.Columns.Cast<DataColumn>()
                            .Select(c => $"[{c.ColumnName}]").ToArray();

                        // Add IDENTITY_INSERT ON if table has identity columns
                        if (identityColumns.Count > 0)
                        {
                            writer.WriteLine($"SET IDENTITY_INSERT [{schemaName}].[{tableName}] ON;");
                            writer.WriteLine();
                        }

                        writer.WriteLine($"INSERT INTO [{schemaName}].[{tableName}]");
                        writer.WriteLine($"({string.Join(", ", columnNames)})");
                        writer.WriteLine("VALUES");

                        var valuesList = new List<string>();

                        foreach (DataRow row in dataTable.Rows)
                        {
                            var values = new List<string>();
                            foreach (var item in row.ItemArray)
                            {
                                if (item == DBNull.Value)
                                    values.Add("NULL");
                                else if (item is string || item is DateTime)
                                    values.Add($"'{item.ToString().Replace("'", "''")}'");
                                else if (item is bool)
                                    values.Add(((bool)item) ? "1" : "0");
                                else
                                    values.Add(item.ToString());
                            }
                            valuesList.Add($"({string.Join(", ", values)})");
                        }

                        writer.WriteLine(string.Join(",\n", valuesList));
                        writer.WriteLine(";");
                        
                        // Add IDENTITY_INSERT OFF if it was enabled
                        if (identityColumns.Count > 0)
                        {
                            writer.WriteLine();
                            writer.WriteLine($"SET IDENTITY_INSERT [{schemaName}].[{tableName}] OFF;");
                        }
                    }
                }
            });
        }
        
        private async Task<List<string>> GetIdentityColumns()
        {
            var identityColumns = new List<string>();
            
            try
            {
                var query = $@"
                    SELECT c.name AS ColumnName
                    FROM sys.columns c
                    INNER JOIN sys.tables t ON c.object_id = t.object_id
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE
                        t.name = @tableName AND
                        s.name = @schemaName AND
                        c.is_identity = 1";
                        
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    command.Parameters.AddWithValue("@schemaName", schemaName);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            identityColumns.Add(reader["ColumnName"].ToString());
                        }
                    }
                }
                
                statusLabel.Text = $"Found {identityColumns.Count} identity columns";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error checking identity columns: {ex.Message}";
            }
            
            return identityColumns;
        }
        
        private async void LoadAvailableTables()
        {
            if (targetTableComboBox == null)
                return;
                
            try
            {
                allTablesList.Clear();
                targetTableComboBox.Items.Clear();
                
                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        s.name AS SchemaName,
                        t.name AS TableName
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE t.is_ms_shipped = 0
                    ORDER BY s.name, t.name";
                
                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var schema = reader["SchemaName"].ToString();
                        var table = reader["TableName"].ToString();
                        var tableName = $"{schema}.{table}";
                        allTablesList.Add(tableName);
                        targetTableComboBox.Items.Add(tableName);
                    }
                }
                
                statusLabel.Text = $"Loaded {targetTableComboBox.Items.Count} available tables";
                
                if (targetTableComboBox.Items.Count > 0)
                {
                    // If a table was pre-selected, try to find and select it
                    if (!string.IsNullOrEmpty(schemaName) && !string.IsNullOrEmpty(tableName))
                    {
                        var preselectedTable = $"{schemaName}.{tableName}";
                        var index = -1;
                        for (int i = 0; i < targetTableComboBox.Items.Count; i++)
                        {
                            if (targetTableComboBox.Items[i].ToString().Equals(preselectedTable, StringComparison.OrdinalIgnoreCase))
                            {
                                index = i;
                                break;
                            }
                        }
                        targetTableComboBox.SelectedIndex = index >= 0 ? index : 0;
                    }
                    else
                    {
                        targetTableComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error loading tables: {ex.Message}";
                MessageBox.Show($"Error loading available tables: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void TargetTableComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (targetTableComboBox.SelectedItem != null)
            {
                var selectedTable = targetTableComboBox.SelectedItem.ToString();
                var parts = selectedTable.Split('.');
                
                if (parts.Length == 2)
                {
                    schemaName = parts[0];
                    tableName = parts[1];
                    
                    // Update the dialog title
                    this.Text = $"{operation} Data - {schemaName}.{tableName}";
                    
                    // Load table preview if this is export operation
                    if (operation == OperationType.Export)
                    {
                        LoadTableDataPreview();
                    }
                    
                    statusLabel.Text = $"Selected table: {schemaName}.{tableName}";
                }
            }
        }
        
        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filePathTextBox.Text))
            {
                MessageBox.Show("Please select a file path.", "File Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Always validate table selection since we always show it now
            if (string.IsNullOrEmpty(schemaName) || string.IsNullOrEmpty(tableName))
            {
                MessageBox.Show("Please select a target table.", "Table Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                executeButton.Enabled = false;
                cancelButton.Text = "Cancel";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;

                if (operation == OperationType.Export)
                {
                    await ExportData();
                }
                else if (operation == OperationType.Import)
                {
                    await ImportData();
                }

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation failed: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                executeButton.Enabled = true;
                progressBar.Visible = false;
                progressBar.Style = ProgressBarStyle.Continuous;
            }
        }
    }
}
