# SQL Database Manager

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download)
[![Windows](https://img.shields.io/badge/Platform-Windows-0078D6)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A modern, feature-rich Windows desktop application for managing Microsoft SQL Server databases with an intuitive graphical interface. Built with .NET 8.0 and Windows Forms, offering powerful database management capabilities with a focus on user experience.

## Features

### Connection Management
- **Windows Authentication** and **SQL Server Authentication** support
- Test connections before connecting
- Save successful connections for quick reconnection
- Connection status display in the toolbar

### Database Operations
- **List all databases** on the connected server
- **Create new databases** with a simple dialog
- **Rename existing databases**
- **Delete databases** (with confirmation)
- **View database properties** including:
  - General information (owner, creation date, collation, etc.)
  - File sizes and growth settings
  - Object counts (tables, views, stored procedures)

### Tables and Columns Viewer
- **Browse tables** in any database
- View table schemas and types
- **Inspect column details** including:
  - Column names and data types
  - Maximum length constraints
  - Nullable settings
  - Default values

## User Interface

### Main Window
The application uses a tabbed interface with two main tabs:

1. **Databases Tab**: Lists all databases with right-click context menu for operations
2. **Tables & Columns Tab**: Split view showing tables in the top panel and columns in the bottom panel

### Toolbar
- **Connect**: Opens the connection dialog
- **Disconnect**: Closes the current connection
- **Refresh**: Refreshes the current view
- **Connection Status**: Shows current connection state

## How to Use

### 1. Connect to SQL Server
1. Click the **Connect** button in the toolbar
2. Enter your server name (e.g., `localhost`, `.\SQLEXPRESS`, or server IP)
3. Choose authentication method:
   - **Windows Authentication**: Uses your current Windows credentials
   - **SQL Server Authentication**: Enter username and password
4. Optionally specify a default database
5. Click **Test Connection** to verify settings
6. Click **OK** to connect

### 2. Manage Databases
1. Once connected, all databases appear in the **Databases** tab
2. Right-click any database for operations:
   - Create new database
   - Rename database
   - Delete database
   - View properties
3. Double-click a database to view its tables

### 3. View Tables and Columns
1. Select a database in the **Databases** tab
2. Double-click to open in the **Tables & Columns** tab
3. Click any table to see its columns
4. Column details show data types, sizes, and constraints

## Building from Source

### Requirements
- .NET Framework 4.7.2 or later
- Visual Studio 2017 or later (optional)
- SQL Server or SQL Server Express

### Build Instructions

#### Using Visual Studio:
1. Open `SqlServerManager.csproj` in Visual Studio
2. Build → Build Solution (Ctrl+Shift+B)
3. Run with F5 (Debug) or Ctrl+F5 (Release)

#### Using Command Line:
```bash
# Navigate to project directory
cd SqlServerManager

# Build the project
dotnet build

# Run the application
dotnet run
```

## Running the Application

After building, you can find the executable at:
- Debug build: `bin\Debug\SqlServerManager.exe`
- Release build: `bin\Release\SqlServerManager.exe`

Simply double-click the executable to run the application.

## Security Notes

- Connection strings with SQL authentication are saved locally (passwords are not encrypted)
- Ensure proper SQL Server permissions for database operations
- Be careful with delete operations as they cannot be undone

## System Requirements

- Windows 7 or later
- .NET Framework 4.7.2
- Microsoft SQL Server (any version) or SQL Server Express
- Network connectivity to SQL Server (if remote)

## Troubleshooting

### Cannot connect to SQL Server
- Verify SQL Server service is running
- Check firewall settings for SQL Server port (default 1433)
- Ensure SQL Server allows remote connections (if connecting remotely)
- For SQL authentication, verify it's enabled in SQL Server settings

### Access denied errors
- Ensure your account has necessary permissions
- For Windows authentication, your Windows user must have SQL Server access
- For SQL authentication, verify username and password

### Application won't start
- Ensure .NET Framework 4.7.2 is installed
- Check Windows Event Viewer for detailed error messages

## License

This application is provided as-is for educational and development purposes.
