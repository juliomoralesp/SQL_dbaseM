using System;
using System.Drawing;
using System.Windows.Forms;

namespace SqlServerManager
{
    public static class FontManager
    {
        private static float BaseFontSize = 10f;
        private static float CurrentScaleFactor = 1.0f;

        public static float CurrentFontSize => BaseFontSize * CurrentScaleFactor;

        public static void SetFontScale(float scaleFactor)
        {
            CurrentScaleFactor = scaleFactor;
        }

        public static void ApplyFontSize(Form form, float scaleFactor)
        {
            CurrentScaleFactor = scaleFactor;
            ApplyFontSizeToControl(form);
        }

        private static void ApplyFontSizeToControl(Control control)
        {
            if (control.Font != null)
            {
                control.Font = new Font(control.Font.FontFamily, CurrentFontSize, control.Font.Style);
            }

            if (control is DataGridView dgv)
            {
                dgv.DefaultCellStyle.Font = new Font(dgv.DefaultCellStyle.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, CurrentFontSize);
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.ColumnHeadersDefaultCellStyle.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, CurrentFontSize + 1, FontStyle.Bold);
                dgv.RowTemplate.Height = (int)(22 * CurrentScaleFactor);
            }

            if (control is ToolStrip toolStrip)
            {
                toolStrip.Font = new Font(toolStrip.Font.FontFamily, CurrentFontSize);
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    item.Font = new Font(item.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, CurrentFontSize);
                }
            }

            if (control is StatusStrip statusStrip)
            {
                statusStrip.Font = new Font(statusStrip.Font.FontFamily, CurrentFontSize);
                foreach (ToolStripItem item in statusStrip.Items)
                {
                    item.Font = new Font(item.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, CurrentFontSize);
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyFontSizeToControl(child);
            }
        }

        public static Font GetScaledFont(float baseSize, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", baseSize * CurrentScaleFactor, style);
        }

        public static Font GetScaledFont(string fontFamily, float baseSize, FontStyle style = FontStyle.Regular)
        {
            return new Font(fontFamily, baseSize * CurrentScaleFactor, style);
        }
    }
}
