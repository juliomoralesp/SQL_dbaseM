using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SqlServerManager
{
    public class TableEditorDialog : Form
    {
        private SqlConnection connection;
        private string databaseName;
        private string tableName;
        private bool isEditMode;
        
        private TextBox tableNameTextBox;
        private DataGridView columnsGridView;
        private Button addColumnButton;
        private Button removeColumnButton;
        private Button moveUpButton;
        private Button moveDownButton;
        private Button okButton;
        private Button cancelButton;
        private Label tableNameLabel;
        private Label columnsLabel;
        private Panel buttonPanel;
        private CheckBox primaryKeyCheckBox;
        
        public string TableName => tableNameTextBox.Text;
        public List<ColumnDefinition> Columns { get; private set; }
        
        public TableEditorDialog(SqlConnection connection, string databaseName, string tableName = null)
        {
            this.connection = connection;
            this.databaseName = databaseName;
            this.tableName = tableName;
            this.isEditMode = !string.IsNullOrEmpty(tableName);
            
            Columns = new List<ColumnDefinition>();
            InitializeComponent();
            LoadDataTypes();
            
            if (isEditMode)
            {
                LoadExistingTable();
            }
            else
            {
                AddDefaultColumn();
            }
            
            // Apply theme and font
            ThemeManager.ApplyThemeToDialog(this);
            FontManager.ApplyFontSize(this, FontManager.CurrentFontSize / 10f);
        }
        
        private void InitializeComponent()
        {
            this.Text = isEditMode ? $"Edit Table: {tableName}" : "Create New Table";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(700, 500);
            
            // Table name
            tableNameLabel = new Label();
            tableNameLabel.Text = "Table Name:";
            tableNameLabel.Location = new Point(20, 20);
            tableNameLabel.Size = new Size(100, 25);
            tableNameLabel.Font = FontManager.GetScaledFont(9, FontStyle.Bold);
            
            tableNameTextBox = new TextBox();
            tableNameTextBox.Location = new Point(130, 18);
            tableNameTextBox.Size = new Size(300, 25);
            tableNameTextBox.Text = tableName ?? "";
            tableNameTextBox.Enabled = !isEditMode;
            tableNameTextBox.Font = FontManager.GetScaledFont(9);
            
            // Columns label
            columnsLabel = new Label();
            columnsLabel.Text = "Columns:";
            columnsLabel.Location = new Point(20, 60);
            columnsLabel.Size = new Size(100, 25);
            columnsLabel.Font = FontManager.GetScaledFont(9, FontStyle.Bold);
            
            // Columns grid
            columnsGridView = new DataGridView();
            columnsGridView.Location = new Point(20, 90);
            columnsGridView.Size = new Size(740, 380);
            columnsGridView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            columnsGridView.AllowUserToAddRows = false;
            columnsGridView.AllowUserToDeleteRows = false;
            columnsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            columnsGridView.MultiSelect = false;
            columnsGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            
            // Setup columns
            SetupGridColumns();
            
            // Button panel
            buttonPanel = new Panel();
            buttonPanel.Location = new Point(770, 90);
            buttonPanel.Size = new Size(100, 200);
            buttonPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            
            addColumnButton = new Button();
            addColumnButton.Text = "Add";
            addColumnButton.Location = new Point(0, 0);
            addColumnButton.Size = new Size(100, 30);
            addColumnButton.Click += AddColumnButton_Click;
            addColumnButton.Font = FontManager.GetScaledFont(9);
            
            removeColumnButton = new Button();
            removeColumnButton.Text = "Remove";
            removeColumnButton.Location = new Point(0, 40);
            removeColumnButton.Size = new Size(100, 30);
            removeColumnButton.Click += RemoveColumnButton_Click;
            removeColumnButton.Font = FontManager.GetScaledFont(9);
            
            moveUpButton = new Button();
            moveUpButton.Text = "Move Up";
            moveUpButton.Location = new Point(0, 80);
            moveUpButton.Size = new Size(100, 30);
            moveUpButton.Click += MoveUpButton_Click;
            moveUpButton.Font = FontManager.GetScaledFont(9);
            
            moveDownButton = new Button();
            moveDownButton.Text = "Move Down";
            moveDownButton.Location = new Point(0, 120);
            moveDownButton.Size = new Size(100, 30);
            moveDownButton.Click += MoveDownButton_Click;
            moveDownButton.Font = FontManager.GetScaledFont(9);
            
            buttonPanel.Controls.AddRange(new Control[] { 
                addColumnButton, removeColumnButton, moveUpButton, moveDownButton 
            });
            
            // Primary key checkbox (for new tables)
            primaryKeyCheckBox = new CheckBox();
            primaryKeyCheckBox.Text = "Add auto-increment ID as primary key";
            primaryKeyCheckBox.Location = new Point(20, 490);
            primaryKeyCheckBox.Size = new Size(300, 25);
            primaryKeyCheckBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            primaryKeyCheckBox.Checked = true;
            primaryKeyCheckBox.Visible = !isEditMode;
            primaryKeyCheckBox.Font = FontManager.GetScaledFont(9);
            
            // OK and Cancel buttons
            okButton = new Button();
            okButton.Text = isEditMode ? "Save Changes" : "Create Table";
            okButton.Location = new Point(590, 520);
            okButton.Size = new Size(120, 30);
            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += OkButton_Click;
            okButton.Font = FontManager.GetScaledFont(9);
            
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(720, 520);
            cancelButton.Size = new Size(80, 30);
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Font = FontManager.GetScaledFont(9);
            
            // Add controls
            this.Controls.AddRange(new Control[] {
                tableNameLabel, tableNameTextBox,
                columnsLabel, columnsGridView,
                buttonPanel, primaryKeyCheckBox,
                okButton, cancelButton
            });
            
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        
        private void SetupGridColumns()
        {
            columnsGridView.Columns.Clear();
            
            var nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.Name = "ColumnName";
            nameColumn.HeaderText = "Column Name";
            nameColumn.Width = 150;
            
            var typeColumn = new DataGridViewComboBoxColumn();
            typeColumn.Name = "DataType";
            typeColumn.HeaderText = "Data Type";
            typeColumn.Width = 120;
            
            var lengthColumn = new DataGridViewTextBoxColumn();
            lengthColumn.Name = "Length";
            lengthColumn.HeaderText = "Length/Precision";
            lengthColumn.Width = 100;
            
            var nullableColumn = new DataGridViewCheckBoxColumn();
            nullableColumn.Name = "Nullable";
            nullableColumn.HeaderText = "Allow Nulls";
            nullableColumn.Width = 80;
            
            var defaultColumn = new DataGridViewTextBoxColumn();
            defaultColumn.Name = "DefaultValue";
            defaultColumn.HeaderText = "Default Value";
            defaultColumn.Width = 120;
            
            var identityColumn = new DataGridViewCheckBoxColumn();
            identityColumn.Name = "Identity";
            identityColumn.HeaderText = "Identity";
            identityColumn.Width = 70;
            
            var primaryKeyColumn = new DataGridViewCheckBoxColumn();
            primaryKeyColumn.Name = "PrimaryKey";
            primaryKeyColumn.HeaderText = "Primary Key";
            primaryKeyColumn.Width = 90;
            
            columnsGridView.Columns.AddRange(new DataGridViewColumn[] {
                nameColumn, typeColumn, lengthColumn, nullableColumn,
                defaultColumn, identityColumn, primaryKeyColumn
            });
        }
        
        private void LoadDataTypes()
        {
            var dataTypes = new string[] {
                "int", "bigint", "smallint", "tinyint", "bit",
                "decimal", "numeric", "money", "smallmoney",
                "float", "real",
                "datetime", "datetime2", "date", "time", "timestamp",
                "char", "varchar", "text", "nchar", "nvarchar", "ntext",
                "binary", "varbinary", "image",
                "uniqueidentifier", "xml"
            };
            
            var typeColumn = (DataGridViewComboBoxColumn)columnsGridView.Columns["DataType"];
            typeColumn.Items.AddRange(dataTypes);
        }
        
        private void AddDefaultColumn()
        {
            columnsGridView.Rows.Add("Column1", "nvarchar", "50", true, "", false, false);
        }
        
        private void LoadExistingTable()
        {
            try
            {
                string query = @"
                    SELECT 
                        c.COLUMN_NAME,
                        c.DATA_TYPE,
                        c.CHARACTER_MAXIMUM_LENGTH,
                        c.NUMERIC_PRECISION,
                        c.NUMERIC_SCALE,
                        CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END as IS_NULLABLE,
                        c.COLUMN_DEFAULT,
                        CASE WHEN COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1 THEN 1 ELSE 0 END as IS_IDENTITY,
                        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IS_PRIMARY_KEY
                    FROM INFORMATION_SCHEMA.COLUMNS c
                    LEFT JOIN (
                        SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                            ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                        WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
                        AND c.TABLE_NAME = pk.TABLE_NAME 
                        AND c.COLUMN_NAME = pk.COLUMN_NAME
                    WHERE c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = @tableName
                    ORDER BY c.ORDINAL_POSITION";
                
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader["COLUMN_NAME"].ToString();
                            string dataType = reader["DATA_TYPE"].ToString();
                            string length = "";
                            
                            if (reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                            {
                                length = reader["CHARACTER_MAXIMUM_LENGTH"].ToString();
                            }
                            else if (reader["NUMERIC_PRECISION"] != DBNull.Value)
                            {
                                length = reader["NUMERIC_PRECISION"].ToString();
                                if (reader["NUMERIC_SCALE"] != DBNull.Value && Convert.ToInt32(reader["NUMERIC_SCALE"]) > 0)
                                {
                                    length += "," + reader["NUMERIC_SCALE"].ToString();
                                }
                            }
                            
                            bool isNullable = Convert.ToBoolean(reader["IS_NULLABLE"]);
                            string defaultValue = reader["COLUMN_DEFAULT"]?.ToString() ?? "";
                            bool isIdentity = Convert.ToBoolean(reader["IS_IDENTITY"]);
                            bool isPrimaryKey = Convert.ToBoolean(reader["IS_PRIMARY_KEY"]);
                            
                            columnsGridView.Rows.Add(columnName, dataType, length, isNullable, 
                                defaultValue, isIdentity, isPrimaryKey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table structure: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void AddColumnButton_Click(object sender, EventArgs e)
        {
            string newColumnName = $"Column{columnsGridView.Rows.Count + 1}";
            columnsGridView.Rows.Add(newColumnName, "nvarchar", "50", true, "", false, false);
        }
        
        private void RemoveColumnButton_Click(object sender, EventArgs e)
        {
            if (columnsGridView.CurrentRow != null && columnsGridView.Rows.Count > 1)
            {
                columnsGridView.Rows.Remove(columnsGridView.CurrentRow);
            }
        }
        
        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            if (columnsGridView.CurrentRow != null && columnsGridView.CurrentRow.Index > 0)
            {
                int index = columnsGridView.CurrentRow.Index;
                DataGridViewRow row = columnsGridView.Rows[index];
                columnsGridView.Rows.Remove(row);
                columnsGridView.Rows.Insert(index - 1, row);
                columnsGridView.CurrentCell = columnsGridView.Rows[index - 1].Cells[0];
            }
        }
        
        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            if (columnsGridView.CurrentRow != null && 
                columnsGridView.CurrentRow.Index < columnsGridView.Rows.Count - 1)
            {
                int index = columnsGridView.CurrentRow.Index;
                DataGridViewRow row = columnsGridView.Rows[index];
                columnsGridView.Rows.Remove(row);
                columnsGridView.Rows.Insert(index + 1, row);
                columnsGridView.CurrentCell = columnsGridView.Rows[index + 1].Cells[0];
            }
        }
        
        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tableNameTextBox.Text))
            {
                MessageBox.Show("Please enter a table name.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            
            if (columnsGridView.Rows.Count == 0)
            {
                MessageBox.Show("Please add at least one column.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            
            // Collect column definitions
            Columns.Clear();
            foreach (DataGridViewRow row in columnsGridView.Rows)
            {
                var column = new ColumnDefinition
                {
                    Name = row.Cells["ColumnName"].Value?.ToString() ?? "",
                    DataType = row.Cells["DataType"].Value?.ToString() ?? "nvarchar",
                    Length = row.Cells["Length"].Value?.ToString() ?? "",
                    IsNullable = Convert.ToBoolean(row.Cells["Nullable"].Value),
                    DefaultValue = row.Cells["DefaultValue"].Value?.ToString() ?? "",
                    IsIdentity = Convert.ToBoolean(row.Cells["Identity"].Value),
                    IsPrimaryKey = Convert.ToBoolean(row.Cells["PrimaryKey"].Value)
                };
                
                if (string.IsNullOrWhiteSpace(column.Name))
                {
                    MessageBox.Show("All columns must have a name.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                
                Columns.Add(column);
            }
            
            try
            {
                if (isEditMode)
                {
                    ExecuteTableModification();
                }
                else
                {
                    ExecuteTableCreation();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
        
        private void ExecuteTableCreation()
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine($"USE [{databaseName}];");
            sql.AppendLine($"CREATE TABLE [dbo].[{tableNameTextBox.Text}] (");
            
            // Add auto-increment ID if requested
            if (primaryKeyCheckBox.Checked && !Columns.Any(c => c.IsPrimaryKey))
            {
                sql.AppendLine("    [Id] INT IDENTITY(1,1) PRIMARY KEY,");
            }
            
            // Add columns
            for (int i = 0; i < Columns.Count; i++)
            {
                var column = Columns[i];
                sql.Append($"    [{column.Name}] {column.DataType}");
                
                // Add length/precision
                if (!string.IsNullOrEmpty(column.Length) && NeedsLength(column.DataType))
                {
                    sql.Append($"({column.Length})");
                }
                
                // Add identity
                if (column.IsIdentity)
                {
                    sql.Append(" IDENTITY(1,1)");
                }
                
                // Add nullable
                sql.Append(column.IsNullable ? " NULL" : " NOT NULL");
                
                // Add default value
                if (!string.IsNullOrEmpty(column.DefaultValue))
                {
                    sql.Append($" DEFAULT {column.DefaultValue}");
                }
                
                // Add primary key
                if (column.IsPrimaryKey)
                {
                    sql.Append(" PRIMARY KEY");
                }
                
                if (i < Columns.Count - 1)
                {
                    sql.AppendLine(",");
                }
                else
                {
                    sql.AppendLine();
                }
            }
            
            sql.AppendLine(");");
            
            using (var command = new SqlCommand(sql.ToString(), connection))
            {
                command.ExecuteNonQuery();
            }
            
            MessageBox.Show($"Table '{tableNameTextBox.Text}' created successfully.", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void ExecuteTableModification()
        {
            try
            {
                // Get current table structure from database
                var currentColumns = GetCurrentTableStructure();
                var newColumns = Columns.ToList();
                
                // Generate ALTER TABLE statements
                var alterStatements = GenerateAlterTableStatements(currentColumns, newColumns);
                
                if (alterStatements.Count == 0)
                {
                    MessageBox.Show("No changes detected in table structure.", "Information", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Show confirmation dialog with changes
                var confirmationDialog = new TableModificationConfirmationDialog(alterStatements);
                if (confirmationDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                
                // Execute ALTER TABLE statements in a transaction
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var statement in alterStatements)
                        {
                            using (var command = new SqlCommand(statement, connection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        
                        transaction.Commit();
                        
                        MessageBox.Show($"Table '{tableName}' modified successfully.\n\n" +
                            $"Executed {alterStatements.Count} ALTER TABLE statement(s).", 
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Failed to modify table: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error modifying table: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
        
        private List<ColumnDefinition> GetCurrentTableStructure()
        {
            var columns = new List<ColumnDefinition>();
            
            string sql = @"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as IsIdentity,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey,
                    c.ORDINAL_POSITION
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                        AND ku.TABLE_NAME = @tableName
                ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
                WHERE c.TABLE_NAME = @tableName
                    AND c.TABLE_SCHEMA = 'dbo'
                ORDER BY c.ORDINAL_POSITION";
                
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var column = new ColumnDefinition
                        {
                            Name = reader["COLUMN_NAME"].ToString(),
                            DataType = reader["DATA_TYPE"].ToString(),
                            IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                            DefaultValue = reader["COLUMN_DEFAULT"]?.ToString(),
                            IsIdentity = Convert.ToBoolean(reader["IsIdentity"]),
                            IsPrimaryKey = Convert.ToBoolean(reader["IsPrimaryKey"])
                        };
                        
                        // Handle length/precision
                        if (reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                        {
                            column.Length = reader["CHARACTER_MAXIMUM_LENGTH"].ToString();
                        }
                        else if (reader["NUMERIC_PRECISION"] != DBNull.Value)
                        {
                            var precision = reader["NUMERIC_PRECISION"].ToString();
                            var scale = reader["NUMERIC_SCALE"].ToString();
                            if (scale != "0")
                            {
                                column.Length = $"{precision},{scale}";
                            }
                            else
                            {
                                column.Length = precision;
                            }
                        }
                        
                        columns.Add(column);
                    }
                }
            }
            
            return columns;
        }
        
        private List<string> GenerateAlterTableStatements(List<ColumnDefinition> currentColumns, List<ColumnDefinition> newColumns)
        {
            var statements = new List<string>();
            var tableName = this.tableName;
            
            // Find columns to drop
            foreach (var currentColumn in currentColumns)
            {
                if (!newColumns.Any(nc => nc.Name.Equals(currentColumn.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    statements.Add($"ALTER TABLE [dbo].[{tableName}] DROP COLUMN [{currentColumn.Name}];");
                }
            }
            
            // Find columns to add or modify
            foreach (var newColumn in newColumns)
            {
                var currentColumn = currentColumns.FirstOrDefault(cc => 
                    cc.Name.Equals(newColumn.Name, StringComparison.OrdinalIgnoreCase));
                    
                if (currentColumn == null)
                {
                    // Add new column
                    var columnDef = BuildColumnDefinition(newColumn);
                    statements.Add($"ALTER TABLE [dbo].[{tableName}] ADD [{newColumn.Name}] {columnDef};");
                }
                else
                {
                    // Check if column needs modification
                    if (NeedsColumnModification(currentColumn, newColumn))
                    {
                        // For complex modifications, we might need to drop and recreate
                        // For simple modifications, use ALTER COLUMN
                        if (CanUseAlterColumn(currentColumn, newColumn))
                        {
                            var columnDef = BuildColumnDefinition(newColumn, forAlter: true);
                            statements.Add($"ALTER TABLE [dbo].[{tableName}] ALTER COLUMN [{newColumn.Name}] {columnDef};");
                        }
                        else
                        {
                            // Complex modification - requires drop and add
                            statements.Add($"-- Warning: Complex modification for column [{newColumn.Name}] may require manual intervention");
                            statements.Add($"-- Current: {BuildColumnDefinition(currentColumn)}");
                            statements.Add($"-- Desired: {BuildColumnDefinition(newColumn)}");
                        }
                    }
                }
            }
            
            return statements;
        }
        
        private string BuildColumnDefinition(ColumnDefinition column, bool forAlter = false)
        {
            var def = new StringBuilder();
            def.Append(column.DataType.ToUpper());
            
            // Add length/precision
            if (!string.IsNullOrEmpty(column.Length) && NeedsLength(column.DataType))
            {
                def.Append($"({column.Length})");
            }
            
            // Add identity (only for new columns, not ALTER COLUMN)
            if (column.IsIdentity && !forAlter)
            {
                def.Append(" IDENTITY(1,1)");
            }
            
            // Add nullable
            def.Append(column.IsNullable ? " NULL" : " NOT NULL");
            
            // Add default value (only for new columns)
            if (!string.IsNullOrEmpty(column.DefaultValue) && !forAlter)
            {
                def.Append($" DEFAULT {column.DefaultValue}");
            }
            
            return def.ToString();
        }
        
        private bool NeedsColumnModification(ColumnDefinition current, ColumnDefinition newCol)
        {
            return !current.DataType.Equals(newCol.DataType, StringComparison.OrdinalIgnoreCase) ||
                   !string.Equals(current.Length ?? "", newCol.Length ?? "") ||
                   current.IsNullable != newCol.IsNullable ||
                   !string.Equals(current.DefaultValue ?? "", newCol.DefaultValue ?? "");
        }
        
        private bool CanUseAlterColumn(ColumnDefinition current, ColumnDefinition newCol)
        {
            // Can't use ALTER COLUMN if changing identity, primary key, or certain data type conversions
            if (current.IsIdentity != newCol.IsIdentity || current.IsPrimaryKey != newCol.IsPrimaryKey)
            {
                return false;
            }
            
            // Check for incompatible data type changes
            var incompatibleChanges = new[]
            {
                ("int", "varchar"), ("varchar", "int"),
                ("datetime", "int"), ("int", "datetime")
                // Add more incompatible combinations as needed
            };
            
            var currentType = current.DataType.ToLower();
            var newType = newCol.DataType.ToLower();
            
            return !incompatibleChanges.Any(ic => 
                (ic.Item1 == currentType && ic.Item2 == newType) ||
                (ic.Item2 == currentType && ic.Item1 == newType));
        }
        
        private bool NeedsLength(string dataType)
        {
            var typesWithLength = new[] { 
                "char", "varchar", "nchar", "nvarchar", 
                "binary", "varbinary", "decimal", "numeric" 
            };
            return typesWithLength.Contains(dataType.ToLower());
        }
    }
    
    public class ColumnDefinition
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Length { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}
