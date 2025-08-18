# SQL Server Manager for Android

A mobile Android application for managing Microsoft SQL Server databases, ported from the Windows desktop version using .NET MAUI.

## Features

### Core Functionality
- **SQL Server Connection Management**
  - Support for both Windows and SQL Server Authentication
  - Save and manage multiple server connections
  - Test connections before connecting
  - Secure connection with certificate trust options

- **Database Operations**
  - View all databases on connected server
  - Create new databases
  - Rename existing databases
  - Delete databases with confirmation
  - View database properties

- **Table & Column Management**
  - Browse tables in any database
  - View table schemas and structures
  - Inspect column details (data types, constraints, nullable)
  - Table operations (create, rename, delete)

### Mobile-Optimized Features
- Touch-friendly interface
- Responsive layout for different screen sizes
- Native Android look and feel
- Offline connection history
- Network state awareness

## System Requirements

### Android Device
- Android 5.0 (API 21) or higher
- Minimum 100MB free storage
- Active network connection for SQL Server access

### SQL Server Requirements
- Microsoft SQL Server 2012 or later
- SQL Server must be configured to accept remote connections
- TCP/IP protocol enabled
- Firewall configured to allow SQL Server port (default 1433)

## Installation

### Method 1: Direct APK Installation
1. Download `SqlServerManager.apk` from releases
2. On your Android device, go to Settings > Security
3. Enable "Unknown Sources" or "Install unknown apps"
4. Open the APK file to install
5. Follow the installation prompts

### Method 2: Using ADB (Android Debug Bridge)
```bash
adb install SqlServerManager.apk
```

### Method 3: Build from Source
```powershell
# Clone the repository
git clone https://github.com/yourusername/SqlServerManager.git
cd SqlServerManager

# Checkout Android branch
git checkout feature/android-apk-port

# Build the APK
.\Build-AndroidAPK.ps1 -Configuration Release
```

## Configuration

### Network Configuration
For the app to connect to SQL Server:

1. **SQL Server Configuration:**
   - Enable TCP/IP in SQL Server Configuration Manager
   - Set a static port (default 1433)
   - Restart SQL Server service

2. **Firewall Rules:**
   - Allow inbound connections on SQL Server port
   - Allow SQL Server Browser service (UDP 1434) if using named instances

3. **SQL Server Settings:**
   - Enable remote connections
   - For SQL Authentication, ensure it's enabled in server properties

### Connection Setup in App

1. Launch SQL Server Manager on your Android device
2. Tap "Connect" button
3. Enter connection details:
   - **Server Name**: IP address or hostname (e.g., `192.168.1.100` or `myserver.local`)
   - **Authentication**: Choose Windows or SQL Server
   - **Username/Password**: For SQL Authentication only
   - **Database**: Optional initial database
   - **Trust Certificate**: Enable for self-signed certificates

4. Tap "Test Connection" to verify
5. Tap "Connect" to establish connection

## Usage Guide

### Managing Databases
1. After connecting, all databases appear in the Databases tab
2. Tap a database to select it
3. Double-tap to view its tables
4. Long-press for context menu (create, rename, delete)

### Viewing Tables and Columns
1. Navigate to "Tables & Columns" tab
2. Select a database first
3. Tables appear in the upper section
4. Tap a table to view its columns in the lower section

### Connection Management
- Recent connections are saved automatically
- Tap a saved connection to quickly reconnect
- Swipe to delete saved connections

## Troubleshooting

### Cannot Connect to SQL Server

**Check Network Connectivity:**
```bash
# From terminal on Android (using Termux or similar)
ping your-sql-server-ip
```

**Verify SQL Server is listening:**
```sql
-- On SQL Server
SELECT * FROM sys.dm_exec_connections
```

**Common Issues:**
- **Connection Timeout**: Increase timeout in connection settings
- **Certificate Error**: Enable "Trust Server Certificate" option
- **Authentication Failed**: Verify credentials and authentication mode
- **Network Error**: Check if device is on same network or VPN

### App Crashes or Freezes
1. Force stop the app
2. Clear app cache and data
3. Reinstall the app
4. Check device storage space

### Performance Issues
- Close unused database connections
- Limit the number of tables/columns retrieved
- Use WiFi instead of mobile data for better performance

## Security Considerations

### Best Practices
1. **Use SQL Authentication** with strong passwords
2. **Enable SSL/TLS** encryption for connections
3. **Limit SQL Server permissions** for mobile app users
4. **Use VPN** when connecting over public networks
5. **Don't save passwords** on shared devices

### Permissions Required
The app requires the following Android permissions:
- `INTERNET` - For SQL Server connections
- `ACCESS_NETWORK_STATE` - To check network availability
- `ACCESS_WIFI_STATE` - To optimize for WiFi connections

## Known Limitations

1. **Windows Authentication** may not work in all network configurations
2. **Large databases** (>1000 tables) may experience slower performance
3. **Complex data types** (geography, geometry) display as text
4. **Stored procedures** and functions are not yet supported
5. **Data editing** is read-only in this version

## Development

### Building from Source

**Prerequisites:**
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Android SDK (API 21+)
- MAUI workload installed

**Build Steps:**
```bash
# Install MAUI workload
dotnet workload install maui-android

# Restore packages
dotnet restore SqlServerManager.Mobile.csproj

# Build Debug APK
dotnet build -f net8.0-android -c Debug

# Build Release APK
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk
```

### Project Structure
```
SqlServerManager/
├── Views/              # MAUI XAML pages
├── ViewModels/         # MVVM view models
├── Services/           # Database and connection services
├── Models/             # Data models
├── Platforms/
│   └── Android/        # Android-specific code
├── Resources/          # Images, styles, fonts
└── SqlServerManager.Mobile.csproj
```

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## License

MIT License - See LICENSE file for details

## Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Email: support@example.com

## Changelog

### Version 1.0.0 (Android Port)
- Initial Android release
- Full database browsing capabilities
- Connection management
- Table and column viewing
- Responsive mobile UI
- Offline connection history

## Roadmap

### Planned Features
- [ ] Data editing capabilities
- [ ] Query executor
- [ ] Export data to CSV/JSON
- [ ] Dark mode support
- [ ] Biometric authentication
- [ ] Cloud backup of connections
- [ ] Support for Azure SQL Database
- [ ] Stored procedure execution
- [ ] Performance monitoring tools

## Credits

Original Windows application by [Original Author]
Android port developed using .NET MAUI
Icons from Material Design Icons
