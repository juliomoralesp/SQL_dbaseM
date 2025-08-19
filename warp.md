# Warp Configuration

## SQL_dbaseM Project

This is a configuration file for Warp terminal integration with the SQL_dbaseM project.

### Project Information
- **Name**: SQL_dbaseM
- **Type**: C# Windows Forms Application
- **Purpose**: SQL Server Database Management Tool

### Quick Commands

#### Development
```bash
# Build the project
dotnet build SqlServerManager.csproj

# Run the application
dotnet run --project SqlServerManager.csproj

# Clean build artifacts
dotnet clean SqlServerManager.csproj
```

#### Git Operations
```bash
# Check status
git status

# View branches
git branch -a

# View recent commits
git log --oneline -10
```

#### Project Management
```bash
# Package standalone application
powershell -ExecutionPolicy Bypass -File Package-Standalone.ps1

# Run via batch file
.\RunSqlServerManager.bat

# View project structure
Get-ChildItem -Recurse -Name
```

### Environment
- **.NET Framework/Core**: Check SqlServerManager.csproj for target framework
- **IDE**: Visual Studio or VS Code recommended
- **Database**: SQL Server compatibility

### Notes
- Main entry point: `Program.cs`
- Main UI: `MainForm.cs`
- Database operations handled through various dialog classes
- Supports theming via `ThemeManager.cs`
