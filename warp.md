# Warp Configuration

## SQL Server Manager Project

This is a configuration file for Warp terminal integration with the SQL Server Manager project.

### Project Information
- **Name**: SQL Server Manager (SQL_dbaseM)
- **Type**: C# Windows Forms Application (.NET 8)
- **Purpose**: Advanced SQL Server Database Management Tool
- **Status**: Production Ready ✅
- **Latest Build**: Release configuration with data editing capabilities

### Recent Updates (August 2025)
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
- **Data Editing**: Full CRUD operations with transaction support
- **Schema Management**: View and manage database schemas, tables, and properties
- **Advanced UI**: Dark/Light themes, resizable dialogs, progress indicators
- **Error Handling**: Comprehensive logging and user-friendly error messages
- **Async Operations**: Non-blocking UI with cancellation support

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
- `DatabasePropertiesDialog.cs` - Database schema information
- `InputDialog.cs`, `CreateDatabaseDialog.cs` - Utility dialogs

### Deployment
The application can be deployed as:
1. **Self-contained** - No .NET installation required (197MB exe)
2. **Framework-dependent** - Requires .NET 8 runtime on target machine
3. **Portable** - Copy publish folder to any Windows x64 machine

### Troubleshooting
- **Connection Issues**: Check SQL Server service status and connection strings
- **LocalDB Problems**: Use `sqllocaldb start MSSQLLocalDB` to start instance
- **Build Errors**: Clean solution and restore NuGet packages
- **Runtime Errors**: Check `error.log` file in application directory
