Add-Type -AssemblyName System.Windows.Forms
. (Join-Path $PSScriptRoot 'form1.designer.ps1')
$Form1.ShowDialog()