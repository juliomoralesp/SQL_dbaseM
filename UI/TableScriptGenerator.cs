using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SqlServerManager.Core.Controls;

namespace SqlServerManager.UI
{
    public partial class TableScriptGenerator : Form
    {
        private SqlConnection connection;
        private string databaseName;
        private string schemaName;
        private string tableName;
        
        private CheckBox includeStructureCheckBox;
        private CheckBox includeDataCheckBox;
        private CheckBox includeIndexesCheckBox;
        private CheckBox includeConstraintsCheckBox;
        private CheckBox includeTriggersCheckBox;
        private NumericUpDown rowLimitNumeric;
        private SimpleTextEditor scriptTextBox;
        private Button generateButton;
        private Button saveButton;
        private Button copyButton;
        private Button printButton;
        private Button closeButton;

        public TableScriptGenerator(SqlConnection connection, string databaseName, string schemaName, string tableName)
        {
            this.connection = connection;
            this.databaseName = databaseName;
            this.schemaName = schemaName;
            this.tableName = tableName;
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Script Generator - {schemaName}.{tableName}";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(700, 500);

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            // Options panel
            var optionsPanel = new GroupBox
            {
                Text = "Script Options",
                Size = new Size(250, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var optionsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                Padding = new Padding(10)
            };

            includeStructureCheckBox = new CheckBox
            {
                Text = "Include Table Structure (CREATE TABLE)",
                Checked = true,
                AutoSize = true
            };

            includeDataCheckBox = new CheckBox
            {
                Text = "Include Data (INSERT statements)",
                Checked = true,
                AutoSize = true
            };

            includeIndexesCheckBox = new CheckBox
            {
                Text = "Include Indexes",
                Checked = true,
                AutoSize = true
            };

            includeConstraintsCheckBox = new CheckBox
            {
                Text = "Include Constraints",
                Checked = true,
                AutoSize = true
            };

            includeTriggersCheckBox = new CheckBox
            {
                Text = "Include Triggers",
                Checked = false,
                AutoSize = true
            };

            var rowLimitLabel = new Label
            {
                Text = "Data Row Limit (0 = All):",
                AutoSize = true
            };

            rowLimitNumeric = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 1000000,
                Value = 1000,
                Width = 100
            };

            generateButton = new Button
            {
                Text = "Generate Script",
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            generateButton.Click += GenerateButton_Click;

            optionsLayout.Controls.Add(includeStructureCheckBox, 0, 0);
            optionsLayout.Controls.Add(includeDataCheckBox, 0, 1);
            optionsLayout.Controls.Add(includeIndexesCheckBox, 0, 2);
            optionsLayout.Controls.Add(includeConstraintsCheckBox, 0, 3);
            optionsLayout.Controls.Add(includeTriggersCheckBox, 0, 4);
            optionsLayout.Controls.Add(rowLimitLabel, 0, 5);
            optionsLayout.Controls.Add(rowLimitNumeric, 0, 6);
            optionsLayout.Controls.Add(generateButton, 0, 7);

            optionsPanel.Controls.Add(optionsLayout);

            // Script display panel
            var scriptPanel = new GroupBox
            {
                Text = "Generated Script",
                Dock = DockStyle.Fill
            };

            scriptTextBox = new SimpleTextEditor
            {
                Dock = DockStyle.Fill,
                ReadOnly = false
            };

            // Configure basic text editing
            scriptTextBox.LexerName = "sql";
            scriptTextBox.ApplySqlStyling();

            scriptPanel.Controls.Add(scriptTextBox);

            // Button panel
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };

            saveButton = new Button
            {
                Text = "Save Script...",
                Size = new Size(100, 30),
                Location = new Point(10, 10)
            };
            saveButton.Click += SaveButton_Click;

            copyButton = new Button
            {
                Text = "Copy to Clipboard",
                Size = new Size(120, 30),
                Location = new Point(120, 10)
            };
            copyButton.Click += CopyButton_Click;

            printButton = new Button
            {
                Text = "Print",
                Size = new Size(80, 30),
                Location = new Point(250, 10)
            };
            printButton.Click += PrintButton_Click;

            closeButton = new Button
            {
                Text = "Close",
                Size = new Size(80, 30),
                Location = new Point(340, 10),
                DialogResult = DialogResult.OK
            };

            buttonPanel.Controls.AddRange(new Control[] { saveButton, copyButton, printButton, closeButton });

            // Layout setup
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            mainPanel.Controls.Add(optionsPanel, 0, 0);
            mainPanel.Controls.Add(scriptPanel, 1, 0);
            mainPanel.SetColumnSpan(buttonPanel, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 1);

            this.Controls.Add(mainPanel);
        }

