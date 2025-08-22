# Changelog

All notable changes to SQL Server Manager will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2025-08-22

### 🔧 Fixed
- **CRITICAL**: Fixed persistent "SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize" error in AdvancedSearchDialog
- Resolved form initialization issues that caused application crashes during dialog creation
- Fixed layout management problems in Windows Forms SplitContainer controls
- Improved control initialization sequences to prevent constraint violations

### ✨ Added
- New `IConnectionService` interface for better separation of concerns
- `TableSelectionDialog` component for enhanced table selection functionality
- Enhanced error handling and logging throughout the application
- Better diagnostic capabilities for debugging UI issues

### 🎨 Changed
- Replaced custom `DiagnosticSplitContainer` with standard `SplitContainer` using minimal default settings
- Simplified SplitContainer configuration with 25px minimum panel sizes
- Removed all explicit SplitterDistance settings, letting Windows Forms handle automatic layout
- Enhanced ConnectionDialog with improved error handling
- Optimized various UI components for better stability

### 🧹 Maintenance
- Organized documentation and test files into `._backup` folder structure
- Removed obsolete diagnostic components and temporary files
- Cleaned up project structure and dependencies
- Improved code organization and maintainability
- Updated project documentation

### 📦 Technical Details
- Self-contained deployment for Windows x64
- .NET 8.0 Windows Forms application
- Package size: ~97MB (self-contained with all dependencies)
- Commit: 919f1bb

## [1.1.0] - Previous Release
- Initial stable release with core functionality
- Database connection management
- Table browsing and data editing
- Advanced search capabilities (with known SplitContainer bug)
- Modern UI implementation

## [1.0.0] - Initial Release
- Core SQL Server management functionality
- Basic UI implementation
- Connection and query capabilities
