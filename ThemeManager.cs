using System;
using System.Drawing;
using System.Windows.Forms;

namespace SqlServerManager
{
    public static class ThemeManager
    {
        public enum Theme
        {
            Light,
            Dark,
            System
        }

        public static Theme CurrentTheme { get; private set; } = Theme.System;
        
        // Light theme colors
        private static Color LightThemePrimary = SystemColors.Control;
        private static Color LightThemeSecondary = SystemColors.Window;
        private static Color LightThemeText = SystemColors.ControlText;
        private static Color LightThemeTabActive = Color.White;
        private static Color LightThemeTabInactive = Color.FromArgb(240, 240, 240);
        private static Color LightThemeBorder = Color.FromArgb(171, 173, 179);

        // Dark theme colors
        private static Color DarkThemePrimary = Color.FromArgb(45, 45, 48);
        private static Color DarkThemeSecondary = Color.FromArgb(30, 30, 30);
        private static Color DarkThemeText = Color.FromArgb(241, 241, 241);
        private static Color DarkThemeTabActive = Color.FromArgb(37, 37, 38);
        private static Color DarkThemeTabInactive = Color.FromArgb(45, 45, 48);
        private static Color DarkThemeBorder = Color.FromArgb(63, 63, 70);

        public static void ApplyTheme(Form form, Theme theme)
        {
            CurrentTheme = theme;
            var primaryColor = GetPrimaryColor();
            var secondaryColor = GetSecondaryColor();
            var textColor = GetTextColor();

            form.BackColor = primaryColor;
            form.ForeColor = textColor;

            foreach (Control control in form.Controls)
            {
                ApplyThemeToControl(control, primaryColor, secondaryColor, textColor);
            }
        }

        public static void ApplyThemeToDialog(Form dialog)
        {
            // Apply current theme to a dialog
            var primaryColor = GetPrimaryColor();
            var secondaryColor = GetSecondaryColor();
            var textColor = GetTextColor();

            dialog.BackColor = primaryColor;
            dialog.ForeColor = textColor;

            foreach (Control control in dialog.Controls)
            {
                ApplyThemeToControl(control, primaryColor, secondaryColor, textColor);
            }
        }

        private static void ApplyThemeToControl(Control control, Color primary, Color secondary, Color text)
        {
            control.BackColor = primary;
            control.ForeColor = text;

            if (control is Button || control is TextBox || control is ListBox || control is ComboBox)
            {
                control.BackColor = secondary;
                control.ForeColor = text;
            }

            if (control is TabControl tabControl)
            {
                tabControl.BackColor = primary;
                tabControl.ForeColor = text;
                
                // Apply custom drawing for better theme support
                tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
                
                // Remove existing handlers to prevent duplicates
                tabControl.DrawItem -= TabControl_DrawItem;
                
                // Add the custom draw handler
                tabControl.DrawItem += TabControl_DrawItem;
                
                // Apply theme to each tab page
                foreach (TabPage tabPage in tabControl.TabPages)
                {
                    tabPage.BackColor = GetTabPageColor();
                    tabPage.ForeColor = text;
                    tabPage.BorderStyle = BorderStyle.None;
                }
            }

            if (control is DataGridView dgv)
            {
                dgv.BackgroundColor = secondary;
                dgv.ForeColor = text;
                dgv.GridColor = GetBorderColor();
                dgv.BorderStyle = BorderStyle.FixedSingle;
                
                // Style the cells
                dgv.DefaultCellStyle.BackColor = secondary;
                dgv.DefaultCellStyle.ForeColor = text;
                dgv.DefaultCellStyle.SelectionBackColor = GetSelectionColor();
                dgv.DefaultCellStyle.SelectionForeColor = text;
                
                // Style the column headers
                dgv.ColumnHeadersDefaultCellStyle.BackColor = primary;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = text;
                dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                dgv.EnableHeadersVisualStyles = false;
                
                // Style the row headers
                dgv.RowHeadersDefaultCellStyle.BackColor = primary;
                dgv.RowHeadersDefaultCellStyle.ForeColor = text;
            }

            if (control is MenuStrip menuStrip)
            {
                menuStrip.BackColor = primary;
                menuStrip.ForeColor = text;
                menuStrip.Renderer = new ThemeMenuRenderer();
            }

            if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = primary;
                toolStrip.ForeColor = text;
                toolStrip.Renderer = new ThemeToolStripRenderer();
            }

            if (control is StatusStrip statusStrip)
            {
                statusStrip.BackColor = primary;
                statusStrip.ForeColor = text;
                statusStrip.Renderer = new ThemeToolStripRenderer();
            }

            if (control is Label label)
            {
                label.BackColor = primary;
                label.ForeColor = text;
            }

            if (control is Panel panel)
            {
                panel.BackColor = primary;
                panel.ForeColor = text;
            }

