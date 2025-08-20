using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace SqlServerManager
{
    public partial class TableDataEditor : Form
    {
        private SqlConnection connection;
        private string databaseName;
        private string tableName;
        private string schemaName;
        private DataTable originalData;
        private DataTable currentData;
        private List<string> primaryKeyColumns;
        private Dictionary<string, string> columnTypes;
        
        private DataGridView dataGridView;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripButton saveButton;
        private ToolStripButton refreshButton;
        private ToolStripButton addRowButton;
        private ToolStripButton deleteRowButton;
        private ToolStripButton cancelButton;
        
        private bool hasChanges = false;
        private HashSet<int> modifiedRows = new HashSet<int>();
        private HashSet<int> newRows = new HashSet<int>();
        private HashSet<int> deletedRows = new HashSet<int>();

        public TableDataEditor(SqlConnection conn, string dbName, string schema, string table)
        {
            connection = conn;
            databaseName = dbName;
            schemaName = schema;
            tableName = table;
            primaryKeyColumns = new List<string>();
            columnTypes = new Dictionary<string, string>();
            
            InitializeComponent();
            _ = Task.Run(async () =>
            {
                await LoadTableSchemaAsync().ConfigureAwait(false);
                await LoadTableDataAsync().ConfigureAwait(false);
            });
        }

        private void InitializeComponent()
        {
            this.Text = $"Edit Data: {schemaName}.{tableName}";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 400);

            // Create ToolStrip
            toolStrip = new ToolStrip();
            
            saveButton = new ToolStripButton("Save Changes", null, SaveChanges_Click);
            saveButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            saveButton.Enabled = false;
            
            refreshButton = new ToolStripButton("Refresh", null, Refresh_Click);
            refreshButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            addRowButton = new ToolStripButton("Add Row", null, AddRow_Click);
            addRowButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            deleteRowButton = new ToolStripButton("Delete Row", null, DeleteRow_Click);
            deleteRowButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            deleteRowButton.Enabled = false;
            
            cancelButton = new ToolStripButton("Cancel Changes", null, CancelChanges_Click);
            cancelButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            cancelButton.Enabled = false;

            toolStrip.Items.AddRange(new ToolStripItem[] {
                saveButton,
                new ToolStripSeparator(),
                refreshButton,
                new ToolStripSeparator(),
                addRowButton,
                deleteRowButton,
                new ToolStripSeparator(),
                cancelButton
            });

            // Create DataGridView
            dataGridView = new DataGridView();
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.MultiSelect = true;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dataGridView.RowHeadersVisible = true;
            dataGridView.EditMode = DataGridViewEditMode.EditOnEnter;
            
            // Event handlers
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            dataGridView.SelectionChanged += DataGridView_SelectionChanged;
            dataGridView.DataError += DataGridView_DataError;
            dataGridView.RowValidating += DataGridView_RowValidating;

            // Create StatusStrip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            statusStrip.Items.Add(statusLabel);

            // Add controls to form
            this.Controls.Add(dataGridView);
            this.Controls.Add(toolStrip);
            this.Controls.Add(statusStrip);
        }

        private async Task LoadTableSchemaAsync()
        {
            try
            {
                // Get primary key columns
                var pkQuery = $@"
                    USE [{databaseName}];
                    SELECT c.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc ON tc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
                    JOIN INFORMATION_SCHEMA.COLUMNS c ON cc.COLUMN_NAME = c.COLUMN_NAME 
                        AND cc.TABLE_NAME = c.TABLE_NAME AND cc.TABLE_SCHEMA = c.TABLE_SCHEMA
                    WHERE tc.TABLE_SCHEMA = @schema 
                        AND tc.TABLE_NAME = @table 
                        AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ORDER BY c.ORDINAL_POSITION";

                using (var command = new SqlCommand(pkQuery, connection))
                {
                    command.Parameters.AddWithValue("@schema", schemaName);
                    command.Parameters.AddWithValue("@table", tableName);
                    
                    LoggingService.LogInformation($"Loading primary keys for {schemaName}.{tableName}");
                    LoggingService.LogDebug($"PK Query: {pkQuery}");
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var pkColumn = reader["COLUMN_NAME"].ToString();
                            LoggingService.LogInformation($"Found primary key column: '{pkColumn}'");
                            primaryKeyColumns.Add(pkColumn);
                        }
                    }
                }

                // Get column data types
                var typeQuery = $@"
                    USE [{databaseName}];
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH,
                           NUMERIC_PRECISION, NUMERIC_SCALE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                    ORDER BY ORDINAL_POSITION";

                using (var command = new SqlCommand(typeQuery, connection))
                {
                    command.Parameters.AddWithValue("@schema", schemaName);
                    command.Parameters.AddWithValue("@table", tableName);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var columnName = reader["COLUMN_NAME"].ToString();
                            var dataType = reader["DATA_TYPE"].ToString();
                            var isNullable = reader["IS_NULLABLE"].ToString() == "YES";
                            var maxLength = reader["CHARACTER_MAXIMUM_LENGTH"];
                            
                            columnTypes[columnName] = dataType;
                        }
                    }
                }

                statusLabel.Text = $"Primary key columns: {string.Join(", ", primaryKeyColumns)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table schema: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadTableDataAsync()
        {
            try
            {
                // Update UI on UI thread
                if (this.InvokeRequired)
                {
                    this.Invoke(() => statusLabel.Text = "Loading data...");
                }
                else
                {
                    statusLabel.Text = "Loading data...";
                }
                
                var query = $@"USE [{databaseName}]; SELECT * FROM [{schemaName}].[{tableName}]";
                
                using (var command = new SqlCommand(query, connection))
                {
                    var adapter = new SqlDataAdapter(command);
                    originalData = new DataTable();
                    adapter.Fill(originalData);
                    
                    // Create a copy for editing
                    currentData = originalData.Copy();
                    
                    // Update UI on UI thread
                    if (dataGridView.InvokeRequired)
                    {
                        dataGridView.Invoke(() => 
                        {
                            dataGridView.DataSource = currentData;
                            ConfigureDataGridColumns();
                        });
                    }
                    else
                    {
                        dataGridView.DataSource = currentData;
                        ConfigureDataGridColumns();
                    }
                }

                hasChanges = false;
                modifiedRows.Clear();
                newRows.Clear();
                deletedRows.Clear();
                
                // Update UI on UI thread
                if (this.InvokeRequired)
                {
                    this.Invoke(() => 
                    {
                        UpdateButtonStates();
                        statusLabel.Text = $"Loaded {currentData.Rows.Count} rows";
                    });
                }
                else
                {
                    UpdateButtonStates();
                    statusLabel.Text = $"Loaded {currentData.Rows.Count} rows";
                }
            }
            catch (Exception ex)
            {
                // Update UI on UI thread
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(() => 
                    {
                        MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "Error loading data";
                    });
                }
                else
                {
                    MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Error loading data";
                }
            }
        }

        private void ConfigureDataGridColumns()
        {
            try
            {
                // Create a copy of the columns collection to avoid modification during enumeration
                var columns = dataGridView.Columns.Cast<DataGridViewColumn>().ToList();
                
                foreach (DataGridViewColumn column in columns)
                {
                    var columnName = column.Name;
                    
                    // Make primary key columns read-only for existing rows
                    if (primaryKeyColumns.Contains(columnName))
                    {
                        column.HeaderText = $"{columnName} (PK)";
                        column.DefaultCellStyle.BackColor = Color.LightGray;
                    }
                    
                    // Set appropriate cell types based on data type
                    if (columnTypes.ContainsKey(columnName))
                    {
                        var dataType = columnTypes[columnName].ToLower();
                        
                        switch (dataType)
                        {
                            case "bit":
                                // Convert to checkbox column
                                var checkColumn = new DataGridViewCheckBoxColumn();
                                checkColumn.Name = columnName;
                                checkColumn.HeaderText = columnName;
                                checkColumn.DataPropertyName = columnName;
                                checkColumn.TrueValue = true;
                                checkColumn.FalseValue = false;
                                checkColumn.IndeterminateValue = DBNull.Value;
                                
                                var columnIndex = dataGridView.Columns.IndexOf(column);
                                dataGridView.Columns.Remove(column);
                                dataGridView.Columns.Insert(columnIndex, checkColumn);
                                break;
                                
                            case "datetime":
                            case "datetime2":
                            case "date":
                            case "time":
                                column.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error configuring DataGrid columns: {ex.Message}");
            }
        }

        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                hasChanges = true;
                
                if (!newRows.Contains(e.RowIndex))
                {
                    modifiedRows.Add(e.RowIndex);
                }
                
                // Mark row header to indicate changes
                dataGridView.Rows[e.RowIndex].HeaderCell.Value = hasChanges ? "*" : "";
                
                UpdateButtonStates();
            }
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            deleteRowButton.Enabled = dataGridView.SelectedRows.Count > 0;
        }

        private void DataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            try
            {
                // Use thread-safe UI updates
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(() => 
                    {
                        var columnName = e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView.Columns.Count 
                            ? dataGridView.Columns[e.ColumnIndex].HeaderText 
                            : "Unknown";
                        MessageBox.Show($"Data error in column {columnName}: {e.Exception.Message}", 
                            "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    });
                }
                else
                {
                    var columnName = e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView.Columns.Count 
                        ? dataGridView.Columns[e.ColumnIndex].HeaderText 
                        : "Unknown";
                    MessageBox.Show($"Data error in column {columnName}: {e.Exception.Message}", 
                        "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch
            {
                // Fallback - just prevent the error from bubbling up
            }
            e.Cancel = true;
        }

        private void DataGridView_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Validate required primary key fields for new rows
            if (newRows.Contains(e.RowIndex))
            {
                var row = dataGridView.Rows[e.RowIndex];
                foreach (var pkColumn in primaryKeyColumns)
                {
                    var cell = row.Cells[pkColumn];
                    if (cell.Value == null || cell.Value == DBNull.Value || string.IsNullOrWhiteSpace(cell.Value.ToString()))
                    {
                        MessageBox.Show($"Primary key column '{pkColumn}' cannot be empty.", "Validation Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                        return;
                    }
                }
            }
        }

        private void AddRow_Click(object sender, EventArgs e)
        {
            try
            {
                var newRow = currentData.NewRow();
                currentData.Rows.Add(newRow);
                
                var rowIndex = currentData.Rows.Count - 1;
                newRows.Add(rowIndex);
                hasChanges = true;
                
                dataGridView.Rows[rowIndex].HeaderCell.Value = "+";
                dataGridView.CurrentCell = dataGridView.Rows[rowIndex].Cells[0];
                
                UpdateButtonStates();
                statusLabel.Text = "New row added. Enter data and save changes.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding row: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteRow_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete {dataGridView.SelectedRows.Count} row(s)?", 
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var rowsToDelete = new List<int>();
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    rowsToDelete.Add(row.Index);
                }

                rowsToDelete.Sort();
                rowsToDelete.Reverse(); // Delete from bottom to top to maintain indices

                foreach (var rowIndex in rowsToDelete)
                {
                    if (newRows.Contains(rowIndex))
                    {
                        // Remove from new rows and delete immediately
                        newRows.Remove(rowIndex);
                        currentData.Rows.RemoveAt(rowIndex);
                    }
                    else
                    {
                        // Mark for deletion
                        deletedRows.Add(rowIndex);
                        dataGridView.Rows[rowIndex].HeaderCell.Value = "X";
                        dataGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                        dataGridView.Rows[rowIndex].ReadOnly = true;
                    }
                }

                hasChanges = true;
                UpdateButtonStates();
                statusLabel.Text = $"Marked {rowsToDelete.Count} row(s) for deletion.";
            }
        }

        private async void SaveChanges_Click(object sender, EventArgs e)
        {
            if (!hasChanges) return;

            try
            {
                saveButton.Enabled = false;
                statusLabel.Text = "Saving changes...";

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Process deletions
                        await ProcessDeletions(transaction);
                        
                        // Process updates
                        await ProcessUpdates(transaction);
                        
                        // Process insertions
                        await ProcessInsertions(transaction);

                        transaction.Commit();
                        
                        // Reload data to reflect changes
                        LoadTableData();
                        
                        statusLabel.Text = "Changes saved successfully.";
                        MessageBox.Show("Changes saved successfully.", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error saving changes.";
            }
            finally
            {
                saveButton.Enabled = hasChanges;
            }
        }

        private async Task ProcessDeletions(SqlTransaction transaction)
        {
            foreach (var rowIndex in deletedRows)
            {
                if (rowIndex >= currentData.Rows.Count) continue;
                
                var row = currentData.Rows[rowIndex];
                var whereClause = BuildWhereClause(row);
                
                var deleteQuery = $"USE [{databaseName}]; DELETE FROM [{schemaName}].[{tableName}] WHERE {whereClause}";
                
                using (var command = new SqlCommand(deleteQuery, connection, transaction))
                {
                    AddWhereParameters(command, row);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task ProcessUpdates(SqlTransaction transaction)
        {
            foreach (var rowIndex in modifiedRows)
            {
                try
                {
                    if (rowIndex >= currentData.Rows.Count) continue;
                    
                    var currentRow = currentData.Rows[rowIndex];
                    var originalRow = originalData.Rows[rowIndex];
                    
                    // Enhanced DataTable debugging
                    LoggingService.LogInformation($"=== PROCESSING UPDATE FOR ROW {rowIndex} ===");
                    LoggingService.LogInformation($"Current DataTable info: Name='{currentData.TableName}', Columns={currentData.Columns.Count}");
                    LoggingService.LogInformation($"Original DataTable info: Name='{originalData.TableName}', Columns={originalData.Columns.Count}");
                    LoggingService.LogInformation($"Current row table info: Name='{currentRow.Table?.TableName ?? "NULL"}', Columns={currentRow.Table?.Columns.Count ?? 0}");
                    LoggingService.LogInformation($"Original row table info: Name='{originalRow.Table?.TableName ?? "NULL"}', Columns={originalRow.Table?.Columns.Count ?? 0}");
                    LoggingService.LogInformation($"Available columns: {string.Join(", ", currentData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                    LoggingService.LogInformation($"Primary keys: {string.Join(", ", primaryKeyColumns)}");
                    
                    var setClause = BuildSetClause(currentRow, originalRow);
                    if (string.IsNullOrEmpty(setClause)) continue; // No actual changes
                    
                    LoggingService.LogInformation($"Set clause built successfully: {setClause}");
                    
                    var whereClause = BuildWhereClause(originalRow);
                    LoggingService.LogInformation($"Where clause built successfully: {whereClause}");
                    
                    var updateQuery = $"USE [{databaseName}]; UPDATE [{schemaName}].[{tableName}] SET {setClause} WHERE {whereClause}";
                    LoggingService.LogInformation($"Executing update query: {updateQuery}");
                    
                    using (var command = new SqlCommand(updateQuery, connection, transaction))
                    {
                        LoggingService.LogInformation($"About to call AddSetParameters...");
                        AddSetParameters(command, currentRow, originalRow);
                        LoggingService.LogInformation($"AddSetParameters completed. About to call AddWhereParameters...");
                        AddWhereParameters(command, originalRow);
                        LoggingService.LogInformation($"AddWhereParameters completed. About to execute query...");
                        await command.ExecuteNonQueryAsync();
                        LoggingService.LogInformation($"Query executed successfully for row {rowIndex}");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Error processing update for row {rowIndex}: {ex.Message}", ex);
                    LoggingService.LogError($"Exception details: {ex}");
                    throw new Exception($"Error updating row {rowIndex}: {ex.Message}", ex);
                }
            }
        }

        private async Task ProcessInsertions(SqlTransaction transaction)
        {
            foreach (var rowIndex in newRows)
            {
                try
                {
                    if (rowIndex >= currentData.Rows.Count) continue;
                    
                    var row = currentData.Rows[rowIndex];
                    LoggingService.LogInformation($"Processing insertion for row {rowIndex}. Available columns: {string.Join(", ", currentData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                    
                    var columns = string.Join(", ", currentData.Columns.Cast<DataColumn>()
                        .Select(c => $"[{c.ColumnName}]"));
                    var values = string.Join(", ", currentData.Columns.Cast<DataColumn>()
                        .Select(c => SanitizeParameterName(c.ColumnName, "")));
                    
                    var insertQuery = $"USE [{databaseName}]; INSERT INTO [{schemaName}].[{tableName}] ({columns}) VALUES ({values})";
                    LoggingService.LogInformation($"Executing insert query: {insertQuery}");
                    
                    using (var command = new SqlCommand(insertQuery, connection, transaction))
                    {
                        foreach (DataColumn column in currentData.Columns)
                        {
                            try
                            {
                                if (!row.Table.Columns.Contains(column.ColumnName))
                                {
                                    LoggingService.LogError($"Column '{column.ColumnName}' does not exist in row. Available columns: {string.Join(", ", row.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                                    continue;
                                }
                                
                                var value = row[column] == DBNull.Value ? null : row[column];
                                var paramName = SanitizeParameterName(column.ColumnName, "");
                                LoggingService.LogDebug($"Adding INSERT parameter: {paramName} = {value ?? "NULL"}");
                                command.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                            }
                            catch (Exception ex)
                            {
                                LoggingService.LogError($"Error adding INSERT parameter for column '{column.ColumnName}': {ex.Message}", ex);
                                throw;
                            }
                        }
                        await command.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Error processing insertion for row {rowIndex}: {ex.Message}", ex);
                    throw new Exception($"Error inserting row {rowIndex}: {ex.Message}", ex);
                }
            }
        }

        private string BuildWhereClause(DataRow row)
        {
            if (primaryKeyColumns.Count > 0)
            {
                return string.Join(" AND ", primaryKeyColumns.Select(pk => {
                    var paramName = SanitizeParameterName(pk, "where_");
                    return $"[{pk}] = {paramName}";
                }));
            }
            else
            {
                // If no primary key, use all columns for WHERE clause
                return string.Join(" AND ", currentData.Columns.Cast<DataColumn>()
                    .Select(c => {
                        var paramName = SanitizeParameterName(c.ColumnName, "where_");
                        return $"[{c.ColumnName}] = {paramName}";
                    }));
            }
        }

        private void AddWhereParameters(SqlCommand command, DataRow row)
        {
            var columnsToUse = primaryKeyColumns.Count > 0 ? primaryKeyColumns : 
                currentData.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
            
            // Enhanced debugging
            LoggingService.LogInformation($"AddWhereParameters: Primary key columns: [{string.Join(", ", primaryKeyColumns)}]");
            LoggingService.LogInformation($"AddWhereParameters: Current data columns: [{string.Join(", ", currentData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");
            LoggingService.LogInformation($"AddWhereParameters: Row table columns: [{string.Join(", ", row.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");
            LoggingService.LogInformation($"AddWhereParameters: Columns to use: [{string.Join(", ", columnsToUse)}]");
            LoggingService.LogInformation($"AddWhereParameters: Row table name: '{row.Table.TableName}', Current data table name: '{currentData.TableName}'");
                
            foreach (var columnName in columnsToUse)
            {
                try
                {
                    LoggingService.LogDebug($"Processing column: '{columnName}'");
                    
                    // Check if column exists in the row
                    if (!row.Table.Columns.Contains(columnName))
                    {
                        LoggingService.LogError($"COLUMN MISMATCH: Column '{columnName}' does not exist in row's DataTable.");
                        LoggingService.LogError($"  - Row's table columns: [{string.Join(", ", row.Table.Columns.Cast<DataColumn>().Select(c => $"'{c.ColumnName}'"))}]");
                        LoggingService.LogError($"  - Expected column: '{columnName}'");
                        LoggingService.LogError($"  - Row's table: '{row.Table.TableName}' vs Current table: '{currentData.TableName}'");
                        LoggingService.LogError($"  - Are they the same reference? {object.ReferenceEquals(row.Table, currentData)}");
                        throw new ArgumentException($"Column '{columnName}' does not belong to table.");
                    }
                    
                    var value = row[columnName] == DBNull.Value ? null : row[columnName];
                    var paramName = SanitizeParameterName(columnName, "where_");
                    LoggingService.LogDebug($"Adding WHERE parameter: {paramName} = {value ?? "NULL"}");
                    command.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Error adding WHERE parameter for column '{columnName}': {ex.Message}", ex);
                    throw;
                }
            }
        }

        private string BuildSetClause(DataRow currentRow, DataRow originalRow)
        {
            var changes = new List<string>();
            
            foreach (DataColumn column in currentData.Columns)
            {
                try
                {
                    // Use column name instead of DataColumn object to avoid cross-table reference issues
                    var columnName = column.ColumnName;
                    
                    // Verify column exists in both rows
                    if (!currentRow.Table.Columns.Contains(columnName))
                    {
                        LoggingService.LogError($"Column '{columnName}' does not exist in current row table. Available columns: {string.Join(", ", currentRow.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                        continue;
                    }
                    
                    if (!originalRow.Table.Columns.Contains(columnName))
                    {
                        LoggingService.LogError($"Column '{columnName}' does not exist in original row table. Available columns: {string.Join(", ", originalRow.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                        continue;
                    }
                    
                    var currentValue = currentRow[columnName];
                    var originalValue = originalRow[columnName];
                    
                    if (!Equals(currentValue, originalValue))
                    {
                        var paramName = SanitizeParameterName(columnName, "set_");
                        changes.Add($"[{columnName}] = {paramName}");
                        LoggingService.LogDebug($"Change detected in column '{columnName}': '{originalValue}' -> '{currentValue}'");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Error processing column '{column.ColumnName}' in BuildSetClause: {ex.Message}", ex);
                    throw;
                }
            }
            
            return string.Join(", ", changes);
        }

        private void AddSetParameters(SqlCommand command, DataRow currentRow, DataRow originalRow)
        {
            foreach (DataColumn column in currentData.Columns)
            {
                var columnName = column.ColumnName;
                try
                {
                    // Verify column exists in both rows
                    if (!currentRow.Table.Columns.Contains(columnName))
                    {
                        LoggingService.LogError($"Column '{columnName}' does not exist in current row. Available columns: {string.Join(", ", currentRow.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                        continue;
                    }
                    
                    if (!originalRow.Table.Columns.Contains(columnName))
                    {
                        LoggingService.LogError($"Column '{columnName}' does not exist in original row. Available columns: {string.Join(", ", originalRow.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                        continue;
                    }
                    
                    // Use column names instead of DataColumn objects
                    var currentValue = currentRow[columnName];
                    var originalValue = originalRow[columnName];
                    
                    if (!Equals(currentValue, originalValue))
                    {
                        var value = currentValue == DBNull.Value ? null : currentValue;
                        var paramName = SanitizeParameterName(columnName, "set_");
                        LoggingService.LogDebug($"Adding SET parameter: {paramName} = {value ?? "NULL"} (was {originalValue ?? "NULL"})");
                        command.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Error adding SET parameter for column '{columnName}': {ex.Message}", ex);
                    throw;
                }
            }
        }

        private void CancelChanges_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to cancel all changes?", 
                "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                LoadTableData();
            }
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            if (hasChanges)
            {
                var result = MessageBox.Show("You have unsaved changes. Are you sure you want to refresh?", 
                    "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    
                if (result == DialogResult.No) return;
            }
            
            LoadTableData();
        }

        private void UpdateButtonStates()
        {
            saveButton.Enabled = hasChanges;
            cancelButton.Enabled = hasChanges;
        }
        
        private string SanitizeParameterName(string columnName, string prefix)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                columnName = "UnknownColumn";
            }
            
            // Clean column name: remove brackets, replace spaces with underscores
            var cleanName = columnName.Replace(" ", "_").Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "");
            
            // Remove any other problematic characters
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"[^a-zA-Z0-9_]", "_");
            
            // Ensure we have at least something to work with
            if (string.IsNullOrEmpty(cleanName))
            {
                cleanName = "UnknownColumn";
            }
            
            // Ensure it starts with a letter or underscore
            if (cleanName.Length > 0 && !char.IsLetter(cleanName[0]) && cleanName[0] != '_')
            {
                cleanName = "_" + cleanName;
            }
            
            // Construct the parameter name with prefix
            var paramName = $"@{prefix}{cleanName}";
            
            // Truncate if too long, but ensure we don't cut off in the middle of important parts
            if (paramName.Length > 128)
            {
                // Keep the prefix and truncate the column name part
                var maxColumnNameLength = 128 - prefix.Length - 1; // -1 for the @ symbol
                if (maxColumnNameLength > 0)
                {
                    cleanName = cleanName.Substring(0, maxColumnNameLength);
                    paramName = $"@{prefix}{cleanName}";
                }
                else
                {
                    // If prefix is too long, just truncate everything
                    paramName = paramName.Substring(0, 128);
                }
            }
            
            return paramName;
        }

        private async void LoadTableData()
        {
            try
            {
                statusLabel.Text = "Loading table data...";
                
                // Clear existing data
                currentData = null;
                originalData = null;
                primaryKeyColumns.Clear();
                newRows.Clear();
                modifiedRows.Clear();
                deletedRows.Clear();
                hasChanges = false;
                
                // Load primary key information
                await LoadPrimaryKeyInfo();
                
                // Load table data
                var query = $"USE [{databaseName}]; SELECT * FROM [{schemaName}].[{tableName}]";
                
                using (var adapter = new SqlDataAdapter(query, connection))
                {
                    currentData = new DataTable();
                    await Task.Run(() => adapter.Fill(currentData));
                }
                
                // Create a copy of original data
                originalData = currentData.Copy();
                
                // Bind to DataGridView
                dataGridView.DataSource = currentData;
                
                // Configure grid appearance
                ConfigureDataGridView();
                
                UpdateButtonStates();
                statusLabel.Text = $"Loaded {currentData.Rows.Count} rows";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table data: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error loading data";
            }
        }
        
        private async Task LoadPrimaryKeyInfo()
        {
            var query = $@"USE [{databaseName}];
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                WHERE TABLE_SCHEMA = '{schemaName}' 
                  AND TABLE_NAME = '{tableName}' 
                  AND CONSTRAINT_NAME LIKE 'PK_%'
                ORDER BY ORDINAL_POSITION";
                
            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        primaryKeyColumns.Add(reader.GetString(0));
                    }
                }
            }
        }
        
        private void ConfigureDataGridView()
        {
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                // Make primary key columns read-only if editing existing records
                if (primaryKeyColumns.Contains(column.Name))
                {
                    column.ReadOnly = false; // Allow editing for new records
                    column.DefaultCellStyle.BackColor = Color.LightYellow;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (hasChanges)
            {
                var result = MessageBox.Show("You have unsaved changes. Do you want to save them before closing?", 
                    "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    
                switch (result)
                {
                    case DialogResult.Yes:
                        SaveChanges_Click(this, EventArgs.Empty);
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }
            
            base.OnFormClosing(e);
        }
    }
}
