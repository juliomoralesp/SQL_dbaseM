# SQL Server Manager - Android Build & Deployment Instructions

## Project Information
- **APK Name**: SqlServerManager-Android.apk
- **Package ID**: com.sqlservermanager.mobile
- **Version**: 1.0
- **Target Android**: API 21+ (Android 5.0 and above)
- **Build Date**: August 17, 2025

## Prerequisites

### Development Environment
1. **Windows 10/11** with PowerShell
2. **.NET 8 SDK** or later
3. **Visual Studio 2022** (optional) or VS Code
4. **MAUI Workload** installed:
   ```powershell
   dotnet workload install maui-android
   ```

### Android Development
1. **Android SDK** (API 21 or higher)
2. **Java Development Kit (JDK)** 11 or higher
3. **Android Emulator** (optional) or physical device for testing

## Building the APK

### Quick Build Command
```powershell
# Navigate to project directory
cd C:\Users\ju_mo\OneDrive\SqlServerManager

# Build the APK
dotnet publish SqlServerManager.Mobile.csproj -f net8.0-android -c Release -p:AndroidPackageFormat=apk
```

### Using Build Script
```powershell
# Run the automated build script
.\Build-AndroidAPK.ps1 -Configuration Release
```

### Build Output Location
The signed APK will be generated at:
- `.\bin\Release\net8.0-android\com.sqlservermanager.mobile-Signed.apk`

### Copy to Root Directory
```powershell
Copy-Item -Path ".\bin\Release\net8.0-android\com.sqlservermanager.mobile-Signed.apk" -Destination ".\SqlServerManager-Android.apk" -Force
```

## Installation Methods

### Method 1: Direct Installation on Android Device

1. **Transfer the APK** to your Android device using one of these methods:
   - USB cable transfer
   - Email attachment
   - Cloud storage (Google Drive, OneDrive, Dropbox)
   - Direct download link

2. **Enable Unknown Sources** on Android device:
   - Go to **Settings → Security**
   - Enable **"Install from Unknown Sources"**
   - Or for newer Android versions:
     - Go to **Settings → Apps & notifications → Advanced → Special app access**
     - Select **"Install unknown apps"**
     - Choose your file manager or browser
     - Toggle **"Allow from this source"**

3. **Install the APK**:
   - Open file manager on Android device
   - Navigate to the APK file location
   - Tap on `SqlServerManager-Android.apk`
   - Tap **Install**
   - Tap **Open** to launch the app

### Method 2: Using ADB (Android Debug Bridge)

1. **Enable Developer Options** on Android:
   - Go to **Settings → About phone**
   - Tap **Build number** 7 times
   - Go back to **Settings → Developer options**
   - Enable **USB debugging**

2. **Connect device to computer** via USB

3. **Install using ADB**:
   ```bash
   # Check if device is connected
   adb devices
   
   # Install the APK
   adb install SqlServerManager-Android.apk
   
   # Or force reinstall if updating
   adb install -r SqlServerManager-Android.apk
   ```

### Method 3: Using Android Emulator

1. **Start Android Emulator** in Android Studio or via command line:
   ```bash
   emulator -avd <emulator_name>
   ```

2. **Install APK on emulator**:
   ```bash
   adb install SqlServerManager-Android.apk
   ```

3. **Or drag and drop** the APK file onto the emulator window

## SQL Server Configuration

### Server-Side Requirements

1. **Enable TCP/IP Protocol**:
   - Open SQL Server Configuration Manager
   - Navigate to SQL Server Network Configuration
   - Select Protocols for your instance
   - Right-click TCP/IP → Enable
   - Set static port (default 1433) in TCP/IP Properties

2. **Enable Remote Connections**:
   - Open SQL Server Management Studio
   - Right-click server → Properties
   - Go to Connections page
   - Check "Allow remote connections to this server"

3. **Configure Firewall**:
   ```powershell
   # Windows Firewall - Allow SQL Server
   New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow
   
   # Allow SQL Browser (for named instances)
   New-NetFirewallRule -DisplayName "SQL Browser" -Direction Inbound -Protocol UDP -LocalPort 1434 -Action Allow
   ```

4. **Enable SQL Server Authentication** (if needed):
   - Server Properties → Security
   - Select "SQL Server and Windows Authentication mode"
   - Restart SQL Server service

### Network Requirements

1. **Same Network**: Ensure Android device and SQL Server are on the same network
2. **VPN**: If accessing remotely, configure VPN connection
3. **Port Forwarding**: For external access, configure router port forwarding

## Using the App

### First Connection

1. **Launch** SQL Server Manager on Android
2. **Tap** "Connect" button
3. **Enter connection details**:
   - **Server Name**: 
     - Local network: `192.168.1.100` or `192.168.1.100,1433`
     - Named instance: `192.168.1.100\SQLEXPRESS`
     - With custom port: `192.168.1.100,1434`
   - **Authentication**: 
     - Windows Authentication (domain required)
     - SQL Server Authentication (username/password)
   - **Database** (optional): Initial database to connect
   - **Timeout**: Connection timeout in seconds (default 30)
   - **Trust Certificate**: Enable for self-signed certificates

