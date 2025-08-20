using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using SqlServerManager.Services;

namespace SqlServerManager.UI
{
    public enum Theme
    {
        Light,
        Dark,
        System,
        HighContrast
    }

    public class ThemeColors
    {
        public Color BackgroundPrimary { get; set; }
        public Color BackgroundSecondary { get; set; }
        public Color BackgroundTertiary { get; set; }
        public Color ForegroundPrimary { get; set; }
        public Color ForegroundSecondary { get; set; }
        public Color AccentColor { get; set; }
        public Color BorderColor { get; set; }
        public Color HighlightColor { get; set; }
        public Color ErrorColor { get; set; }
        public Color WarningColor { get; set; }
        public Color SuccessColor { get; set; }
        public Color InfoColor { get; set; }
        
        // Grid specific colors
        public Color GridBackgroundColor { get; set; }
        public Color GridHeaderColor { get; set; }
        public Color GridAlternateRowColor { get; set; }
        public Color GridSelectionColor { get; set; }
        
        // Editor specific colors
        public Color EditorBackgroundColor { get; set; }
        public Color EditorForegroundColor { get; set; }
        public Color EditorLineNumberColor { get; set; }
        public Color EditorSelectionColor { get; set; }
        
        // Button states
        public Color ButtonNormalColor { get; set; }
        public Color ButtonHoverColor { get; set; }
        public Color ButtonPressedColor { get; set; }
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public Theme OldTheme { get; }
        public Theme NewTheme { get; }
        public ThemeColors Colors { get; }
        public float FontScale { get; }

