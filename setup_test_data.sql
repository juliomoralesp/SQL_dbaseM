-- Test Script for SQL Server LocalDB
-- Run this to create sample data for testing the Table Data Editor

-- Create a test database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TestEditingDB')
BEGIN
    CREATE DATABASE TestEditingDB;
END
GO

USE TestEditingDB;
GO

-- Drop tables if they exist (for clean re-run)
IF OBJECT_ID('Employees', 'U') IS NOT NULL DROP TABLE Employees;
IF OBJECT_ID('TestDataTypes', 'U') IS NOT NULL DROP TABLE TestDataTypes;
GO

-- Create Employees table
CREATE TABLE Employees (
    EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100),
    Department NVARCHAR(50),
    Salary DECIMAL(10,2),
    HireDate DATE DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);
GO

-- Insert sample data into Employees
INSERT INTO Employees (FirstName, LastName, Email, Department, Salary, IsActive)
VALUES 
    ('John', 'Doe', 'john.doe@company.com', 'IT', 75000.00, 1),
    ('Jane', 'Smith', 'jane.smith@company.com', 'HR', 65000.00, 1),
    ('Bob', 'Johnson', 'bob.johnson@company.com', 'Finance', 70000.00, 1),
    ('Alice', 'Brown', 'alice.brown@company.com', 'Marketing', 60000.00, 0),
    ('Charlie', 'Davis', 'charlie.davis@company.com', 'IT', 80000.00, 1);
GO

-- Create TestDataTypes table for testing different data types
CREATE TABLE TestDataTypes (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    TextValue NVARCHAR(100),
    IntValue INT,
    DecimalValue DECIMAL(10,2),
    DateValue DATE,
    DateTimeValue DATETIME,
    BitValue BIT,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- Insert sample data into TestDataTypes
INSERT INTO TestDataTypes (TextValue, IntValue, DecimalValue, DateValue, DateTimeValue, BitValue)
VALUES 
    ('Sample Text 1', 42, 123.45, '2024-01-15', '2024-01-15 10:30:00', 1),
    ('Sample Text 2', 100, 999.99, '2024-02-20', '2024-02-20 14:45:00', 0),
    ('Sample Text 3', -5, 0.01, '2024-03-10', '2024-03-10 09:15:00', 1);
GO

-- Verify the data
SELECT 'Employees Table:' as Info;
SELECT * FROM Employees;

SELECT 'TestDataTypes Table:' as Info;
SELECT * FROM TestDataTypes;

SELECT 'Setup complete! You can now test the Table Data Editor.' as Message;
