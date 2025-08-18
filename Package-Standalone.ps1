# SQL Server Manager - Standalone Packager
# This script creates a standalone package of the application

Write-Host "Creating standalone package for SQL Server Manager..." -ForegroundColor Green

# Define paths
$projectPath = $PSScriptRoot
$releasePath = Join-Path $projectPath "bin\Release"
$standalonePath = Join-Path $projectPath "SqlServerManager-Standalone"

# Remove existing standalone folder if it exists
if (Test-Path $standalonePath) {
    Write-Host "Removing existing standalone package..." -ForegroundColor Yellow
    Remove-Item $standalonePath -Recurse -Force
}

# Create standalone folder
Write-Host "Creating standalone folder..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $standalonePath | Out-Null

# Copy executable and dependencies
Write-Host "Copying application files..." -ForegroundColor Cyan
Copy-Item "$releasePath\*.exe" $standalonePath
Copy-Item "$releasePath\*.dll" $standalonePath -ErrorAction SilentlyContinue
Copy-Item "$releasePath\*.config" $standalonePath

# Create a simple launcher
$launcherContent = @"
@echo off
title SQL Server Manager
start SqlServerManager.exe
exit
"@
Set-Content -Path "$standalonePath\Launch.bat" -Value $launcherContent

# Create README for the standalone package
$readmeContent = @"
SQL Server Manager - Standalone Edition
========================================

This is a standalone package of SQL Server Manager.
No installation required!

REQUIREMENTS:
- Windows 7 or later
- .NET Framework 4.7.2 (usually pre-installed on Windows 10/11)

HOW TO RUN:
- Double-click 'Launch.bat' or 'SqlServerManager.exe'

FEATURES:
- Connect to SQL Server instances
- Manage databases (create, rename, delete)
- Create and edit tables
- View table structures and columns
- Light/Dark theme support
- Adjustable font sizes
- Save connection credentials

FIRST TIME SETUP:
1. Launch the application
2. Click 'Connect' or File -> Connect
3. Enter your SQL Server details
4. Check 'Save password' to remember credentials

For more information, see README.md in the project folder.
"@
Set-Content -Path "$standalonePath\README.txt" -Value $readmeContent

Write-Host "`nStandalone package created successfully!" -ForegroundColor Green
Write-Host "Location: $standalonePath" -ForegroundColor Yellow
Write-Host "`nThe application can now be:" -ForegroundColor Cyan
Write-Host "  - Run directly without Visual Studio" -ForegroundColor White
Write-Host "  - Copied to any Windows machine with .NET Framework 4.7.2" -ForegroundColor White
Write-Host "  - Distributed as a portable application" -ForegroundColor White

# Ask if user wants to open the folder
$response = Read-Host "`nDo you want to open the standalone folder? (Y/N)"
if ($response -eq 'Y' -or $response -eq 'y') {
    explorer.exe $standalonePath
}
