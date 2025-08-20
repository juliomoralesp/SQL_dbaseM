
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
    public partial class DatabaseScriptGenerator : Form
    {
        private SqlConnection connection;
        private string databaseName;

        private CheckBox includeStructureCheckBox;
        private CheckBox includeDataCheckBox;
        private SimpleTextEditor scriptTextBox;
        private Button generateButton;
        private Button saveButton;
        private Button copyButton;
        private Button printButton;
        private Button closeButton;

        public DatabaseScriptGenerator(SqlConnection connection, string databaseName)
        {
            this.connection = connection;
            this.databaseName = databaseName;
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Script Generator - {databaseName}";
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
                RowCount = 4,
                Padding = new Padding(10)
            };

            includeStructureCheckBox = new CheckBox
            {
                Text = "Include All Tables (CREATE TABLE)",
                Checked = true,
                AutoSize = true
            };

            includeDataCheckBox = new CheckBox
            {
                Text = "Include Data (INSERT statements)",
                Checked = true,
                AutoSize = true
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
            optionsLayout.Controls.Add(generateButton, 0, 3);

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
                script.AppendLine($"-- Script for database {databaseName}");
                script.AppendLine($"-- Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                script.AppendLine();

                var tables = new List<Tuple<string, string>>();
                using (var command = new SqlCommand("SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tables.Add(new Tuple<string, string>(reader["TABLE_SCHEMA"].ToString(), reader["TABLE_NAME"].ToString()));
                        }
                    }
                }

                foreach (var table in tables)
                {
                    var tableScriptGenerator = new TableScriptGenerator(connection, databaseName, table.Item1, table.Item2);
                    if (includeStructureCheckBox.Checked)
                    {
                        script.AppendLine(await tableScriptGenerator.GenerateCreateTableScript());
                        script.AppendLine();
                    }
                    if (includeDataCheckBox.Checked)
                    {
                        script.AppendLine(await tableScriptGenerator.GenerateInsertScript());
                        script.AppendLine();
                    }
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
                saveDialog.FileName = $"{databaseName}_script.sql";
                
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
                PrintUtility.PrintText(scriptTextBox.Text, $"Script for {databaseName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing script: {ex.Message}", "Print Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