        private async void GenerateButton_Click(object sender, EventArgs e)
        {
            try
            {
                generateButton.Enabled = false;
                generateButton.Text = "Generating...";
                
                var script = new StringBuilder();
                script.AppendLine($"-- Script for table {schemaName}.{tableName}");
                script.AppendLine($"-- Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                script.AppendLine();

                if (includeStructureCheckBox.Checked)
                {
                    script.AppendLine(await GenerateCreateTableScript());
                    script.AppendLine();
                }

                if (includeConstraintsCheckBox.Checked)
                {
                    script.AppendLine(await GenerateConstraintsScript());
                    script.AppendLine();
                }

                if (includeIndexesCheckBox.Checked)
                {
                    script.AppendLine(await GenerateIndexesScript());
                    script.AppendLine();
                }

                if (includeDataCheckBox.Checked)
                {
                    script.AppendLine(await GenerateInsertScript());
                    script.AppendLine();
                }

                if (includeTriggersCheckBox.Checked)
                {
                    script.AppendLine(await GenerateTriggersScript());
                }

                scriptTextBox.Text = script.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating script: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                generateButton.Enabled = true;
                generateButton.Text = "Generate Script";
            }
        }

        public async Task<string> GenerateCreateTableScript()
        {
            var script = new StringBuilder();
            script.AppendLine($"-- CREATE TABLE script for {schemaName}.{tableName}");
            script.AppendLine($"CREATE TABLE [{schemaName}].[{tableName}] (");

            var query = @"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as IS_IDENTITY
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_SCHEMA = @schema 
                AND c.TABLE_NAME = @table
                ORDER BY c.ORDINAL_POSITION";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@schema", schemaName);
                command.Parameters.AddWithValue("@table", tableName);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var columns = new List<string>();
                    
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader["COLUMN_NAME"].ToString();
                        var dataType = reader["DATA_TYPE"].ToString().ToUpper();
                        var maxLength = reader["CHARACTER_MAXIMUM_LENGTH"];
                        var precision = reader["NUMERIC_PRECISION"];
                        var scale = reader["NUMERIC_SCALE"];
                        var isNullable = reader["IS_NULLABLE"].ToString() == "YES";
                        var defaultValue = reader["COLUMN_DEFAULT"];
                        var isIdentity = Convert.ToBoolean(reader["IS_IDENTITY"]);

                        var columnDef = new StringBuilder();
                        columnDef.Append($"    [{columnName}] {dataType}");

                        // Add length/precision
                        if (maxLength != DBNull.Value && Convert.ToInt32(maxLength) > 0)
                        {
                            if (Convert.ToInt32(maxLength) == -1)
                                columnDef.Append("(MAX)");
                            else
                                columnDef.Append($"({maxLength})");
                        }
                        else if (precision != DBNull.Value && Convert.ToInt32(precision) > 0)
                        {
                            if (scale != DBNull.Value && Convert.ToInt32(scale) > 0)
                                columnDef.Append($"({precision},{scale})");
                            else
                                columnDef.Append($"({precision})");
                        }

                        // Add identity
                        if (isIdentity)
                            columnDef.Append(" IDENTITY(1,1)");

                        // Add nullable
                        if (!isNullable)
                            columnDef.Append(" NOT NULL");

                        // Add default
                        if (defaultValue != DBNull.Value)
                            columnDef.Append($" DEFAULT {defaultValue}");

                        columns.Add(columnDef.ToString());
                    }

