# Build Script for SQL Server Manager Android APK
# This script builds and packages the MAUI app as an Android APK

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\bin\Android\",
    [switch]$Sign = $false
)

Write-Host "Building SQL Server Manager for Android..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean SqlServerManager.Mobile.csproj -c $Configuration

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore SqlServerManager.Mobile.csproj

# Build the Android project
Write-Host "Building Android APK..." -ForegroundColor Yellow
$buildArgs = @(
    "publish",
    "SqlServerManager.Mobile.csproj",
    "-f", "net8.0-android",
    "-c", $Configuration,
    "-p:AndroidPackageFormat=apk"
)

if ($Sign) {
    Write-Host "Note: Signing is configured in the project file" -ForegroundColor Cyan
    # Add signing parameters if needed
    # $buildArgs += "-p:AndroidSigningKeyStore=keystore.jks"
    # $buildArgs += "-p:AndroidSigningKeyAlias=key0"
    # $buildArgs += "-p:AndroidSigningKeyPass=password"
    # $buildArgs += "-p:AndroidSigningStorePass=password"
}

# Execute build
$result = & dotnet $buildArgs 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    
    # Find the generated APK
    $apkPath = Get-ChildItem -Path ".\bin\$Configuration\net8.0-android\" -Filter "*.apk" -Recurse | Select-Object -First 1
    
    if ($apkPath) {
        $finalPath = Join-Path $OutputPath "SqlServerManager.apk"
        
        # Create output directory if it doesn't exist
        if (!(Test-Path $OutputPath)) {
            New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        }
        
        # Copy APK to output path
        Copy-Item $apkPath.FullName -Destination $finalPath -Force
        
        Write-Host "APK generated successfully!" -ForegroundColor Green
        Write-Host "Location: $finalPath" -ForegroundColor Cyan
        Write-Host "Size: $([math]::Round((Get-Item $finalPath).Length / 1MB, 2)) MB" -ForegroundColor Cyan
        
        # Display installation instructions
        Write-Host "`nInstallation Instructions:" -ForegroundColor Yellow
        Write-Host "1. Enable 'Unknown Sources' or 'Install from Unknown Apps' on your Android device" -ForegroundColor White
        Write-Host "2. Transfer the APK file to your Android device" -ForegroundColor White
        Write-Host "3. Open the APK file on your device to install" -ForegroundColor White
        Write-Host "`nAlternatively, use ADB to install:" -ForegroundColor Yellow
        Write-Host "   adb install $finalPath" -ForegroundColor Cyan
    }
    else {
        Write-Host "Warning: APK file not found in expected location" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    exit 1
}
