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

        private SqlConnection connection;
        private string databaseName;
        private string schemaName;
        private string tableName;
        private OperationType operation;
        
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
        private CheckBox overwriteDataCheckBox;
        private NumericUpDown batchSizeNumeric;
        private Panel optionsPanel;

        public DataImportExportDialog(SqlConnection connection, string databaseName, string schemaName, string tableName, OperationType operation)
        {
            this.connection = connection;
            this.databaseName = databaseName;
            this.schemaName = schemaName;
            this.tableName = tableName;
            this.operation = operation;
            
            InitializeComponent();
            SetupForOperation();
        }

        private void InitializeComponent()
        {
            this.Text = $"{operation} Data - {schemaName}.{tableName}";
            this.Size = new Size(800, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(600, 400);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };

            // File selection panel
            var filePanel = new GroupBox
            {
                Text = "File Settings",
                Height = 180,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var fileLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(10)
            };

            // Format selection
            var formatLabel = new Label { Text = "Format:", AutoSize = true, Font = new Font("Segoe UI", 9) };
            formatComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Font = new Font("Segoe UI", 9)
            };
            formatComboBox.Items.AddRange(new[] { "CSV", "Excel (.xlsx)", "JSON", "XML", "SQL Script" });
            formatComboBox.SelectedIndex = 0;
            formatComboBox.SelectedIndexChanged += FormatComboBox_SelectedIndexChanged;

            // File path
            var fileLabel = new Label { Text = "File Path:", AutoSize = true, Font = new Font("Segoe UI", 9) };
            filePathTextBox = new TextBox { Width = 300, Font = new Font("Segoe UI", 9) };
            browseButton = new Button 
            { 
                Text = "Browse...", 
                Width = 80,
                Height = 35,
                Font = new Font("Segoe UI", 9)
            };
            browseButton.Click += BrowseButton_Click;

            // Encoding
            var encodingLabel = new Label { Text = "Encoding:", AutoSize = true, Font = new Font("Segoe UI", 9) };
            encodingComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150,
                Font = new Font("Segoe UI", 9)
            };
            encodingComboBox.Items.AddRange(new[] { "UTF-8", "UTF-16", "ASCII", "Windows-1252" });
            encodingComboBox.SelectedIndex = 0;

            // Delimiter (for CSV)
            var delimiterLabel = new Label { Text = "Delimiter:", AutoSize = true, Font = new Font("Segoe UI", 9) };
            delimiterComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Font = new Font("Segoe UI", 9)
            };
            delimiterComboBox.Items.AddRange(new[] { ",", ";", "\\t", "|" });
            delimiterComboBox.SelectedIndex = 0;

            fileLayout.Controls.Add(formatLabel, 0, 0);
            fileLayout.Controls.Add(formatComboBox, 1, 0);
            fileLayout.Controls.Add(new Label(), 2, 0);

            fileLayout.Controls.Add(fileLabel, 0, 1);
            fileLayout.Controls.Add(filePathTextBox, 1, 1);
            fileLayout.Controls.Add(browseButton, 2, 1);

            fileLayout.Controls.Add(encodingLabel, 0, 2);
            fileLayout.Controls.Add(encodingComboBox, 1, 2);
            fileLayout.Controls.Add(new Label(), 2, 2);

            fileLayout.Controls.Add(delimiterLabel, 0, 3);
            fileLayout.Controls.Add(delimiterComboBox, 1, 3);
            fileLayout.Controls.Add(new Label(), 2, 3);

            filePanel.Controls.Add(fileLayout);

            // Options panel
            optionsPanel = new Panel
            {
                Height = 100,
                Dock = DockStyle.Top
            };

            var optionsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(10)
            };

            includeHeadersCheckBox = new CheckBox
            {
                Text = "Include Headers",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, 0, 20, 0)
            };

            overwriteDataCheckBox = new CheckBox
            {
                Text = "Overwrite Existing Data",
                Checked = false,
                AutoSize = true,
                Margin = new Padding(0, 0, 20, 0)
            };

            var batchLabel = new Label
            {
                Text = "Batch Size:",
                AutoSize = true,
                Margin = new Padding(0, 3, 5, 0)
            };

            batchSizeNumeric = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 10000,
                Value = 1000,
                Width = 80,
                Margin = new Padding(0, 0, 20, 0)
            };

            optionsLayout.Controls.AddRange(new Control[] 
            { 
                includeHeadersCheckBox, 
                overwriteDataCheckBox,
                batchLabel,
                batchSizeNumeric
            });

            optionsPanel.Controls.Add(optionsLayout);

            // Preview panel
            var previewPanel = new GroupBox
            {
                Text = "Data Preview",
                Dock = DockStyle.Fill
            };

            previewGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            previewPanel.Controls.Add(previewGridView);

            // Status and buttons panel
            var bottomPanel = new Panel
            {
                Height = 80,
                Dock = DockStyle.Bottom
            };

            progressBar = new ProgressBar
            {
                Location = new Point(10, 10),
                Size = new Size(300, 20),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            statusLabel = new Label
            {
                Location = new Point(10, 35),
                Size = new Size(400, 20),
                Text = "Ready"
            };

            executeButton = new Button
            {
                Text = operation == OperationType.Import ? "Import Data" : "Export Data",
                Size = new Size(100, 30),
                Location = new Point(520, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            executeButton.Click += ExecuteButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                Location = new Point(630, 25),
                DialogResult = DialogResult.Cancel
            };

            bottomPanel.Controls.AddRange(new Control[] 
            { 
                progressBar, 
                statusLabel, 
                executeButton, 
                cancelButton 
            });

            // Add panels to main layout
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));

            mainLayout.Controls.Add(filePanel, 0, 0);
            mainLayout.Controls.Add(optionsPanel, 0, 1);
            mainLayout.Controls.Add(previewPanel, 0, 2);
            mainLayout.Controls.Add(bottomPanel, 0, 3);

            this.Controls.Add(mainLayout);
        }

        private void SetupForOperation()
        {
            if (operation == OperationType.Export)
            {
                overwriteDataCheckBox.Visible = false;
                overwriteDataCheckBox.Checked = false;
                
                // Load table data for preview
                LoadTableDataPreview();
            }
            else
            {
                // For import, we'll load preview when file is selected
                previewGridView.DataSource = null;
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

            if (operation == OperationType.Import)
            {
                using (var openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = filter;
                    openDialog.Title = "Select file to import";
                    
                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        filePathTextBox.Text = openDialog.FileName;
                        LoadFilePreview();
                    }
                }
            }
            else
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

        private void LoadFilePreview()
        {
            if (string.IsNullOrEmpty(filePathTextBox.Text) || !File.Exists(filePathTextBox.Text))
                return;

            try
            {
                var selectedFormat = formatComboBox.SelectedItem.ToString();
                DataTable previewData = null;

                switch (selectedFormat)
                {
                    case "CSV":
                        previewData = LoadCsvPreview(filePathTextBox.Text);
                        break;
                    case "Excel (.xlsx)":
                        previewData = LoadExcelPreview(filePathTextBox.Text);
                        break;
                    case "JSON":
                        previewData = LoadJsonPreview(filePathTextBox.Text);
                        break;
                    case "XML":
                        previewData = LoadXmlPreview(filePathTextBox.Text);
                        break;
                }

                if (previewData != null)
                {
                    previewGridView.DataSource = previewData;
                    statusLabel.Text = $"Preview loaded: {previewData.Rows.Count} rows";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error loading preview: {ex.Message}";
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
                var lines = new List<string>();
                for (int i = 0; i < 100 && !reader.EndOfStream; i++)
                {
                    lines.Add(reader.ReadLine());
                }

                if (lines.Count > 0)
                {
                    // Parse headers
                    var headers = lines[0].Split(delimiter);
                    var startRow = 0;

                    if (includeHeadersCheckBox.Checked)
                    {
                        foreach (var header in headers)
                        {
                            dataTable.Columns.Add(header.Trim('"'));
                        }
                        startRow = 1;
                    }
                    else
                    {
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dataTable.Columns.Add($"Column{i + 1}");
                        }
                    }

                    // Parse data rows
                    for (int i = startRow; i < lines.Count; i++)
                    {
                        var values = lines[i].Split(delimiter);
                        var row = dataTable.NewRow();
                        
                        for (int j = 0; j < Math.Min(values.Length, dataTable.Columns.Count); j++)
                        {
                            row[j] = values[j].Trim('"');
                        }
                        
                        dataTable.Rows.Add(row);
                    }
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
                
                if (worksheet.Dimension != null)
                {
                    var startRow = 1;
                    var endCol = worksheet.Dimension.End.Column;
                    var maxPreviewRows = Math.Min(100, worksheet.Dimension.End.Row);

                    // Add columns
                    if (includeHeadersCheckBox.Checked && worksheet.Dimension.End.Row > 1)
                    {
                        for (int col = 1; col <= endCol; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                            dataTable.Columns.Add(headerValue);
                        }
                        startRow = 2;
                    }
                    else
                    {
                        for (int col = 1; col <= endCol; col++)
                        {
                            dataTable.Columns.Add($"Column{col}");
                        }
                    }

                    // Add rows
                    for (int row = startRow; row <= maxPreviewRows; row++)
                    {
                        var dataRow = dataTable.NewRow();
                        for (int col = 1; col <= endCol; col++)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Value?.ToString() ?? "";
                        }
                        dataTable.Rows.Add(dataRow);
                    }
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

            if (jsonArray?.Count > 0)
            {
                // Add columns based on first object
                foreach (var key in jsonArray[0].Keys)
                {
                    dataTable.Columns.Add(key);
                }

                // Add rows (limit to 100 for preview)
                var previewCount = Math.Min(100, jsonArray.Count);
                for (int i = 0; i < previewCount; i++)
                {
                    var row = dataTable.NewRow();
                    foreach (var kvp in jsonArray[i])
                    {
                        if (dataTable.Columns.Contains(kvp.Key))
                        {
                            row[kvp.Key] = kvp.Value?.ToString() ?? "";
                        }
                    }
                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }

        private DataTable LoadXmlPreview(string filePath)
        {
            var dataTable = new DataTable();
            var doc = new XmlDocument();
            doc.Load(filePath);

            var nodes = doc.SelectNodes("//row") ?? doc.DocumentElement?.ChildNodes;
            if (nodes?.Count > 0)
            {
                // Add columns based on first node
                var firstNode = nodes[0];
                if (firstNode is XmlElement firstElement)
                {
                    foreach (XmlNode child in firstElement.ChildNodes)
                    {
                        if (child is XmlElement)
                        {
                            dataTable.Columns.Add(child.Name);
                        }
                    }

                    // Add rows (limit to 100 for preview)
                    var previewCount = Math.Min(100, nodes.Count);
                    for (int i = 0; i < previewCount; i++)
                    {
                        if (nodes[i] is XmlElement element)
                        {
                            var row = dataTable.NewRow();
                            foreach (XmlNode child in element.ChildNodes)
                            {
                                if (child is XmlElement childElement && dataTable.Columns.Contains(childElement.Name))
                                {
                                    row[childElement.Name] = childElement.InnerText;
                                }
                            }
                            dataTable.Rows.Add(row);
                        }
                    }
                }
            }

            return dataTable;
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

        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filePathTextBox.Text))
            {
                MessageBox.Show("Please select a file path.", "File Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                executeButton.Enabled = false;
                cancelButton.Text = "Cancel";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;

                if (operation == OperationType.Import)
                {
                    await ImportData();
                }
                else
                {
                    await ExportData();
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

        private async Task ImportData()
        {
            var selectedFormat = formatComboBox.SelectedItem.ToString();
            var batchSize = (int)batchSizeNumeric.Value;

            statusLabel.Text = "Reading import file...";

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
            }

            if (importData == null || importData.Rows.Count == 0)
            {
                statusLabel.Text = "No data to import";
                return;
            }

            statusLabel.Text = $"Importing {importData.Rows.Count} rows...";

            // Clear existing data if requested
            if (overwriteDataCheckBox.Checked)
            {
                var deleteCommand = new SqlCommand($"DELETE FROM [{schemaName}].[{tableName}]", connection);
                await deleteCommand.ExecuteNonQueryAsync();
            }

            // Insert data in batches
            var totalRows = importData.Rows.Count;
            var importedRows = 0;

            for (int i = 0; i < totalRows; i += batchSize)
            {
                var batchRows = importData.Rows.Cast<DataRow>()
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                await InsertBatch(batchRows, importData.Columns);
                
                importedRows += batchRows.Count;
                statusLabel.Text = $"Imported {importedRows}/{totalRows} rows...";
            }

            statusLabel.Text = $"Import completed: {importedRows} rows imported";
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
                    var firstLine = reader.ReadLine();
                    if (firstLine == null) return dataTable;

                    var headers = firstLine.Split(delimiter);
                    var startFromSecondLine = includeHeadersCheckBox.Checked;

                    // Add columns
                    if (startFromSecondLine)
                    {
                        foreach (var header in headers)
                        {
                            dataTable.Columns.Add(header.Trim('"'));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dataTable.Columns.Add($"Column{i + 1}");
                        }

                        // Add first line as data if no headers
                        var firstRow = dataTable.NewRow();
                        for (int i = 0; i < Math.Min(headers.Length, dataTable.Columns.Count); i++)
                        {
                            firstRow[i] = headers[i].Trim('"');
                        }
                        dataTable.Rows.Add(firstRow);
                    }

                    // Read remaining lines
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var values = line.Split(delimiter);
                        var row = dataTable.NewRow();

                        for (int i = 0; i < Math.Min(values.Length, dataTable.Columns.Count); i++)
                        {
                            row[i] = values[i].Trim('"');
                        }

                        dataTable.Rows.Add(row);
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
                    var worksheet = package.Workbook.Worksheets[0];
                    
                    if (worksheet.Dimension != null)
                    {
                        var startRow = 1;
                        var endCol = worksheet.Dimension.End.Column;
                        var endRow = worksheet.Dimension.End.Row;

                        // Add columns
                        if (includeHeadersCheckBox.Checked && endRow > 1)
                        {
                            for (int col = 1; col <= endCol; col++)
                            {
                                var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                                dataTable.Columns.Add(headerValue);
                            }
                            startRow = 2;
                        }
                        else
                        {
                            for (int col = 1; col <= endCol; col++)
                            {
                                dataTable.Columns.Add($"Column{col}");
                            }
                        }

                        // Add all rows
                        for (int row = startRow; row <= endRow; row++)
                        {
                            var dataRow = dataTable.NewRow();
                            for (int col = 1; col <= endCol; col++)
                            {
                                dataRow[col - 1] = worksheet.Cells[row, col].Value?.ToString() ?? "";
                            }
                            dataTable.Rows.Add(dataRow);
                        }
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

                if (jsonArray?.Count > 0)
                {
                    // Add columns
                    foreach (var key in jsonArray[0].Keys)
                    {
                        dataTable.Columns.Add(key);
                    }

                    // Add all rows
                    foreach (var jsonObject in jsonArray)
                    {
                        var row = dataTable.NewRow();
                        foreach (var kvp in jsonObject)
                        {
                            if (dataTable.Columns.Contains(kvp.Key))
                            {
                                row[kvp.Key] = kvp.Value?.ToString() ?? "";
                            }
                        }
                        dataTable.Rows.Add(row);
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

                var nodes = doc.SelectNodes("//row") ?? doc.DocumentElement?.ChildNodes;
                if (nodes?.Count > 0)
                {
                    // Add columns based on first node
                    var firstNode = nodes[0];
                    if (firstNode is XmlElement firstElement)
                    {
                        foreach (XmlNode child in firstElement.ChildNodes)
                        {
                            if (child is XmlElement)
                            {
                                dataTable.Columns.Add(child.Name);
                            }
                        }

                        // Add all rows
                        foreach (XmlNode node in nodes)
                        {
                            if (node is XmlElement element)
                            {
                                var row = dataTable.NewRow();
                                foreach (XmlNode child in element.ChildNodes)
                                {
                                    if (child is XmlElement childElement && dataTable.Columns.Contains(childElement.Name))
                                    {
                                        row[childElement.Name] = childElement.InnerText;
                                    }
                                }
                                dataTable.Rows.Add(row);
                            }
                        }
                    }
                }

                return dataTable;
            });
        }

        private async Task InsertBatch(List<DataRow> batchRows, DataColumnCollection columns)
        {
            var columnNames = columns.Cast<DataColumn>().Select(c => $"[{c.ColumnName}]").ToArray();
            var valueParams = columns.Cast<DataColumn>().Select((c, i) => $"@param{i}").ToArray();
            
            var insertQuery = $@"
                INSERT INTO [{schemaName}].[{tableName}] 
                ({string.Join(", ", columnNames)}) 
                VALUES ({string.Join(", ", valueParams)})";

            using (var command = new SqlCommand(insertQuery, connection))
            {
                // Add parameters for the first row to define parameter types
                for (int i = 0; i < columns.Count; i++)
                {
                    command.Parameters.Add($"@param{i}", SqlDbType.NVarChar);
                }

                foreach (var row in batchRows)
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        command.Parameters[$"@param{i}"].Value = row[i] ?? DBNull.Value;
                    }

                    await command.ExecuteNonQueryAsync();
                }
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
            await Task.Run(() =>
            {
                var encoding = GetSelectedEncoding();
                
                using (var writer = new StreamWriter(filePathTextBox.Text, false, encoding))
                {
                    writer.WriteLine($"-- Data export for {schemaName}.{tableName}");
                    writer.WriteLine($"-- Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();

                    if (dataTable.Rows.Count > 0)
                    {
                        var columnNames = dataTable.Columns.Cast<DataColumn>()
                            .Select(c => $"[{c.ColumnName}]").ToArray();

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
                    }
                }
            });
        }
    }
}