                    script.AppendLine(string.Join(",\n", columns));
                }
            }

            // Add primary key constraint
            var pkQuery = @"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 
                AND TABLE_SCHEMA = @schema 
                AND TABLE_NAME = @table
                ORDER BY ORDINAL_POSITION";

            using (var pkCommand = new SqlCommand(pkQuery, connection))
            {
                pkCommand.Parameters.AddWithValue("@schema", schemaName);
                pkCommand.Parameters.AddWithValue("@table", tableName);

                using (var pkReader = await pkCommand.ExecuteReaderAsync())
                {
                    var pkColumns = new List<string>();
                    while (await pkReader.ReadAsync())
                    {
                        pkColumns.Add($"[{pkReader["COLUMN_NAME"]}]");
                    }

                    if (pkColumns.Count > 0)
                    {
                        script.AppendLine($",    PRIMARY KEY ({string.Join(", ", pkColumns)})");
                    }
                }
            }

            script.AppendLine(");");
            return script.ToString();
        }

        private async Task<string> GenerateConstraintsScript()
        {
            var script = new StringBuilder();
            script.AppendLine($"-- Constraints for {schemaName}.{tableName}");

            var query = @"
                SELECT 
                    tc.CONSTRAINT_NAME,
                    tc.CONSTRAINT_TYPE,
                    kcu.COLUMN_NAME,
                    ccu.TABLE_NAME AS REFERENCED_TABLE,
                    ccu.COLUMN_NAME AS REFERENCED_COLUMN
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
                    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                LEFT JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu
                    ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                WHERE tc.TABLE_SCHEMA = @schema 
                AND tc.TABLE_NAME = @table
                AND tc.CONSTRAINT_TYPE != 'PRIMARY KEY'
                ORDER BY tc.CONSTRAINT_TYPE, tc.CONSTRAINT_NAME";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@schema", schemaName);
                command.Parameters.AddWithValue("@table", tableName);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var constraintName = reader["CONSTRAINT_NAME"].ToString();
                        var constraintType = reader["CONSTRAINT_TYPE"].ToString();
                        var columnName = reader["COLUMN_NAME"].ToString();
                        var refTable = reader["REFERENCED_TABLE"].ToString();
                        var refColumn = reader["REFERENCED_COLUMN"].ToString();

                        if (constraintType == "FOREIGN KEY")
                        {
                            script.AppendLine($"ALTER TABLE [{schemaName}].[{tableName}]");
                            script.AppendLine($"ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{columnName}])");
                            script.AppendLine($"REFERENCES [{schemaName}].[{refTable}] ([{refColumn}]);");
                            script.AppendLine();
                        }
                    }
                }
            }

            return script.ToString();
        }

        private async Task<string> GenerateIndexesScript()
        {
            var script = new StringBuilder();
            script.AppendLine($"-- Indexes for {schemaName}.{tableName}");

            var query = @"
                SELECT 
                    i.name as IndexName,
                    i.is_unique,
                    c.name as ColumnName
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.object_id = OBJECT_ID(@fullTableName)
                AND i.is_primary_key = 0
                ORDER BY i.name, ic.key_ordinal";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@fullTableName", $"{schemaName}.{tableName}");

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var indexes = new Dictionary<string, List<string>>();
                    var uniqueIndexes = new HashSet<string>();

                    while (await reader.ReadAsync())
                    {
                        var indexName = reader["IndexName"].ToString();
                        var isUnique = Convert.ToBoolean(reader["is_unique"]);
                        var columnName = reader["ColumnName"].ToString();

                        if (!indexes.ContainsKey(indexName))
                            indexes[indexName] = new List<string>();

                        indexes[indexName].Add(columnName);

                        if (isUnique)
                            uniqueIndexes.Add(indexName);
                    }

                    foreach (var index in indexes)
                    {
                        var unique = uniqueIndexes.Contains(index.Key) ? "UNIQUE " : "";
                        script.AppendLine($"CREATE {unique}INDEX [{index.Key}]");
                        script.AppendLine($"ON [{schemaName}].[{tableName}] ({string.Join(", ", index.Value.Select(c => $"[{c}]"))});");
                        script.AppendLine();
                    }
                }
            }

            return script.ToString();
        }

        public async Task<string> GenerateInsertScript()
        {
            var script = new StringBuilder();
            script.AppendLine($"-- INSERT statements for {schemaName}.{tableName}");

            var limit = (int)rowLimitNumeric.Value;
            var limitClause = limit > 0 ? $"TOP {limit}" : "";

            var query = $"SELECT {limitClause} * FROM [{schemaName}].[{tableName}]";

            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var columnNames = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columnNames.Add($"[{reader.GetName(i)}]");
                    }

                    var valuesList = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        var values = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.GetValue(i);
                            if (value == DBNull.Value)
                                values.Add("NULL");
                            else if (value is string || value is DateTime)
                                values.Add($"'{value.ToString().Replace("'", "''")}'");
                            else if (value is bool)
                                values.Add(((bool)value) ? "1" : "0");
                            else
                                values.Add(value.ToString());
                        }
                        valuesList.Add($"({string.Join(", ", values)})");
                    }

                    if (valuesList.Count > 0)
                    {
                        script.AppendLine($"INSERT INTO [{schemaName}].[{tableName}]");
                        script.AppendLine($"({string.Join(", ", columnNames)})");
                        script.AppendLine("VALUES");
                        script.AppendLine(string.Join(",\n", valuesList));
                        script.AppendLine(";");
                    }
                }
            }

            return script.ToString();
        }

        private async Task<string> GenerateTriggersScript()
        {
            var script = new StringBuilder();
            script.AppendLine($"-- Triggers for {schemaName}.{tableName}");

            var query = @"
                SELECT 
                    t.name as TriggerName,
                    m.definition as TriggerDefinition
                FROM sys.triggers t
                INNER JOIN sys.sql_modules m ON t.object_id = m.object_id
                WHERE t.parent_id = OBJECT_ID(@fullTableName)";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@fullTableName", $"{schemaName}.{tableName}");

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var triggerName = reader["TriggerName"].ToString();
                        var triggerDefinition = reader["TriggerDefinition"].ToString();

                        script.AppendLine($"-- Trigger: {triggerName}");
                        script.AppendLine(triggerDefinition);
                        script.AppendLine();
                    }
                }
            }

            return script.ToString();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(scriptTextBox.Text))
            {
                MessageBox.Show("No script to save. Please generate a script first.", "No Script", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "SQL Script Files (*.sql)|*.sql|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                saveDialog.FileName = $"{schemaName}_{tableName}_script.sql";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        System.IO.File.WriteAllText(saveDialog.FileName, scriptTextBox.Text);
                        MessageBox.Show("Script saved successfully.", "Save Complete", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving script: {ex.Message}", "Save Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(scriptTextBox.Text))
            {
                MessageBox.Show("No script to copy. Please generate a script first.", "No Script", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Clipboard.SetText(scriptTextBox.Text);
                MessageBox.Show("Script copied to clipboard.", "Copy Complete", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Copy Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(scriptTextBox.Text))
            {
                MessageBox.Show("No script to print. Please generate a script first.", "No Script", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                PrintUtility.PrintText(scriptTextBox.Text, $"Script for {schemaName}.{tableName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing script: {ex.Message}", "Print Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
