using System;
using System.Drawing;
using System.Windows.Forms;

namespace SqlServerManager.Core.Controls
{
    /// <summary>
    /// A simple text editor control that provides basic functionality to replace ScintillaNET
    /// temporarily while we resolve .NET 8.0 compatibility issues
    /// </summary>
    public class SimpleTextEditor : RichTextBox
    {
        public SimpleTextEditor()
        {
            // Configure the control to behave similarly to a code editor
            Font = new Font("Consolas", 10F);
            AcceptsTab = true;
            WordWrap = false;
            ScrollBars = RichTextBoxScrollBars.Both;
            DetectUrls = false;
            BackColor = Color.White;
            ForeColor = Color.Black;
        }

        // Properties to mimic Scintilla interface
        public string LexerName 
        { 
            get { return "text"; } 
            set { /* No-op for now */ }
        }

        // Method to mimic Scintilla styling
        public void StyleSetFont(int styleNumber, string fontName)
        {
            // No-op for now - could implement basic font styling later
        }

        public void StyleSetSize(int styleNumber, int size)
        {
            // No-op for now
        }

        public void StyleSetForeColor(int styleNumber, Color color)
        {
            // No-op for now
        }

        public void StyleSetBackColor(int styleNumber, Color color)
        {
            // No-op for now
        }

        public void SetKeywords(int set, string keywords)
        {
            // No-op for now - could implement basic keyword highlighting later
        }

        // Helper method to apply basic SQL syntax highlighting
        public void ApplySqlStyling()
        {
            // This is a placeholder - we could implement basic keyword highlighting
            // using RichTextBox's RTF capabilities later if needed
        }
    }
}
