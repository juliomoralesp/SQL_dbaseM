namespace SqlServerManager.Mobile.Models
{
    public class DatabaseInfo
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Collation { get; set; }
        public long Size { get; set; }
        public string Status { get; set; }
    }

    public class TableInfo
    {
        public string TableSchema { get; set; }
        public string TableName { get; set; }
        public string TableType { get; set; }
        public int RowCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public class ColumnInfo
    {
        public string ColumnName { get; set; }
        public int OrdinalPosition { get; set; }
        public string DataType { get; set; }
        public int? MaxLength { get; set; }
        public byte? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
        public string IsNullable { get; set; }
        public string DefaultValue { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsPrimaryKey { get; set; }
    }

    public class ConnectionInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ServerName { get; set; }
        public string AuthType { get; set; }
        public string Username { get; set; }
        public string Database { get; set; }
        public string DisplayName { get; set; }
        public DateTime LastUsed { get; set; } = DateTime.Now;
    }

    public class DatabaseProperties
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Collation { get; set; }
        public string RecoveryModel { get; set; }
        public string State { get; set; }
        public long DataFileSize { get; set; }
        public long LogFileSize { get; set; }
        public int TableCount { get; set; }
        public int ViewCount { get; set; }
        public int StoredProcedureCount { get; set; }
        public int FunctionCount { get; set; }
    }
}