            if (control is SplitContainer splitContainer)
            {
                splitContainer.BackColor = GetBorderColor();
                splitContainer.Panel1.BackColor = primary;
                splitContainer.Panel2.BackColor = primary;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, primary, secondary, text);
            }
        }

        private static void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            if (tabControl == null) return;

            TabPage tabPage = tabControl.TabPages[e.Index];
            Rectangle tabRect = tabControl.GetTabRect(e.Index);
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Get colors based on theme and selection state
            Color backColor = isSelected ? GetTabActiveColor() : GetTabInactiveColor();
            Color textColor = GetTextColor();
            Color borderColor = GetBorderColor();

            // Fill the tab background
            using (Brush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, tabRect);
            }

            // Draw the tab border
            using (Pen borderPen = new Pen(borderColor))
            {
                // Draw top and side borders
                e.Graphics.DrawLine(borderPen, tabRect.Left, tabRect.Top, tabRect.Right - 1, tabRect.Top);
                e.Graphics.DrawLine(borderPen, tabRect.Left, tabRect.Top, tabRect.Left, tabRect.Bottom - 1);
                e.Graphics.DrawLine(borderPen, tabRect.Right - 1, tabRect.Top, tabRect.Right - 1, tabRect.Bottom - 1);
                
                // Don't draw bottom border for selected tab
                if (!isSelected)
                {
                    e.Graphics.DrawLine(borderPen, tabRect.Left, tabRect.Bottom - 1, tabRect.Right - 1, tabRect.Bottom - 1);
                }
            }

            // Draw the tab text
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            using (Brush textBrush = new SolidBrush(textColor))
            {
                // Adjust rectangle for text positioning
                Rectangle textRect = new Rectangle(tabRect.X, tabRect.Y + 3, tabRect.Width, tabRect.Height - 3);
                e.Graphics.DrawString(tabPage.Text, tabPage.Font ?? tabControl.Font, textBrush, textRect, stringFormat);
            }
        }

        public static Color GetPrimaryColor()
        {
            if (CurrentTheme == Theme.Dark) return DarkThemePrimary;
            return LightThemePrimary;
        }

        public static Color GetSecondaryColor()
        {
            if (CurrentTheme == Theme.Dark) return DarkThemeSecondary;
            return LightThemeSecondary;
        }

        public static Color GetTextColor()
        {
            if (CurrentTheme == Theme.Dark) return DarkThemeText;
            return LightThemeText;
        }

        public static Color GetTabActiveColor()
        {
            if (CurrentTheme == Theme.Dark) return DarkThemeTabActive;
            return LightThemeTabActive;
        }

        public static Color GetTabInactiveColor()
        {
            if (CurrentTheme == Theme.Dark) return DarkThemeTabInactive;
            return LightThemeTabInactive;
        }

        public static Color GetBorderColor()
        {
            if (CurrentTheme == Theme.Dark) return DarkThemeBorder;
            return LightThemeBorder;
        }

        public static Color GetTabPageColor()
        {
            if (CurrentTheme == Theme.Dark) return DarkThemeTabActive;
            return LightThemeTabActive;
        }

        public static Color GetSelectionColor()
        {
            if (CurrentTheme == Theme.Dark) return Color.FromArgb(51, 153, 255);
            return SystemColors.Highlight;
        }
    }

    // Custom renderer for themed menus
    public class ThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemeMenuRenderer() : base(new ThemeColorTable()) { }
    }

    // Custom renderer for themed toolstrips
    public class ThemeToolStripRenderer : ToolStripProfessionalRenderer
    {
        public ThemeToolStripRenderer() : base(new ThemeColorTable()) { }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Don't render the border for a cleaner look
        }
    }

    // Custom color table for theme colors
    public class ThemeColorTable : ProfessionalColorTable
    {
        public override Color MenuStripGradientBegin => ThemeManager.GetPrimaryColor();
        public override Color MenuStripGradientEnd => ThemeManager.GetPrimaryColor();
        public override Color MenuBorder => ThemeManager.GetBorderColor();
        public override Color MenuItemBorder => ThemeManager.GetBorderColor();
        public override Color MenuItemSelected => ThemeManager.GetSecondaryColor();
        public override Color MenuItemSelectedGradientBegin => ThemeManager.GetSecondaryColor();
        public override Color MenuItemSelectedGradientEnd => ThemeManager.GetSecondaryColor();
        public override Color MenuItemPressedGradientBegin => ThemeManager.GetSecondaryColor();
        public override Color MenuItemPressedGradientEnd => ThemeManager.GetSecondaryColor();
        public override Color ToolStripDropDownBackground => ThemeManager.GetPrimaryColor();
        public override Color ImageMarginGradientBegin => ThemeManager.GetPrimaryColor();
        public override Color ImageMarginGradientMiddle => ThemeManager.GetPrimaryColor();
        public override Color ImageMarginGradientEnd => ThemeManager.GetPrimaryColor();
        public override Color ToolStripBorder => ThemeManager.GetBorderColor();
        public override Color ToolStripGradientBegin => ThemeManager.GetPrimaryColor();
        public override Color ToolStripGradientMiddle => ThemeManager.GetPrimaryColor();
        public override Color ToolStripGradientEnd => ThemeManager.GetPrimaryColor();
        public override Color StatusStripGradientBegin => ThemeManager.GetPrimaryColor();
        public override Color StatusStripGradientEnd => ThemeManager.GetPrimaryColor();
    }
}

