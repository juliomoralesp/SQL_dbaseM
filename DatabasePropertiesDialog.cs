using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace SqlServerManager
{
    public class DatabasePropertiesDialog : Form
    {
        private SqlConnection connection;
        private string databaseName;
        private TextBox propertiesTextBox;
        private Button closeButton;

        public DatabasePropertiesDialog(SqlConnection conn, string dbName)
        {
            connection = conn;
            databaseName = dbName;
            InitializeComponent();
            LoadProperties();
        }

        private void InitializeComponent()
        {
            this.Text = $"Database Properties - {databaseName}";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            propertiesTextBox = new TextBox();
            propertiesTextBox.Multiline = true;
            propertiesTextBox.ScrollBars = ScrollBars.Both;
            propertiesTextBox.ReadOnly = true;
            propertiesTextBox.Font = new Font("Consolas", 9);
            propertiesTextBox.Dock = DockStyle.Fill;

            closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Size = new Size(75, 30);
            closeButton.Dock = DockStyle.Bottom;
            closeButton.DialogResult = DialogResult.Cancel;

            this.Controls.Add(propertiesTextBox);
            this.Controls.Add(closeButton);
        }

        private void LoadProperties()
        {
            try
            {
                var properties = new System.Text.StringBuilder();
                properties.AppendLine($"DATABASE PROPERTIES FOR: {databaseName}");
                properties.AppendLine(new string('=', 60));
                properties.AppendLine();

                // Get basic database info
                string query = $@"
                    USE [{databaseName}];
                    SELECT 
                        DB_NAME() as DatabaseName,
                        SUSER_SNAME(owner_sid) as Owner,
                        create_date as CreateDate,
                        compatibility_level as CompatibilityLevel,
                        collation_name as Collation,
                        state_desc as State,
                        recovery_model_desc as RecoveryModel
                    FROM sys.databases 
                    WHERE name = @dbname";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@dbname", databaseName);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            properties.AppendLine("GENERAL INFORMATION:");
                            properties.AppendLine($"  Database Name: {reader["DatabaseName"]}");
                            properties.AppendLine($"  Owner: {reader["Owner"]}");
                            properties.AppendLine($"  Created: {reader["CreateDate"]}");
                            properties.AppendLine($"  Compatibility Level: SQL Server {reader["CompatibilityLevel"]}");
                            properties.AppendLine($"  Collation: {reader["Collation"]}");
                            properties.AppendLine($"  State: {reader["State"]}");
                            properties.AppendLine($"  Recovery Model: {reader["RecoveryModel"]}");
                        }
                    }
                }

                properties.AppendLine();
                properties.AppendLine("SIZE INFORMATION:");

                // Get size info
                query = $@"
                    USE [{databaseName}];
                    SELECT 
                        type_desc,
                        name as FileName,
                        physical_name as PhysicalPath,
                        CAST(size * 8.0 / 1024 as decimal(10,2)) as SizeMB,
                        CAST(CASE WHEN max_size = -1 THEN -1 ELSE max_size * 8.0 / 1024 END as decimal(10,2)) as MaxSizeMB,
                        growth,
                        is_percent_growth
                    FROM sys.database_files
                    ORDER BY type";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.AppendLine($"  File: {reader["FileName"]}");
                            properties.AppendLine($"    Type: {reader["type_desc"]}");
                            properties.AppendLine($"    Path: {reader["PhysicalPath"]}");
                            properties.AppendLine($"    Size: {reader["SizeMB"]} MB");
                            
                            var maxSize = reader["MaxSizeMB"].ToString();
                            properties.AppendLine($"    Max Size: {(maxSize == "-1" ? "Unlimited" : maxSize + " MB")}");
                            
                            var growth = reader["growth"].ToString();
                            var isPercent = (bool)reader["is_percent_growth"];
                            properties.AppendLine($"    Growth: {growth} {(isPercent ? "%" : "pages")}");
                            properties.AppendLine();
                        }
                    }
                }

                // Get table count
                query = $@"
                    USE [{databaseName}];
                    SELECT 
                        COUNT(*) as TableCount
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = 'BASE TABLE'";

                using (var command = new SqlCommand(query, connection))
                {
                    var tableCount = command.ExecuteScalar();
                    properties.AppendLine($"OBJECT COUNTS:");
                    properties.AppendLine($"  User Tables: {tableCount}");
                }

                // Get view count
                query = $@"
                    USE [{databaseName}];
                    SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped = 0";

                using (var command = new SqlCommand(query, connection))
                {
                    var viewCount = command.ExecuteScalar();
                    properties.AppendLine($"  User Views: {viewCount}");
                }

                // Get stored procedure count
                query = $@"
                    USE [{databaseName}];
                    SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0";

                using (var command = new SqlCommand(query, connection))
                {
                    var procCount = command.ExecuteScalar();
                    properties.AppendLine($"  Stored Procedures: {procCount}");
                }

                propertiesTextBox.Text = properties.ToString();
            }
            catch (Exception ex)
            {
                propertiesTextBox.Text = $"Error loading properties: {ex.Message}";
            }
        }
    }
}
