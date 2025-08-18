# SQL Database Manager v1.0.0

## 🎉 Initial Release

We're excited to announce the first release of SQL Database Manager - a modern, feature-rich Windows desktop application for managing Microsoft SQL Server databases.

## ✨ Features

### Database Management
- ✅ Connect to SQL Server using Windows or SQL Authentication
- ✅ Create, rename, and delete databases
- ✅ View comprehensive database properties
- ✅ Browse database tables and columns
- ✅ Save connection credentials for quick access

### Table Management
- ✅ **Visual Table Designer** - Create tables with intuitive GUI
- ✅ **Column Management** - Add, remove, and reorder columns
- ✅ **Data Type Support** - Full SQL Server data type compatibility
- ✅ **Constraints** - Set primary keys, identity columns, nullable settings
- ✅ **Table Operations** - Create, edit structure, and delete tables

### User Experience
- 🎨 **Theme Support** - Light, Dark, and System themes
- 🔤 **Adjustable Font Sizes** - 80%, 100%, 120%, 150% scaling
- 💾 **Settings Persistence** - Remembers your preferences
- 🔒 **Credential Management** - Secure password storage (base64 encoded)
- 🚀 **Modern UI** - Clean, intuitive interface with tabbed navigation

## 📦 Download Options

### Standalone Executable (Recommended)
- **SqlServerManager-v1.0.0-win-x64.zip** (70 MB)
- Self-contained .NET 8.0 application
- No installation required
- Works on Windows 10/11 x64

## 🖥️ System Requirements

- **OS:** Windows 10 or Windows 11 (64-bit)
- **Runtime:** Included (self-contained)
- **SQL Server:** Any version (including Express)
- **Memory:** 512 MB RAM minimum
- **Disk Space:** 200 MB

## 📥 Installation

1. Download `SqlServerManager-v1.0.0-win-x64.zip`
2. Extract to any folder
3. Run `SqlServerManager.exe`
4. No installation or admin rights required!

## 🚀 Getting Started

1. Launch the application
2. Click **Connect** or use File → Connect
3. Enter your SQL Server details:
   - Server name (e.g., `localhost`, `.\SQLEXPRESS`)
   - Choose authentication method
   - For SQL Auth, enter username/password
4. Click **Test Connection** to verify
5. Click **OK** to connect

## 🐛 Known Issues

- Table structure editing shows structure but doesn't execute ALTER TABLE commands (for safety)
- Passwords are base64 encoded, not encrypted (use Windows Auth for production)

## 🤝 Contributing

We welcome contributions! Please feel free to submit issues, feature requests, or pull requests.

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

## 🙏 Acknowledgments

Built with:
- .NET 8.0
- Windows Forms
- System.Data.SqlClient

---

**Full Changelog:** This is the initial release

**Download:** [SqlServerManager-v1.0.0-win-x64.zip](../../releases/download/v1.0.0/SqlServerManager-v1.0.0-win-x64.zip)
