-- Quick Test Database Setup for Data Editing
-- Run these commands in SQL Server Management Studio or your SQL client

-- Create a test database (optional)
-- CREATE DATABASE TestEditingDB;
-- USE TestEditingDB;

-- Create a simple test table
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

-- Insert sample data
INSERT INTO Employees (FirstName, LastName, Email, Department, Salary, IsActive)
VALUES 
    ('John', 'Doe', 'john.doe@company.com', 'IT', 75000.00, 1),
    ('Jane', 'Smith', 'jane.smith@company.com', 'HR', 65000.00, 1),
    ('Bob', 'Johnson', 'bob.johnson@company.com', 'Finance', 70000.00, 1),
    ('Alice', 'Brown', 'alice.brown@company.com', 'Marketing', 60000.00, 0),
    ('Charlie', 'Davis', 'charlie.davis@company.com', 'IT', 80000.00, 1);

-- View the data
SELECT * FROM Employees;

-- Test different data types table
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

-- Insert test data
INSERT INTO TestDataTypes (TextValue, IntValue, DecimalValue, DateValue, DateTimeValue, BitValue)
VALUES 
    ('Sample Text 1', 42, 123.45, '2024-01-15', '2024-01-15 10:30:00', 1),
    ('Sample Text 2', 100, 999.99, '2024-02-20', '2024-02-20 14:45:00', 0),
    ('Sample Text 3', -5, 0.01, '2024-03-10', '2024-03-10 09:15:00', 1);
