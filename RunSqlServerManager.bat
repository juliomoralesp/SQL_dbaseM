@echo off
:: SQL Server Manager - Standalone Launcher
:: This batch file ensures the application runs independently

:: Set the application directory
set APP_DIR=%~dp0

:: Change to application directory
cd /d "%APP_DIR%"

:: Check if the executable exists
if not exist "bin\Debug\SqlServerManager.exe" (
    echo Building SQL Server Manager...
    dotnet build > nul 2>&1
    if errorlevel 1 (
        echo Failed to build. Using MSBuild instead...
        "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" SqlServerManager.csproj /p:Configuration=Debug /p:Platform="Any CPU" > nul 2>&1
    )
)

:: Run the application
start "" "bin\Debug\SqlServerManager.exe"

:: Exit the batch file
exit
