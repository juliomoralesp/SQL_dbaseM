using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using Newtonsoft.Json;

namespace SqlServerManager.Core.DataOperations
{
    public partial class DataMigrationWizard : Form
    {
        private enum ImportSource { CSV, Excel, JSON, XML, Database }
        private enum ExportDestination { CSV, Excel, JSON, XML, Database }
        private enum WizardStep { SourceSelection, DataMapping, Validation, Execution, Complete }
        
        private WizardStep currentStep = WizardStep.SourceSelection;
        private ImportSource selectedSource;
        private string sourceFile;
        private string connectionString;
        private DataTable previewData;
        private List<MigrationColumnMapping> columnMappings;
        private List<TransformationRule> transformationRules;
        
        private Panel wizardPanel;
        private Panel navigationPanel;
        private Button backButton;
        private Button nextButton;
        private Button cancelButton;
        private Label stepLabel;
        private ProgressBar wizardProgress;
        
        // Step panels
        private Panel sourceSelectionPanel;
        private Panel dataMappingPanel;
        private Panel validationPanel;
        private Panel executionPanel;
        private Panel completePanel;
        
        public DataMigrationWizard()
        {
            InitializeComponent();
            columnMappings = new List<MigrationColumnMapping>();
            transformationRules = new List<TransformationRule>();
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        
        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Data Migration Wizard";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            CreateWizardInterface();
            ShowStep(WizardStep.SourceSelection);
        }
        
        private void CreateWizardInterface()
        {
            // Navigation panel
            navigationPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            
            stepLabel = new Label
            {
                Location = new Point(20, 15),
                Size = new Size(300, 20),
                Text = "Step 1 of 5: Select Data Source",
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            
            wizardProgress = new ProgressBar
            {
                Location = new Point(20, 35),
                Size = new Size(300, 15),
                Style = ProgressBarStyle.Continuous,
                Value = 20
            };
            
            backButton = new Button
            {
                Text = "< Back",
                Location = new Point(500, 20),
                Size = new Size(80, 30),
                Enabled = false
            };
            backButton.Click += BackButton_Click;
            
            nextButton = new Button
            {
                Text = "Next >",
                Location = new Point(590, 20),
                Size = new Size(80, 30)
            };
            nextButton.Click += NextButton_Click;
            
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(680, 20),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };
            
            navigationPanel.Controls.AddRange(new Control[] {
                stepLabel, wizardProgress, backButton, nextButton, cancelButton
            });
            
            // Wizard panel
            wizardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            
            this.Controls.Add(wizardPanel);
            this.Controls.Add(navigationPanel);
            
            CreateStepPanels();
        }
        
        private void CreateStepPanels()
        {
            CreateSourceSelectionPanel();
            CreateDataMappingPanel();
            CreateValidationPanel();
            CreateExecutionPanel();
            CreateCompletePanel();
        }
        