        public ThemeChangedEventArgs(Theme oldTheme, Theme newTheme, ThemeColors colors, float fontScale)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
            Colors = colors;
            FontScale = fontScale;
        }
    }

    public static class ModernThemeManager
    {
        private static Theme _currentTheme = Theme.System;
        private static ThemeColors _currentColors;
        private static float _fontScale = 1.0f;
        private static bool _isSystemDarkMode = false;
        
        public static event EventHandler<ThemeChangedEventArgs> ThemeChanged;
        
        static ModernThemeManager()
        {
            InitializeTheme();
            SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
        }

        public static Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                var oldTheme = _currentTheme;
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    UpdateCurrentColors();
                    SaveThemePreference();
                    OnThemeChanged(oldTheme);
                }
            }
        }
        
        public static ThemeColors CurrentColors => _currentColors;
        
        public static float FontScale
        {
            get => _fontScale;
            set
            {
                if (Math.Abs(_fontScale - value) > 0.01f)
                {
                    var oldTheme = _currentTheme;
                    _fontScale = Math.Max(0.75f, Math.Min(2.0f, value)); // Clamp between 75% and 200%
                    SaveFontScale();
                    OnThemeChanged(oldTheme);
                }
            }
        }

        private static void InitializeTheme()
        {
            // Load saved theme preference
            _currentTheme = LoadThemePreference();
            _fontScale = LoadFontScale();
            _isSystemDarkMode = IsSystemDarkMode();
            UpdateCurrentColors();
        }

        private static void UpdateCurrentColors()
        {
            var effectiveTheme = GetEffectiveTheme();
            _currentColors = effectiveTheme == Theme.Dark ? GetDarkThemeColors() : 
                           effectiveTheme == Theme.HighContrast ? GetHighContrastThemeColors() :
                           GetLightThemeColors();
        }

        private static Theme GetEffectiveTheme()
        {
            return _currentTheme switch
            {
                Theme.System => _isSystemDarkMode ? Theme.Dark : Theme.Light,
                Theme.HighContrast => Theme.HighContrast,
                _ => _currentTheme
            };
        }

        private static ThemeColors GetLightThemeColors()
        {
            return new ThemeColors
            {
                BackgroundPrimary = Color.FromArgb(255, 255, 255),
                BackgroundSecondary = Color.FromArgb(248, 249, 250),
                BackgroundTertiary = Color.FromArgb(233, 236, 239),
                ForegroundPrimary = Color.FromArgb(33, 37, 41),
                ForegroundSecondary = Color.FromArgb(108, 117, 125),
                AccentColor = Color.FromArgb(0, 123, 255),
                BorderColor = Color.FromArgb(206, 212, 218),
                HighlightColor = Color.FromArgb(232, 245, 255),
                ErrorColor = Color.FromArgb(220, 53, 69),
                WarningColor = Color.FromArgb(255, 193, 7),
                SuccessColor = Color.FromArgb(40, 167, 69),
                InfoColor = Color.FromArgb(23, 162, 184),
                
                GridBackgroundColor = Color.White,
                GridHeaderColor = Color.FromArgb(248, 249, 250),
                GridAlternateRowColor = Color.FromArgb(250, 250, 250),
                GridSelectionColor = Color.FromArgb(0, 123, 255),
                
                EditorBackgroundColor = Color.White,
                EditorForegroundColor = Color.FromArgb(33, 37, 41),
                EditorLineNumberColor = Color.FromArgb(108, 117, 125),
                EditorSelectionColor = Color.FromArgb(173, 214, 255),
                
                ButtonNormalColor = Color.FromArgb(248, 249, 250),
                ButtonHoverColor = Color.FromArgb(233, 236, 239),
                ButtonPressedColor = Color.FromArgb(206, 212, 218)
            };
        }

        private static ThemeColors GetDarkThemeColors()
        {
            return new ThemeColors
            {
                BackgroundPrimary = Color.FromArgb(32, 32, 32),
                BackgroundSecondary = Color.FromArgb(45, 45, 45),
                BackgroundTertiary = Color.FromArgb(60, 60, 60),
                ForegroundPrimary = Color.FromArgb(255, 255, 255),
                ForegroundSecondary = Color.FromArgb(200, 200, 200),
                AccentColor = Color.FromArgb(100, 181, 246),
                BorderColor = Color.FromArgb(80, 80, 80),
                HighlightColor = Color.FromArgb(45, 45, 48),
                ErrorColor = Color.FromArgb(244, 67, 54),
                WarningColor = Color.FromArgb(255, 235, 59),
                SuccessColor = Color.FromArgb(76, 175, 80),
                InfoColor = Color.FromArgb(33, 150, 243),
                
                GridBackgroundColor = Color.FromArgb(45, 45, 45),
                GridHeaderColor = Color.FromArgb(60, 60, 60),
                GridAlternateRowColor = Color.FromArgb(40, 40, 40),
                GridSelectionColor = Color.FromArgb(100, 181, 246),
                
                EditorBackgroundColor = Color.FromArgb(30, 30, 30),
                EditorForegroundColor = Color.FromArgb(255, 255, 255),
                EditorLineNumberColor = Color.FromArgb(150, 150, 150),
                EditorSelectionColor = Color.FromArgb(51, 153, 255),
                
                ButtonNormalColor = Color.FromArgb(60, 60, 60),
                ButtonHoverColor = Color.FromArgb(70, 70, 70),
                ButtonPressedColor = Color.FromArgb(50, 50, 50)
            };
        }

        private static ThemeColors GetHighContrastThemeColors()
        {
            return new ThemeColors
            {
                BackgroundPrimary = SystemColors.Window,
                BackgroundSecondary = SystemColors.Control,
                BackgroundTertiary = SystemColors.ControlLight,
                ForegroundPrimary = SystemColors.WindowText,
                ForegroundSecondary = SystemColors.ControlText,
                AccentColor = SystemColors.Highlight,
                BorderColor = SystemColors.ControlDark,
                HighlightColor = SystemColors.Highlight,
                ErrorColor = Color.Red,
                WarningColor = Color.Orange,
                SuccessColor = Color.Green,
                InfoColor = SystemColors.HotTrack,
                
                GridBackgroundColor = SystemColors.Window,
                GridHeaderColor = SystemColors.Control,
                GridAlternateRowColor = SystemColors.ControlLight,
                GridSelectionColor = SystemColors.Highlight,
                
                EditorBackgroundColor = SystemColors.Window,
                EditorForegroundColor = SystemColors.WindowText,
                EditorLineNumberColor = SystemColors.GrayText,
                EditorSelectionColor = SystemColors.Highlight,
                
                ButtonNormalColor = SystemColors.Control,
                ButtonHoverColor = SystemColors.ControlLight,
                ButtonPressedColor = SystemColors.ControlDark
            };
        }

        public static void ApplyTheme(Form form)
        {
            ApplyThemeToControl(form);
        }

        public static void ApplyThemeToControl(Control control)
        {
            if (control == null) return;

            var colors = CurrentColors;
            var scaledFont = GetScaledFont(control.Font);

            // Apply base styling
            control.BackColor = colors.BackgroundPrimary;
            control.ForeColor = colors.ForegroundPrimary;
            if (scaledFont != control.Font)
            {
                control.Font = scaledFont;
            }

            // Apply control-specific theming
            switch (control)
            {
                case Form form:
                    ApplyFormTheme(form, colors);
                    break;
                case Button button:
                    ApplyButtonTheme(button, colors);
                    break;
                case TextBox textBox:
                    ApplyTextBoxTheme(textBox, colors);
                    break;
                case DataGridView dgv:
                    ApplyDataGridViewTheme(dgv, colors);
                    break;
                case TabControl tabControl:
                    ApplyTabControlTheme(tabControl, colors);
                    break;
                case MenuStrip menuStrip:
                    ApplyMenuStripTheme(menuStrip, colors);
                    break;
                case StatusStrip statusStrip:
                    ApplyStatusStripTheme(statusStrip, colors);
                    break;
                case ToolStrip toolStrip:
                    ApplyToolStripTheme(toolStrip, colors);
                    break;
                case SplitContainer splitContainer:
                    ApplySplitContainerTheme(splitContainer, colors);
                    break;
                case Panel panel:
                    ApplyPanelTheme(panel, colors);
                    break;
                case GroupBox groupBox:
                    ApplyGroupBoxTheme(groupBox, colors);
                    break;
            }

            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }
        }

        private static void ApplyFormTheme(Form form, ThemeColors colors)
        {
            form.BackColor = colors.BackgroundPrimary;
            form.ForeColor = colors.ForegroundPrimary;
        }

        private static void ApplyButtonTheme(Button button, ThemeColors colors)
        {
            button.BackColor = colors.ButtonNormalColor;
            button.ForeColor = colors.ForegroundPrimary;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = colors.BorderColor;
            button.FlatAppearance.MouseOverBackColor = colors.ButtonHoverColor;
            button.FlatAppearance.MouseDownBackColor = colors.ButtonPressedColor;
        }

        private static void ApplyTextBoxTheme(TextBox textBox, ThemeColors colors)
        {
            textBox.BackColor = colors.BackgroundSecondary;
            textBox.ForeColor = colors.ForegroundPrimary;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void ApplyDataGridViewTheme(DataGridView dgv, ThemeColors colors)
        {
            dgv.BackgroundColor = colors.GridBackgroundColor;
            dgv.ForeColor = colors.ForegroundPrimary;
            dgv.GridColor = colors.BorderColor;
            dgv.BorderStyle = BorderStyle.None;
            
            // Cell styling
            dgv.DefaultCellStyle.BackColor = colors.GridBackgroundColor;
            dgv.DefaultCellStyle.ForeColor = colors.ForegroundPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = colors.GridSelectionColor;
            dgv.DefaultCellStyle.SelectionForeColor = colors.ForegroundPrimary;
            
            // Alternating row colors
            dgv.AlternatingRowsDefaultCellStyle.BackColor = colors.GridAlternateRowColor;
            
            // Header styling
            dgv.ColumnHeadersDefaultCellStyle.BackColor = colors.GridHeaderColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = colors.ForegroundPrimary;
            dgv.RowHeadersDefaultCellStyle.BackColor = colors.GridHeaderColor;
            dgv.RowHeadersDefaultCellStyle.ForeColor = colors.ForegroundPrimary;
            dgv.EnableHeadersVisualStyles = false;
        }

        private static void ApplyTabControlTheme(TabControl tabControl, ThemeColors colors)
        {
            tabControl.BackColor = colors.BackgroundPrimary;
            tabControl.ForeColor = colors.ForegroundPrimary;
            
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                tabPage.BackColor = colors.BackgroundSecondary;
                tabPage.ForeColor = colors.ForegroundPrimary;
            }
        }

        private static void ApplyMenuStripTheme(MenuStrip menuStrip, ThemeColors colors)
        {
            menuStrip.BackColor = colors.BackgroundPrimary;
            menuStrip.ForeColor = colors.ForegroundPrimary;
            menuStrip.Renderer = new ModernToolStripRenderer(colors);
        }

        private static void ApplyToolStripTheme(ToolStrip toolStrip, ThemeColors colors)
        {
            toolStrip.BackColor = colors.BackgroundPrimary;
            toolStrip.ForeColor = colors.ForegroundPrimary;
            toolStrip.Renderer = new ModernToolStripRenderer(colors);
        }

        private static void ApplyStatusStripTheme(StatusStrip statusStrip, ThemeColors colors)
        {
            statusStrip.BackColor = colors.BackgroundSecondary;
            statusStrip.ForeColor = colors.ForegroundPrimary;
            statusStrip.Renderer = new ModernToolStripRenderer(colors);
        }

        private static void ApplySplitContainerTheme(SplitContainer splitContainer, ThemeColors colors)
        {
            splitContainer.BackColor = colors.BorderColor;
            splitContainer.Panel1.BackColor = colors.BackgroundPrimary;
            splitContainer.Panel2.BackColor = colors.BackgroundPrimary;
        }

        private static void ApplyPanelTheme(Panel panel, ThemeColors colors)
        {
            panel.BackColor = colors.BackgroundPrimary;
            panel.ForeColor = colors.ForegroundPrimary;
        }

        private static void ApplyGroupBoxTheme(GroupBox groupBox, ThemeColors colors)
        {
            groupBox.BackColor = colors.BackgroundPrimary;
            groupBox.ForeColor = colors.ForegroundPrimary;
        }

        public static Font GetScaledFont(Font baseFont)
        {
            if (baseFont == null || Math.Abs(_fontScale - 1.0f) < 0.01f)
                return baseFont;
                
            var newSize = baseFont.Size * _fontScale;
            return new Font(baseFont.FontFamily, newSize, baseFont.Style, baseFont.Unit);
        }

        private static void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                var wasDarkMode = _isSystemDarkMode;
                _isSystemDarkMode = IsSystemDarkMode();
                
                if (_currentTheme == Theme.System && wasDarkMode != _isSystemDarkMode)
                {
                    UpdateCurrentColors();
                    OnThemeChanged(_currentTheme);
                }
            }
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false; // Default to light mode if we can't determine
            }
        }

        private static Theme LoadThemePreference()
        {
            try
            {
                var themeString = ConfigurationService.GetValue<string>("Application:Theme", "System");
                return Enum.TryParse<Theme>(themeString, true, out var theme) ? theme : Theme.System;
            }
            catch
            {
                return Theme.System;
            }
        }

        private static float LoadFontScale()
        {
            try
            {
                return ConfigurationService.GetValue<float>("Application:FontScale", 1.0f);
            }
            catch
            {
                return 1.0f;
            }
        }

        private static void SaveThemePreference()
        {
            try
            {
                ConfigurationService.SaveSetting("Application:Theme", _currentTheme.ToString());
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Failed to save theme preference: {Message}", ex.Message);
            }
        }

        private static void SaveFontScale()
        {
            try
            {
                ConfigurationService.SaveSetting("Application:FontScale", _fontScale);
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Failed to save font scale preference: {Message}", ex.Message);
            }
        }

        private static void OnThemeChanged(Theme oldTheme)
        {
            var args = new ThemeChangedEventArgs(oldTheme, _currentTheme, _currentColors, _fontScale);
            ThemeChanged?.Invoke(null, args);
        }
    }

    // Modern ToolStrip renderer
    public class ModernToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemeColors _colors;

        public ModernToolStripRenderer(ThemeColors colors) : base(new ModernColorTable(colors))
        {
            _colors = colors;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Render a subtle border
            using var pen = new Pen(_colors.BorderColor);
            var bounds = new Rectangle(0, e.ToolStrip.Height - 1, e.ToolStrip.Width, 1);
            e.Graphics.DrawRectangle(pen, bounds);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var button = e.Item as ToolStripButton;
            if (button != null && (button.Selected || button.Pressed))
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                using var brush = new SolidBrush(button.Pressed ? _colors.ButtonPressedColor : _colors.ButtonHoverColor);
                e.Graphics.FillRectangle(brush, bounds);
            }
        }
    }

    // Modern color table for ToolStrip theming
    public class ModernColorTable : ProfessionalColorTable
    {
        private readonly ThemeColors _colors;

        public ModernColorTable(ThemeColors colors)
        {
            _colors = colors;
        }

        public override Color MenuStripGradientBegin => _colors.BackgroundPrimary;
        public override Color MenuStripGradientEnd => _colors.BackgroundPrimary;
        public override Color MenuBorder => _colors.BorderColor;
        public override Color MenuItemBorder => _colors.BorderColor;
        public override Color MenuItemSelected => _colors.HighlightColor;
        public override Color MenuItemSelectedGradientBegin => _colors.HighlightColor;
        public override Color MenuItemSelectedGradientEnd => _colors.HighlightColor;
        public override Color MenuItemPressedGradientBegin => _colors.ButtonPressedColor;
        public override Color MenuItemPressedGradientEnd => _colors.ButtonPressedColor;
        public override Color ToolStripDropDownBackground => _colors.BackgroundPrimary;
        public override Color ImageMarginGradientBegin => _colors.BackgroundPrimary;
        public override Color ImageMarginGradientMiddle => _colors.BackgroundPrimary;
        public override Color ImageMarginGradientEnd => _colors.BackgroundPrimary;
        public override Color ToolStripBorder => _colors.BorderColor;
        public override Color ToolStripGradientBegin => _colors.BackgroundPrimary;
        public override Color ToolStripGradientMiddle => _colors.BackgroundPrimary;
        public override Color ToolStripGradientEnd => _colors.BackgroundPrimary;
        public override Color StatusStripGradientBegin => _colors.BackgroundSecondary;
        public override Color StatusStripGradientEnd => _colors.BackgroundSecondary;
    }
}
