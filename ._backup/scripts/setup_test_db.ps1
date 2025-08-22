# Setup test database for SQL Server Manager testing
$connectionString = "Server=(localdb)\MSSQLLocalDB;Integrated Security=true;"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
    $connection.Open()
    Write-Host "Connected to LocalDB"
    
    # Create test database
    $command = $connection.CreateCommand()
    $command.CommandText = @"
        IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TestEditingDB')
        BEGIN
            CREATE DATABASE TestEditingDB
        END
"@
    $command.ExecuteNonQuery()
    Write-Host "TestEditingDB database created/verified"
    
    # Use the database and create test table
    $command.CommandText = @"
        USE TestEditingDB;
        
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees')
        BEGIN
            CREATE TABLE Employees (
                ID int IDENTITY(1,1) PRIMARY KEY,
                FirstName nvarchar(50) NOT NULL,
                LastName nvarchar(50) NOT NULL,
                Email nvarchar(100),
                Salary decimal(10,2),
                IsActive bit DEFAULT 1,
                HireDate datetime2 DEFAULT GETDATE()
            )
        END
"@
    $command.ExecuteNonQuery()
    Write-Host "Employees table created/verified"
    
    # Insert sample data
    $command.CommandText = @"
        USE TestEditingDB;
        
        IF NOT EXISTS (SELECT * FROM Employees)
        BEGIN
            INSERT INTO Employees (FirstName, LastName, Email, Salary, IsActive, HireDate) VALUES
            ('John', 'Doe', 'john.doe@email.com', 50000.00, 1, '2023-01-15'),
            ('Jane', 'Smith', 'jane.smith@email.com', 55000.00, 1, '2023-02-20'),
            ('Bob', 'Wilson', 'bob.wilson@email.com', 48000.00, 0, '2023-03-10')
        END
"@
    $command.ExecuteNonQuery()
    Write-Host "Sample data inserted"
    
    # Verify the setup
    $command.CommandText = "USE TestEditingDB; SELECT COUNT(*) FROM Employees"
    $count = $command.ExecuteScalar()
    Write-Host "Total employees in table: $count"
    
    $command.CommandText = "USE TestEditingDB; SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Employees' ORDER BY ORDINAL_POSITION"
    $reader = $command.ExecuteReader()
    Write-Host "Column names:"
    while ($reader.Read()) {
        Write-Host "  $($reader.GetString(0))"
    }
    $reader.Close()
    
} catch {
    Write-Error "Error: $($_.Exception.Message)"
} finally {
    if ($connection) {
        $connection.Close()
    }
}