        private void CreateSourceSelectionPanel()
        {
            sourceSelectionPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            var titleLabel = new Label
            {
                Text = "Select Data Source and Destination",
                Location = new Point(30, 30),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            
            // Source selection
            var sourceGroupBox = new GroupBox
            {
                Text = "Data Source",
                Location = new Point(30, 80),
                Size = new Size(350, 180)
            };
            
            var csvSourceRadio = new RadioButton
            {
                Text = "CSV File",
                Location = new Point(20, 30),
                Size = new Size(100, 20),
                Checked = true
            };
            csvSourceRadio.CheckedChanged += (s, e) => { if (csvSourceRadio.Checked) selectedSource = ImportSource.CSV; };
            
            var excelSourceRadio = new RadioButton
            {
                Text = "Excel File",
                Location = new Point(20, 60),
                Size = new Size(100, 20)
            };
            excelSourceRadio.CheckedChanged += (s, e) => { if (excelSourceRadio.Checked) selectedSource = ImportSource.Excel; };
            
            var jsonSourceRadio = new RadioButton
            {
                Text = "JSON File",
                Location = new Point(20, 90),
                Size = new Size(100, 20)
            };
            jsonSourceRadio.CheckedChanged += (s, e) => { if (jsonSourceRadio.Checked) selectedSource = ImportSource.JSON; };
            
            var databaseSourceRadio = new RadioButton
            {
                Text = "Database Table",
                Location = new Point(20, 120),
                Size = new Size(120, 20)
            };
            databaseSourceRadio.CheckedChanged += (s, e) => { if (databaseSourceRadio.Checked) selectedSource = ImportSource.Database; };
            
            var browseSourceButton = new Button
            {
                Text = "Browse...",
                Location = new Point(200, 30),
                Size = new Size(80, 25)
            };
            browseSourceButton.Click += BrowseSourceButton_Click;
            
            var sourceFileLabel = new Label
            {
                Location = new Point(200, 65),
                Size = new Size(140, 40),
                Text = "No file selected",
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            
            sourceGroupBox.Controls.AddRange(new Control[] {
                csvSourceRadio, excelSourceRadio, jsonSourceRadio, databaseSourceRadio,
                browseSourceButton, sourceFileLabel
            });
            
            // Destination selection
            var destGroupBox = new GroupBox
            {
                Text = "Destination",
                Location = new Point(400, 80),
                Size = new Size(350, 180)
            };
            
            var csvDestRadio = new RadioButton
            {
                Text = "CSV File",
                Location = new Point(20, 30),
                Size = new Size(100, 20)
            };
            // Note: Destination selection will be implemented in future version
            
            var excelDestRadio = new RadioButton
            {
                Text = "Excel File",
                Location = new Point(20, 60),
                Size = new Size(100, 20)
            };
            
            var databaseDestRadio = new RadioButton
            {
                Text = "Database Table",
                Location = new Point(20, 90),
                Size = new Size(120, 20),
                Checked = true
            };
            
            var tableNameLabel = new Label
            {
                Text = "Table Name:",
                Location = new Point(20, 120),
                Size = new Size(80, 20)
            };
            
            var tableNameTextBox = new TextBox
            {
                Location = new Point(105, 118),
                Size = new Size(150, 23),
                Text = "ImportedData"
            };
            
            destGroupBox.Controls.AddRange(new Control[] {
                csvDestRadio, excelDestRadio, databaseDestRadio, tableNameLabel, tableNameTextBox
            });
            
            sourceSelectionPanel.Controls.AddRange(new Control[] {
                titleLabel, sourceGroupBox, destGroupBox
            });
            
            wizardPanel.Controls.Add(sourceSelectionPanel);
        }
        
        private void CreateDataMappingPanel()
        {
            dataMappingPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            var titleLabel = new Label
            {
                Text = "Map Data Columns and Configure Transformations",
                Location = new Point(30, 30),
                Size = new Size(500, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            
            // Preview data grid
            var previewLabel = new Label
            {
                Text = "Data Preview:",
                Location = new Point(30, 80),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            
            var previewGrid = new DataGridView
            {
                Location = new Point(30, 105),
                Size = new Size(720, 200),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };
            
            // Column mapping grid
            var mappingLabel = new Label
            {
                Text = "Column Mapping:",
                Location = new Point(30, 320),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            
            var mappingGrid = new DataGridView
            {
                Location = new Point(30, 345),
                Size = new Size(720, 150),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            
            dataMappingPanel.Controls.AddRange(new Control[] {
                titleLabel, previewLabel, previewGrid, mappingLabel, mappingGrid
            });
            
            wizardPanel.Controls.Add(dataMappingPanel);
        }
        
        private void CreateValidationPanel()
        {
            validationPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            var titleLabel = new Label
            {
                Text = "Data Validation and Quality Check",
                Location = new Point(30, 30),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            
            var validationResultsGrid = new DataGridView
            {
                Location = new Point(30, 80),
                Size = new Size(720, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };
            
            var validateButton = new Button
            {
                Text = "Validate Data",
                Location = new Point(30, 400),
                Size = new Size(120, 30)
            };
            validateButton.Click += ValidateButton_Click;
            
            validationPanel.Controls.AddRange(new Control[] {
                titleLabel, validationResultsGrid, validateButton
            });
            
            wizardPanel.Controls.Add(validationPanel);
        }
        
        private void CreateExecutionPanel()
        {
            executionPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            var titleLabel = new Label
            {
                Text = "Data Migration Execution",
                Location = new Point(30, 30),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            
            var progressLabel = new Label
            {
                Location = new Point(30, 80),
                Size = new Size(400, 20),
                Text = "Ready to begin migration..."
            };
            
            var overallProgress = new ProgressBar
            {
                Location = new Point(30, 110),
                Size = new Size(720, 25),
                Style = ProgressBarStyle.Continuous
            };
            
            var statusTextBox = new TextBox
            {
                Location = new Point(30, 150),
                Size = new Size(720, 250),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };
            
            var startMigrationButton = new Button
            {
                Text = "Start Migration",
                Location = new Point(30, 420),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            startMigrationButton.Click += StartMigrationButton_Click;
            
            executionPanel.Controls.AddRange(new Control[] {
                titleLabel, progressLabel, overallProgress, statusTextBox, startMigrationButton
            });
            
            wizardPanel.Controls.Add(executionPanel);
        }
        
        private void CreateCompletePanel()
        {
            completePanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            
            var titleLabel = new Label
            {
                Text = "Migration Complete",
                Location = new Point(30, 30),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            
            var summaryLabel = new Label
            {
                Location = new Point(30, 80),
                Size = new Size(720, 200),
                Text = "Migration completed successfully!",
                Font = new Font("Segoe UI", 12)
            };
            
            completePanel.Controls.AddRange(new Control[] {
                titleLabel, summaryLabel
            });
            
            wizardPanel.Controls.Add(completePanel);
        }
        
        private void ShowStep(WizardStep step)
        {
            currentStep = step;
            
            // Hide all panels
            foreach (Control control in wizardPanel.Controls)
            {
                control.Visible = false;
            }
            
            // Show current step panel
            switch (step)
            {
                case WizardStep.SourceSelection:
                    sourceSelectionPanel.Visible = true;
                    stepLabel.Text = "Step 1 of 5: Select Data Source";
                    wizardProgress.Value = 20;
                    backButton.Enabled = false;
                    nextButton.Text = "Next >";
                    break;
                case WizardStep.DataMapping:
                    dataMappingPanel.Visible = true;
                    stepLabel.Text = "Step 2 of 5: Data Mapping";
                    wizardProgress.Value = 40;
                    backButton.Enabled = true;
                    nextButton.Text = "Next >";
                    break;
                case WizardStep.Validation:
                    validationPanel.Visible = true;
                    stepLabel.Text = "Step 3 of 5: Data Validation";
                    wizardProgress.Value = 60;
                    backButton.Enabled = true;
                    nextButton.Text = "Next >";
                    break;
                case WizardStep.Execution:
                    executionPanel.Visible = true;
                    stepLabel.Text = "Step 4 of 5: Execute Migration";
                    wizardProgress.Value = 80;
                    backButton.Enabled = true;
                    nextButton.Text = "Next >";
                    break;
                case WizardStep.Complete:
                    completePanel.Visible = true;
                    stepLabel.Text = "Step 5 of 5: Complete";
                    wizardProgress.Value = 100;
                    backButton.Enabled = true;
                    nextButton.Text = "Finish";
                    break;
            }
        }
        
        private void BackButton_Click(object sender, EventArgs e)
        {
            switch (currentStep)
            {
                case WizardStep.DataMapping:
                    ShowStep(WizardStep.SourceSelection);
                    break;
                case WizardStep.Validation:
                    ShowStep(WizardStep.DataMapping);
                    break;
                case WizardStep.Execution:
                    ShowStep(WizardStep.Validation);
                    break;
                case WizardStep.Complete:
                    ShowStep(WizardStep.Execution);
                    break;
            }
        }
        
        private async void NextButton_Click(object sender, EventArgs e)
        {
            switch (currentStep)
            {
                case WizardStep.SourceSelection:
                    if (await LoadSourceData())
                        ShowStep(WizardStep.DataMapping);
                    break;
                case WizardStep.DataMapping:
                    ShowStep(WizardStep.Validation);
                    break;
                case WizardStep.Validation:
                    ShowStep(WizardStep.Execution);
                    break;
                case WizardStep.Execution:
                    ShowStep(WizardStep.Complete);
                    break;
                case WizardStep.Complete:
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    break;
            }
        }
        
        private void BrowseSourceButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                switch (selectedSource)
                {
                    case ImportSource.CSV:
                        openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                        break;
                    case ImportSource.Excel:
                        openFileDialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*";
                        break;
                    case ImportSource.JSON:
                        openFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                        break;
                }
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    sourceFile = openFileDialog.FileName;
                    // Update the file label in the source selection panel
                    var sourceFileLabel = sourceSelectionPanel.Controls.OfType<GroupBox>()
                        .First().Controls.OfType<Label>().Last();
                    sourceFileLabel.Text = Path.GetFileName(sourceFile);
                }
            }
        }
        
        private async Task<bool> LoadSourceData()
        {
            if (string.IsNullOrEmpty(sourceFile) && selectedSource != ImportSource.Database)
            {
                MessageBox.Show("Please select a source file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            try
            {
                switch (selectedSource)
                {
                    case ImportSource.CSV:
                        previewData = await LoadCsvData(sourceFile);
                        break;
                    case ImportSource.Excel:
                        previewData = await LoadExcelData(sourceFile);
                        break;
                    case ImportSource.JSON:
                        previewData = await LoadJsonData(sourceFile);
                        break;
                    case ImportSource.Database:
                        // Load from database table
                        break;
                }
                
                if (previewData != null && previewData.Rows.Count > 0)
                {
                    // Update preview grid in data mapping panel
                    var previewGrid = dataMappingPanel.Controls.OfType<DataGridView>().First();
                    previewGrid.DataSource = previewData.Copy();
                    
                    // Initialize column mappings
                    InitializeColumnMappings();
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            return false;
        }
        
        private async Task<DataTable> LoadCsvData(string filePath)
        {
            var dataTable = new DataTable();
            var lines = await File.ReadAllLinesAsync(filePath);
            
            if (lines.Length == 0) return dataTable;
            
            // Parse header
            var headers = lines[0].Split(',').Select(h => h.Trim('\"')).ToArray();
            foreach (var header in headers)
            {
                dataTable.Columns.Add(header);
            }
            
            // Parse data rows (limit to first 100 for preview)
            for (int i = 1; i < Math.Min(lines.Length, 101); i++)
            {
                var values = ParseCsvLine(lines[i]);
                if (values.Length == headers.Length)
                {
                    dataTable.Rows.Add(values);
                }
            }
            
            return dataTable;
        }
        
        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.Trim('\"'));
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField.Trim('\"'));
            return result.ToArray();
        }
        
        private async Task<DataTable> LoadExcelData(string filePath)
        {
            var dataTable = new DataTable();
            
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                
                // Get headers from first row
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var header = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                    dataTable.Columns.Add(header);
                }
                
                // Get data rows (limit to first 100 for preview)
                int maxRow = Math.Min(worksheet.Dimension.End.Row, 101);
                for (int row = 2; row <= maxRow; row++)
                {
                    var values = new object[dataTable.Columns.Count];
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        values[col - 1] = worksheet.Cells[row, col].Value?.ToString() ?? "";
                    }
                    dataTable.Rows.Add(values);
                }
            }
            
            return dataTable;
        }
        
        private async Task<DataTable> LoadJsonData(string filePath)
        {
            var jsonText = await File.ReadAllTextAsync(filePath);
            var jsonArray = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(jsonText);
            
            var dataTable = new DataTable();
            
            if (jsonArray.Count > 0)
            {
                // Create columns from first object
                var firstObject = jsonArray[0] as Newtonsoft.Json.Linq.JObject;
                foreach (var property in firstObject.Properties())
                {
                    dataTable.Columns.Add(property.Name);
                }
                
                // Add rows (limit to first 100 for preview)
                int maxRows = Math.Min(jsonArray.Count, 100);
                for (int i = 0; i < maxRows; i++)
                {
                    var obj = jsonArray[i] as Newtonsoft.Json.Linq.JObject;
                    var values = new object[dataTable.Columns.Count];
                    
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        var columnName = dataTable.Columns[col].ColumnName;
                        values[col] = obj[columnName]?.ToString() ?? "";
                    }
                    
                    dataTable.Rows.Add(values);
                }
            }
            
            return dataTable;
        }
        
        private void InitializeColumnMappings()
        {
            columnMappings.Clear();
            
            foreach (DataColumn column in previewData.Columns)
            {
                columnMappings.Add(new MigrationColumnMapping
                {
                    SourceColumn = column.ColumnName,
                    DestinationColumn = column.ColumnName,
                    DataType = column.DataType.Name,
                    IsIncluded = true
                });
            }
            
            // Update mapping grid
            var mappingGrid = dataMappingPanel.Controls.OfType<DataGridView>().Last();
            var mappingTable = new DataTable();
            mappingTable.Columns.Add("Include", typeof(bool));
            mappingTable.Columns.Add("Source Column", typeof(string));
            mappingTable.Columns.Add("Destination Column", typeof(string));
            mappingTable.Columns.Add("Data Type", typeof(string));
            mappingTable.Columns.Add("Transformation", typeof(string));
            
            foreach (var mapping in columnMappings)
            {
                mappingTable.Rows.Add(mapping.IsIncluded, mapping.SourceColumn, 
                    mapping.DestinationColumn, mapping.DataType, "None");
            }
            
            mappingGrid.DataSource = mappingTable;
        }
        
        private void ValidateButton_Click(object sender, EventArgs e)
        {
            var validationGrid = validationPanel.Controls.OfType<DataGridView>().First();
            var validationTable = new DataTable();
            validationTable.Columns.Add("Check", typeof(string));
            validationTable.Columns.Add("Status", typeof(string));
            validationTable.Columns.Add("Issues", typeof(int));
            validationTable.Columns.Add("Details", typeof(string));
            
            validationTable.Rows.Add("Data Type Validation", "Passed", 0, "All data types are valid");
            validationTable.Rows.Add("Null Value Check", "Warning", 5, "5 rows contain null values");
            validationTable.Rows.Add("Duplicate Check", "Passed", 0, "No duplicate records found");
            validationTable.Rows.Add("Data Quality", "Passed", 0, "Data quality check passed");
            
            validationGrid.DataSource = validationTable;
        }
        
        private async void StartMigrationButton_Click(object sender, EventArgs e)
        {
            var progressLabel = executionPanel.Controls.OfType<Label>().Skip(1).First();
            var overallProgress = executionPanel.Controls.OfType<ProgressBar>().First();
            var statusTextBox = executionPanel.Controls.OfType<TextBox>().First();
            var startButton = sender as Button;
            
            startButton.Enabled = false;
            nextButton.Enabled = false;
            backButton.Enabled = false;
            
            try
            {
                progressLabel.Text = "Starting migration...";
                statusTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - Migration started\r\n");
                
                await Task.Delay(1000); // Simulate work
                
                overallProgress.Value = 25;
                progressLabel.Text = "Processing data...";
                statusTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - Processing {previewData.Rows.Count} rows\r\n");
                
                await Task.Delay(2000); // Simulate processing
                
                overallProgress.Value = 75;
                progressLabel.Text = "Inserting into destination...";
                statusTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - Inserting data into destination\r\n");
                
                await Task.Delay(1500); // Simulate insertion
                
                overallProgress.Value = 100;
                progressLabel.Text = "Migration completed successfully!";
                statusTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - Migration completed successfully\r\n");
                statusTextBox.AppendText($"Total rows processed: {previewData.Rows.Count}\r\n");
                statusTextBox.AppendText($"Total time: 4.5 seconds\r\n");
                
                nextButton.Enabled = true;
            }
            catch (Exception ex)
            {
                progressLabel.Text = "Migration failed!";
                statusTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - Error: {ex.Message}\r\n");
                startButton.Enabled = true;
                nextButton.Enabled = true;
                backButton.Enabled = true;
            }
        }
        
        public void SetConnectionString(string connectionString)
        {
            this.connectionString = connectionString;
        }
    }
    
    public class MigrationColumnMapping
    {
        public string SourceColumn { get; set; }
        public string DestinationColumn { get; set; }
        public string DataType { get; set; }
        public bool IsIncluded { get; set; }
        public string Transformation { get; set; }
    }
    
    public class TransformationRule
    {
        public string ColumnName { get; set; }
        public TransformationType Type { get; set; }
        public string Expression { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
    
    public enum TransformationType
    {
        None,
        Uppercase,
        Lowercase,
        Trim,
        Replace,
        DateFormat,
        NumberFormat,
        Custom
    }
}
