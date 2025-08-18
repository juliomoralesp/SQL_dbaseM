# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

SqlServerManager is a dual-platform database management tool for Microsoft SQL Server. It consists of:

1. **Windows Desktop Application** - Built with .NET 8.0 and Windows Forms
2. **Android Mobile Application** - Built with .NET MAUI 8.0

Both applications provide SQL Server database management capabilities including connection management, database operations (create, rename, delete), table browsing, and column inspection.

## Architecture

### Desktop Application (Windows Forms)
- **Entry Point**: `Program.cs` - Standard Windows Forms application entry
- **Main UI**: `MainForm.cs` - Tabbed interface with databases and tables/columns views
- **Core Services**: Direct SQL Server integration via `System.Data.SqlClient`
- **UI Patterns**: Windows Forms with context menus, data grids, and dialog forms
- **Configuration**: App.config for connection strings and settings

### Mobile Application (.NET MAUI)
- **Entry Point**: `MauiProgram.cs` - MAUI application setup with dependency injection
- **Architecture Pattern**: MVVM (Model-View-ViewModel)
- **Services Layer**: 
  - `IConnectionService`/`ConnectionService` - Connection and credential management
  - `IDatabaseService`/`DatabaseService` - Database operations and queries
- **Models**: `DatabaseModels.cs` - DTOs for database entities
- **Views**: XAML pages in `Views/` directory with code-behind
- **ViewModels**: Business logic and data binding (referenced in MauiProgram.cs)

### Shared Components
- **Data Access**: Both applications use `System.Data.SqlClient` for SQL Server connectivity
- **Authentication**: Support for both Windows and SQL Server authentication
- **Features**: Database CRUD operations, table browsing, column inspection

## Common Development Commands

### Building Desktop Application
```powershell
# Build debug version
dotnet build SqlServerManager.csproj

# Build release version  
dotnet build SqlServerManager.csproj -c Release

# Run the application
dotnet run --project SqlServerManager.csproj

# Publish self-contained executable
dotnet publish SqlServerManager.csproj -c Release -r win-x64 --self-contained true
```

### Building Android Application
```powershell
# Install MAUI workload (first time setup)
dotnet workload install maui-android

# Restore NuGet packages
dotnet restore SqlServerManager.Mobile.csproj

# Build debug APK
dotnet build SqlServerManager.Mobile.csproj -f net8.0-android -c Debug

# Build release APK
dotnet publish SqlServerManager.Mobile.csproj -f net8.0-android -c Release -p:AndroidPackageFormat=apk

# Use the PowerShell build script
.\Build-AndroidAPK.ps1 -Configuration Release
```

### Package Management
```powershell
# Create standalone desktop package
.\Package-Standalone.ps1

# Install APK on Android device via ADB
adb install SqlServerManager.apk
```

### Testing and Development
```powershell
# Clean all build artifacts
dotnet clean

# Restore dependencies for both projects
dotnet restore SqlServerManager.csproj
dotnet restore SqlServerManager.Mobile.csproj

# Run desktop app in debug mode
dotnet run --project SqlServerManager.csproj

# Check for outdated packages
dotnet list package --outdated
```

## Key Dependencies

### Desktop Application
- **.NET 8.0** with Windows target framework
- **System.Data.SqlClient 4.8.6** - SQL Server connectivity
- **System.Configuration.ConfigurationManager 8.0.0** - App configuration
- **Windows Forms** - UI framework

### Mobile Application  
- **.NET MAUI 8.0** targeting Android API 21+
- **Microsoft.Maui.Controls 8.0.90** - Cross-platform UI
- **CommunityToolkit.Maui 9.0.2** - Additional UI components
- **System.Data.SqlClient 4.8.6** - SQL Server connectivity
- **Microsoft.Extensions.Logging.Debug 8.0.0** - Debug logging

## Development Environment Setup

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- For Android development: Android SDK with API 21+ support

### First-Time Setup
```powershell
# Install required workloads
dotnet workload install maui-android

# Verify installations
dotnet --info
dotnet workload list

# Restore all packages
dotnet restore
```

## Project Structure Patterns

### Service Layer Pattern (MAUI)
Services follow interface segregation with dependency injection:
- `IConnectionService` - Connection management and saved connections
- `IDatabaseService` - Database operations and SQL queries
- Services registered in `MauiProgram.cs` as singletons

### Data Models
All data models in `Models/DatabaseModels.cs`:
- `DatabaseInfo` - Database metadata
- `TableInfo` - Table structure information  
- `ColumnInfo` - Column definitions and constraints
- `ConnectionInfo` - Saved connection details
- `DatabaseProperties` - Extended database properties

### SQL Operations
Both applications execute raw SQL queries for:
- Database listing: Query `sys.databases` system view
- Table browsing: Query `INFORMATION_SCHEMA.TABLES`
- Column inspection: Query `INFORMATION_SCHEMA.COLUMNS`
- Database operations: Direct DDL statements (CREATE/DROP/ALTER DATABASE)

## Build Outputs

### Desktop Application
- **Debug**: `bin\Debug\net8.0-windows\SqlServerManager.exe`
- **Release**: `bin\Release\net8.0-windows\SqlServerManager.exe`
- **Published**: Single-file executable in publish directory

### Android Application
- **Debug APK**: `bin\Debug\net8.0-android\*.apk`
- **Release APK**: `bin\Release\net8.0-android\*.apk`
- **Script Output**: `bin\Android\SqlServerManager.apk` (via Build-AndroidAPK.ps1)

## Configuration Files

- **App.config** - Desktop application configuration
- **SqlServerManager.csproj** - Desktop project file with Windows Forms configuration
- **SqlServerManager.Mobile.csproj** - MAUI project with Android targeting
- **Build-AndroidAPK.ps1** - Automated Android build script
- **Package-Standalone.ps1** - Desktop packaging script

## Connection Management

Both applications support:
- **Windows Authentication** - Uses current user credentials
- **SQL Server Authentication** - Username/password authentication  
- **Connection Testing** - Validate before connecting
- **Saved Connections** - Local storage of connection details (mobile only)
- **Connection Strings** - Standard SQL Server format
