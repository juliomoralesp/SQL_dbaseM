# Warp Configuration

## SQL Server Manager Project

This is a configuration file for Warp terminal integration with the SQL Server Manager project.

### Project Information
- **Name**: SQL Server Manager (SQL_dbaseM)
- **Type**: C# Windows Forms Application (.NET 8)
- **Purpose**: Advanced SQL Server Database Management Tool
- **Status**: Production Ready ✅ - v1.1.0 Released
- **Latest Build**: Enhanced Network Discovery & Modern UI
- **Release Page**: [v1.1.0 on GitHub](https://github.com/juliomoralesp/SQL_dbaseM/releases/tag/v1.1.0)

### Recent Updates (August 2025)

#### Version 1.1.0 (Latest Release)
- ✅ Fixed network discovery functionality for SQL Server instances
- ✅ Improved SQL Browser UDP packet parsing and error handling
- ✅ Added proper ASCII encoding for server instance parsing
- ✅ Enhanced UI with repositioned Connect/Cancel buttons in discovery dialog
- ✅ Implemented robust subnet mask calculation for network scanning
- ✅ Added comprehensive server management and monitoring features
- ✅ Implemented modern theme management and progress indicators

#### Version 1.0.1
- ✅ Fixed parameter naming issues in TableDataEditor
- ✅ Improved async connection handling with cancellation support
- ✅ Added MARS (Multiple Active Result Sets) support
- ✅ Enhanced data editing with full CRUD operations
- ✅ Comprehensive error handling and logging
- ✅ Self-contained publishing for easy deployment

### Quick Commands

#### Development
```bash
# Build the project
dotnet build SqlServerManager.csproj --configuration Release

# Run the application
dotnet run --project SqlServerManager.csproj

# Clean build artifacts
dotnet clean SqlServerManager.csproj

# Restore packages
dotnet restore SqlServerManager.csproj
```

#### Publishing & Distribution
```bash
# Publish self-contained executable (Windows x64)
dotnet publish SqlServerManager.csproj --configuration Release --self-contained true --runtime win-x64 --output publish

# Publish framework-dependent (smaller) - requires .NET 8 runtime
dotnet publish SqlServerManager.csproj --configuration Release --self-contained false --runtime win-x64 --output "./publish/win-x64"

# Create release ZIP archive
Compress-Archive -Path "./publish/win-x64/*" -DestinationPath "./SqlServerManager-v1.1.0-win-x64.zip" -Force

# Run published application
.\publish\SqlServerManager.exe

# Check published files
dir publish
```

#### Git Operations
```bash
# Check status
git status

# View branches
git branch -a

# View recent commits
git log --oneline -10

# Commit changes
git add .
git commit -m "Your commit message"

# Create a version tag
git tag -a v1.1.0 -m "Release v1.1.0: Enhanced Network Discovery and Modern UI Features"

# Push commits and tags
git push origin main
git push origin v1.1.0

# Create GitHub release with gh CLI
gh release create v1.1.0 "./SqlServerManager-v1.1.0-win-x64.zip" --title "SQL Server Manager v1.1.0" --notes "Release notes here"
```

#### Testing & Debugging
```bash
# Start LocalDB instance for testing
sqllocaldb start MSSQLLocalDB

# Check LocalDB status
sqllocaldb info

# Run test database setup
powershell -File setup_test_db.ps1

# Check application logs
type error.log
```

### Key Features
- **Database Connection Management**: Support for SQL Server, LocalDB, and Azure SQL
- **Network Discovery**: Auto-detect SQL Server instances on local network
- **Data Editing**: Full CRUD operations with transaction support
- **Schema Management**: View and manage database schemas, tables, and properties
- **Advanced UI**: Dark/Light themes, resizable dialogs, progress indicators
- **Performance Monitoring**: Server performance metrics and health status
- **Error Handling**: Comprehensive logging and user-friendly error messages
- **Async Operations**: Non-blocking UI with cancellation support
- **Modern SQL Editor**: Syntax highlighting and query execution

### Technical Stack
- **.NET**: 8.0 (Windows-targeted)
- **UI Framework**: Windows Forms
- **Database**: Microsoft.Data.SqlClient (modern SQL client)
- **IDE**: Visual Studio 2022 or VS Code recommended
- **Database Support**: SQL Server 2012+, LocalDB, Azure SQL

### Project Structure
- `Program.cs` - Application entry point with error handling
- `MainForm.cs` - Main UI with database/table management
- `ConnectionDialog.cs` - Enhanced connection dialog with async support
- `TableDataEditor.cs` - Advanced data editing with CRUD operations
- `ThemeManager.cs` - UI theming system
- `Core/ServerManagement/ServerDiscovery.cs` - SQL Server network discovery
- `UI/ServerDiscoveryDialog.cs` - Improved server discovery interface
- `Core/Monitoring/PerformanceMonitor.cs` - Server performance tracking
- `Core/QueryEngine/AdvancedSqlEditor.cs` - SQL editing with syntax highlighting
- `Core/DataOperations/DataMigrationWizard.cs` - Data transfer utilities
- `DatabasePropertiesDialog.cs` - Database schema information
- `Services/ExceptionHandler.cs` - Global exception management
- `Services/ConnectionService.cs` - Connection management with SSL support
- `InputDialog.cs`, `CreateDatabaseDialog.cs` - Utility dialogs

### Deployment
The application can be deployed as:
1. **Self-contained** - No .NET installation required (197MB exe)
2. **Framework-dependent** - Requires .NET 8 runtime on target machine (22MB)
3. **Portable** - Copy publish folder to any Windows x64 machine
4. **GitHub Release** - Download ZIP from [GitHub Releases](https://github.com/juliomoralesp/SQL_dbaseM/releases)

### Troubleshooting
- **Connection Issues**: Check SQL Server service status and connection strings
- **LocalDB Problems**: Use `sqllocaldb start MSSQLLocalDB` to start instance
- **Network Discovery Issues**: Ensure SQL Browser service is running on target servers (UDP port 1434)
- **Build Errors**: Clean solution and restore NuGet packages
- **Runtime Errors**: Check `error.log` file in application directory
- **SSL Certificate Issues**: Use the 'Trust Server Certificate' option for self-signed certificates
