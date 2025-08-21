using System.Diagnostics.CodeAnalysis;

// This application is designed exclusively for Windows and uses Windows Forms
// Suppress CA1416 warnings for Windows-only APIs since this is intentional
[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", 
    Justification = "This application is Windows-specific and uses Windows Forms")]