4. **Test Connection** before connecting
5. **Connect** to establish connection

### Features Available

- **Database Management**:
  - View all databases
  - Create new database
  - Rename database
  - Delete database
  - View database properties

- **Table Operations**:
  - Browse tables
  - View table structure
  - View column details
  - Table metadata

- **Connection Management**:
  - Save connections
  - Recent connections list
  - Quick reconnect

## Troubleshooting

### Connection Issues

1. **Cannot connect to SQL Server**:
   ```sql
   -- Check if SQL Server is listening on the network
   SELECT * FROM sys.dm_exec_connections WHERE session_id = @@SPID
   ```

2. **Test connectivity from Android device**:
   - Install network tools app (like Ping & Net)
   - Ping SQL Server IP address
   - Test port 1433 connectivity

3. **Common error solutions**:
   - **Timeout**: Increase timeout value
   - **Certificate error**: Enable "Trust Server Certificate"
   - **Authentication failed**: Verify credentials
   - **Network error**: Check firewall and network settings

### App Issues

1. **App won't install**:
   - Check Android version (requires 5.0+)
   - Enable unknown sources
   - Clear package installer cache
   - Check available storage space

2. **App crashes**:
   - Clear app data and cache
   - Reinstall the app
   - Check device logs using `adb logcat`

## Build Troubleshooting

### Common Build Errors

1. **Missing workload**:
   ```powershell
   dotnet workload install maui-android
   ```

2. **Clean and rebuild**:
   ```powershell
   dotnet clean SqlServerManager.Mobile.csproj
   dotnet restore SqlServerManager.Mobile.csproj
   dotnet build SqlServerManager.Mobile.csproj -f net8.0-android
   ```

3. **Update packages**:
   ```powershell
   dotnet add package Microsoft.Maui.Controls --version 8.0.90
   dotnet add package Microsoft.Data.SqlClient --version 5.2.0
   ```

## Version Management

### Update Version Number

Edit `SqlServerManager.Mobile.csproj`:
```xml
<ApplicationDisplayVersion>1.1</ApplicationDisplayVersion>
<ApplicationVersion>2</ApplicationVersion>
```

### Build Different Configurations

```powershell
# Debug build (larger, with debugging symbols)
dotnet publish SqlServerManager.Mobile.csproj -f net8.0-android -c Debug -p:AndroidPackageFormat=apk

# Release build (optimized, smaller)
dotnet publish SqlServerManager.Mobile.csproj -f net8.0-android -c Release -p:AndroidPackageFormat=apk
```

## Distribution

### Internal Distribution
1. Upload APK to company file server
2. Share via secure cloud storage
3. Email to specific users
4. Host on internal website

### Google Play Store (Future)
1. Generate signed AAB (Android App Bundle):
   ```powershell
   dotnet publish SqlServerManager.Mobile.csproj -f net8.0-android -c Release -p:AndroidPackageFormat=aab
   ```
2. Create Google Play Developer account
3. Upload AAB to Play Console
4. Complete store listing
5. Submit for review

## Security Considerations

### App Security
- APK is signed with debug certificate (for development)
- For production, use proper keystore:
  ```xml
  <PropertyGroup>
    <AndroidSigningKeyStore>myapp.keystore</AndroidSigningKeyStore>
    <AndroidSigningKeyAlias>myapp</AndroidSigningKeyAlias>
    <AndroidSigningKeyPass>password</AndroidSigningKeyPass>
    <AndroidSigningStorePass>password</AndroidSigningStorePass>
  </PropertyGroup>
  ```

### Connection Security
- Use SQL Authentication with strong passwords
- Enable SSL/TLS for SQL Server connections
- Use VPN for remote connections
- Don't save passwords on shared devices

## Maintenance

### Update Dependencies
```powershell
# Check for outdated packages
dotnet list package --outdated

# Update all packages
dotnet add package Microsoft.Maui.Controls
dotnet add package Microsoft.Data.SqlClient
dotnet add package CommunityToolkit.Maui
```

### Clean Build Artifacts
```powershell
# Clean all build outputs
Remove-Item -Path ".\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\obj" -Recurse -Force -ErrorAction SilentlyContinue
```

## Support Information

### System Requirements
- **Android**: 5.0 (API 21) or higher
- **Storage**: ~100 MB free space
- **Network**: WiFi or mobile data for SQL Server access
- **SQL Server**: 2012 or later

### File Locations
- **Source Code**: `C:\Users\ju_mo\OneDrive\SqlServerManager`
- **APK Output**: `.\bin\Release\net8.0-android\`
- **Final APK**: `.\SqlServerManager-Android.apk`

### Contact
- Report issues in GitHub repository
- Email: support@example.com

## Quick Reference Commands

```powershell
# Build APK
dotnet publish SqlServerManager.Mobile.csproj -f net8.0-android -c Release -p:AndroidPackageFormat=apk

# Install via ADB
adb install SqlServerManager-Android.apk

# Uninstall via ADB
adb uninstall com.sqlservermanager.mobile

# View device logs
adb logcat | Select-String "SqlServerManager"

# Check APK info
aapt dump badging SqlServerManager-Android.apk
```

---
Last Updated: August 17, 2025
Version: 1.0
