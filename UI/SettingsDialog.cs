using System;
using System.Drawing;
using System.Windows.Forms;
using SqlServerManager.Services;
using SqlServerManager.UI;

namespace SqlServerManager.UI
{
    public partial class SettingsDialog : Form
    {
        private TabControl tabControl;
        private Button okButton;
        private Button cancelButton;
        private Button applyButton;
        
        // Appearance tab controls
        private ComboBox themeComboBox;
        private TrackBar fontScaleTrackBar;
        private Label fontScaleLabel;
        private CheckBox enableAnimationsCheckBox;
        private CheckBox showStatusBarCheckBox;
        private CheckBox showToolbarCheckBox;
        
        // Connection tab controls
        private NumericUpDown connectionTimeoutNumeric;
        private NumericUpDown queryTimeoutNumeric;
        private NumericUpDown maxRecentConnectionsNumeric;
        private CheckBox autoConnectCheckBox;
        private CheckBox validateConnectionsCheckBox;
        private CheckBox enableAutoReconnectCheckBox;
        private CheckBox rememberLastConnectionCheckBox;
        private CheckBox autoReconnectOnStartupCheckBox;
        private CheckBox showReconnectDialogCheckBox;
        private NumericUpDown reconnectTimeoutNumeric;
        private NumericUpDown maxReconnectAttemptsNumeric;
        
        // Editor tab controls
        private CheckBox enableSyntaxHighlightingCheckBox;
        private CheckBox showLineNumbersCheckBox;
        private CheckBox enableAutoCompleteCheckBox;
        private CheckBox wordWrapCheckBox;
        private NumericUpDown tabSizeNumeric;
        private ComboBox fontFamilyComboBox;
        private NumericUpDown fontSizeNumeric;
        
        // Data Grid tab controls
        private NumericUpDown pageSizeNumeric;
        private CheckBox autoSizeColumnsCheckBox;
        private CheckBox showGridLinesCheckBox;
        private CheckBox alternateRowColorsCheckBox;

        public SettingsDialog()
        {
            InitializeComponent();
            LoadSettings();
            ModernThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = "Settings";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // Create main layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            // Create tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ModernThemeManager.GetScaledFont(this.Font)
            };

            // Create tabs
            CreateAppearanceTab();
            CreateConnectionTab();
            CreateEditorTab();
            CreateDataGridTab();

            // Create button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 50
            };

            okButton = new Button
            {
                Text = "OK",
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.OK,
                Location = new Point(buttonPanel.Width - 245, 10)
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.Cancel,
                Location = new Point(buttonPanel.Width - 165, 10)
            };

            applyButton = new Button
            {
                Text = "Apply",
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(buttonPanel.Width - 85, 10)
            };
            applyButton.Click += ApplyButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { okButton, cancelButton, applyButton });

            mainPanel.Controls.Add(tabControl, 0, 0);
            mainPanel.Controls.Add(buttonPanel, 0, 1);
            this.Controls.Add(mainPanel);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void CreateAppearanceTab()
        {
            var tab = new TabPage("Appearance");
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 2,
                Padding = new Padding(10)
            };

            // Theme selection
            var themeLabel = new Label { Text = "Theme:", Anchor = AnchorStyles.Left };
            themeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            themeComboBox.Items.AddRange(new object[] { "Light", "Dark", "System", "High Contrast" });

            // Font scale
            var fontLabel = new Label { Text = "Font Scale:", Anchor = AnchorStyles.Left };
            var fontPanel = new Panel { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            fontScaleTrackBar = new TrackBar
            {
                Minimum = 75,
                Maximum = 200,
                Value = 100,
                TickFrequency = 25,
                Dock = DockStyle.Top
            };
            fontScaleLabel = new Label
            {
                Text = "100%",
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter
            };
            fontScaleTrackBar.ValueChanged += FontScaleTrackBar_ValueChanged;
            fontPanel.Controls.AddRange(new Control[] { fontScaleTrackBar, fontScaleLabel });

            // Checkboxes
            enableAnimationsCheckBox = new CheckBox { Text = "Enable animations", Anchor = AnchorStyles.Left };
            showStatusBarCheckBox = new CheckBox { Text = "Show status bar", Anchor = AnchorStyles.Left };
            showToolbarCheckBox = new CheckBox { Text = "Show toolbar", Anchor = AnchorStyles.Left };

            panel.Controls.AddRange(new Control[]
            {
                themeLabel, themeComboBox,
                fontLabel, fontPanel,
                enableAnimationsCheckBox, new Label(),
                showStatusBarCheckBox, new Label(),
                showToolbarCheckBox, new Label()
            });

            tab.Controls.Add(panel);
            tabControl.TabPages.Add(tab);
        }

        private void CreateConnectionTab()
        {
            var tab = new TabPage("Connection");
            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                RowCount = 12,
                ColumnCount = 2,
                Padding = new Padding(10),
                AutoSize = true
            };

            // Connection timeout
            var connectionTimeoutLabel = new Label { Text = "Connection timeout (seconds):", Anchor = AnchorStyles.Left };
            connectionTimeoutNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 300,
                Value = 15,
                Anchor = AnchorStyles.Left
            };

            // Query timeout
            var queryTimeoutLabel = new Label { Text = "Query timeout (seconds):", Anchor = AnchorStyles.Left };
            queryTimeoutNumeric = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 3600,
                Value = 30,
                Anchor = AnchorStyles.Left
            };

            // Max recent connections
            var maxRecentLabel = new Label { Text = "Max recent connections:", Anchor = AnchorStyles.Left };
            maxRecentConnectionsNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 50,
                Value = 10,
                Anchor = AnchorStyles.Left
            };

            // Basic connection checkboxes
            autoConnectCheckBox = new CheckBox { Text = "Auto-connect on startup (deprecated)", Anchor = AnchorStyles.Left };
            validateConnectionsCheckBox = new CheckBox { Text = "Validate connections", Anchor = AnchorStyles.Left };

            // Auto-reconnect section separator
            var reconnectSeparator = new GroupBox
            {
                Text = "Auto-Reconnect Settings",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 2,
                FlatStyle = FlatStyle.Flat
            };

            // Auto-reconnect settings
            enableAutoReconnectCheckBox = new CheckBox { Text = "Enable auto-reconnect", Anchor = AnchorStyles.Left };
            rememberLastConnectionCheckBox = new CheckBox { Text = "Remember last connection", Anchor = AnchorStyles.Left };
            autoReconnectOnStartupCheckBox = new CheckBox { Text = "Auto-reconnect on startup", Anchor = AnchorStyles.Left };
            showReconnectDialogCheckBox = new CheckBox { Text = "Show reconnect dialog", Anchor = AnchorStyles.Left };

            var reconnectTimeoutLabel = new Label { Text = "Reconnect timeout (seconds):", Anchor = AnchorStyles.Left };
            reconnectTimeoutNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 60,
                Value = 10,
                Anchor = AnchorStyles.Left
            };

            var maxReconnectAttemptsLabel = new Label { Text = "Max reconnect attempts:", Anchor = AnchorStyles.Left };
            maxReconnectAttemptsNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10,
                Value = 3,
                Anchor = AnchorStyles.Left
            };

            panel.Controls.AddRange(new Control[]
            {
                connectionTimeoutLabel, connectionTimeoutNumeric,
                queryTimeoutLabel, queryTimeoutNumeric,
                maxRecentLabel, maxRecentConnectionsNumeric,
                autoConnectCheckBox, new Label(),
                validateConnectionsCheckBox, new Label(),
                reconnectSeparator, new Label(),
                enableAutoReconnectCheckBox, new Label(),
                rememberLastConnectionCheckBox, new Label(),
                autoReconnectOnStartupCheckBox, new Label(),
                showReconnectDialogCheckBox, new Label(),
                reconnectTimeoutLabel, reconnectTimeoutNumeric,
                maxReconnectAttemptsLabel, maxReconnectAttemptsNumeric
            });

            scrollPanel.Controls.Add(panel);
            tab.Controls.Add(scrollPanel);
            tabControl.TabPages.Add(tab);
        }

        private void CreateEditorTab()
        {
            var tab = new TabPage("Editor");
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 8,
                ColumnCount = 2,
                Padding = new Padding(10)
            };

            // Font family
            var fontFamilyLabel = new Label { Text = "Font family:", Anchor = AnchorStyles.Left };
            fontFamilyComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            // Add common monospace fonts
            fontFamilyComboBox.Items.AddRange(new object[] 
            { 
                "Consolas", "Courier New", "Lucida Console", "Monaco", "Source Code Pro", "Fira Code" 
            });

            // Font size
            var fontSizeLabel = new Label { Text = "Font size:", Anchor = AnchorStyles.Left };
            fontSizeNumeric = new NumericUpDown
            {
                Minimum = 6,
                Maximum = 72,
                Value = 9,
                Anchor = AnchorStyles.Left
            };

            // Tab size
            var tabSizeLabel = new Label { Text = "Tab size:", Anchor = AnchorStyles.Left };
            tabSizeNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 16,
                Value = 4,
                Anchor = AnchorStyles.Left
            };

            // Checkboxes
            enableSyntaxHighlightingCheckBox = new CheckBox { Text = "Enable syntax highlighting", Anchor = AnchorStyles.Left };
            showLineNumbersCheckBox = new CheckBox { Text = "Show line numbers", Anchor = AnchorStyles.Left };
            enableAutoCompleteCheckBox = new CheckBox { Text = "Enable auto-completion", Anchor = AnchorStyles.Left };
            wordWrapCheckBox = new CheckBox { Text = "Enable word wrap", Anchor = AnchorStyles.Left };

            panel.Controls.AddRange(new Control[]
            {
                fontFamilyLabel, fontFamilyComboBox,
                fontSizeLabel, fontSizeNumeric,
                tabSizeLabel, tabSizeNumeric,
                enableSyntaxHighlightingCheckBox, new Label(),
                showLineNumbersCheckBox, new Label(),
                enableAutoCompleteCheckBox, new Label(),
                wordWrapCheckBox, new Label()
            });

            tab.Controls.Add(panel);
            tabControl.TabPages.Add(tab);
        }

        private void CreateDataGridTab()
        {
            var tab = new TabPage("Data Grid");
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 2,
                Padding = new Padding(10)
            };

            // Page size
            var pageSizeLabel = new Label { Text = "Page size (rows):", Anchor = AnchorStyles.Left };
            pageSizeNumeric = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 10000,
                Value = 1000,
                Increment = 100,
                Anchor = AnchorStyles.Left
            };

            // Checkboxes
            autoSizeColumnsCheckBox = new CheckBox { Text = "Auto-size columns", Anchor = AnchorStyles.Left };
            showGridLinesCheckBox = new CheckBox { Text = "Show grid lines", Anchor = AnchorStyles.Left };
            alternateRowColorsCheckBox = new CheckBox { Text = "Alternate row colors", Anchor = AnchorStyles.Left };

            panel.Controls.AddRange(new Control[]
            {
                pageSizeLabel, pageSizeNumeric,
                autoSizeColumnsCheckBox, new Label(),
                showGridLinesCheckBox, new Label(),
                alternateRowColorsCheckBox, new Label()
            });

            tab.Controls.Add(panel);
            tabControl.TabPages.Add(tab);
        }

        private void LoadSettings()
        {
            try
            {
                // Appearance settings
                themeComboBox.SelectedItem = ModernThemeManager.CurrentTheme.ToString();
                var fontScale = (int)(ModernThemeManager.FontScale * 100);
                fontScaleTrackBar.Value = Math.Max(75, Math.Min(200, fontScale));
                fontScaleLabel.Text = $"{fontScale}%";

                // Connection settings
                connectionTimeoutNumeric.Value = ConfigurationService.GetValue<int>("Database:DefaultConnectionTimeout", 15);
                queryTimeoutNumeric.Value = ConfigurationService.GetValue<int>("Database:CommandTimeout", 30);
                maxRecentConnectionsNumeric.Value = ConfigurationService.GetValue<int>("Application:MaxRecentConnections", 10);
                
                // Auto-reconnect settings
                enableAutoReconnectCheckBox.Checked = ConfigurationService.GetValue<bool>("Connection:EnableAutoReconnect", false);
                rememberLastConnectionCheckBox.Checked = ConfigurationService.GetValue<bool>("Connection:RememberLastConnection", true);
                autoReconnectOnStartupCheckBox.Checked = ConfigurationService.GetValue<bool>("Connection:AutoReconnectOnStartup", false);
                showReconnectDialogCheckBox.Checked = ConfigurationService.GetValue<bool>("Connection:ShowReconnectDialog", true);
                reconnectTimeoutNumeric.Value = ConfigurationService.GetValue<int>("Connection:ReconnectTimeoutSeconds", 10);
                maxReconnectAttemptsNumeric.Value = ConfigurationService.GetValue<int>("Connection:MaxReconnectAttempts", 3);

                // Editor settings
                var fontFamily = ConfigurationService.GetValue<string>("Editor:FontFamily", "Consolas");
                if (fontFamilyComboBox.Items.Contains(fontFamily))
                    fontFamilyComboBox.SelectedItem = fontFamily;
                else
                    fontFamilyComboBox.SelectedIndex = 0;

                fontSizeNumeric.Value = ConfigurationService.GetValue<decimal>("Editor:FontSize", 9);
                tabSizeNumeric.Value = ConfigurationService.GetValue<decimal>("Editor:TabSize", 4);
                enableSyntaxHighlightingCheckBox.Checked = ConfigurationService.GetValue<bool>("Editor:SyntaxHighlighting", true);
                showLineNumbersCheckBox.Checked = ConfigurationService.GetValue<bool>("Editor:ShowLineNumbers", true);
                enableAutoCompleteCheckBox.Checked = ConfigurationService.GetValue<bool>("Editor:AutoComplete", true);
                wordWrapCheckBox.Checked = ConfigurationService.GetValue<bool>("Editor:WordWrap", false);

                // Data grid settings
                pageSizeNumeric.Value = ConfigurationService.GetValue<decimal>("UI:DataGrid:PageSize", 1000);
                autoSizeColumnsCheckBox.Checked = ConfigurationService.GetValue<bool>("UI:DataGrid:AutoSizeColumns", true);
                showGridLinesCheckBox.Checked = ConfigurationService.GetValue<bool>("UI:DataGrid:ShowGridLines", true);
                alternateRowColorsCheckBox.Checked = ConfigurationService.GetValue<bool>("UI:DataGrid:AlternateRowColors", true);

                // UI settings
                enableAnimationsCheckBox.Checked = ConfigurationService.GetValue<bool>("UI:EnableAnimations", true);
                showStatusBarCheckBox.Checked = ConfigurationService.GetValue<bool>("UI:ShowStatusBar", true);
                showToolbarCheckBox.Checked = ConfigurationService.GetValue<bool>("UI:ShowToolbar", true);
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning("Failed to load settings: {Message}", ex.Message);
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Appearance settings
                if (Enum.TryParse<Theme>(themeComboBox.SelectedItem?.ToString(), out var theme))
                {
                    ModernThemeManager.CurrentTheme = theme;
                }
                ModernThemeManager.FontScale = fontScaleTrackBar.Value / 100f;

                // Connection settings
                ConfigurationService.SaveSetting("Database:DefaultConnectionTimeout", (int)connectionTimeoutNumeric.Value);
                ConfigurationService.SaveSetting("Database:CommandTimeout", (int)queryTimeoutNumeric.Value);
                ConfigurationService.SaveSetting("Application:MaxRecentConnections", (int)maxRecentConnectionsNumeric.Value);
                
                // Auto-reconnect settings
                ConfigurationService.SaveSetting("Connection:EnableAutoReconnect", enableAutoReconnectCheckBox.Checked);
                ConfigurationService.SaveSetting("Connection:RememberLastConnection", rememberLastConnectionCheckBox.Checked);
                ConfigurationService.SaveSetting("Connection:AutoReconnectOnStartup", autoReconnectOnStartupCheckBox.Checked);
                ConfigurationService.SaveSetting("Connection:ShowReconnectDialog", showReconnectDialogCheckBox.Checked);
                ConfigurationService.SaveSetting("Connection:ReconnectTimeoutSeconds", (int)reconnectTimeoutNumeric.Value);
                ConfigurationService.SaveSetting("Connection:MaxReconnectAttempts", (int)maxReconnectAttemptsNumeric.Value);

                // Editor settings
                ConfigurationService.SaveSetting("Editor:FontFamily", fontFamilyComboBox.SelectedItem?.ToString() ?? "Consolas");
                ConfigurationService.SaveSetting("Editor:FontSize", (float)fontSizeNumeric.Value);
                ConfigurationService.SaveSetting("Editor:TabSize", (int)tabSizeNumeric.Value);
                ConfigurationService.SaveSetting("Editor:SyntaxHighlighting", enableSyntaxHighlightingCheckBox.Checked);
                ConfigurationService.SaveSetting("Editor:ShowLineNumbers", showLineNumbersCheckBox.Checked);
                ConfigurationService.SaveSetting("Editor:AutoComplete", enableAutoCompleteCheckBox.Checked);
                ConfigurationService.SaveSetting("Editor:WordWrap", wordWrapCheckBox.Checked);

                // Data grid settings
                ConfigurationService.SaveSetting("UI:DataGrid:PageSize", (int)pageSizeNumeric.Value);
                ConfigurationService.SaveSetting("UI:DataGrid:AutoSizeColumns", autoSizeColumnsCheckBox.Checked);
                ConfigurationService.SaveSetting("UI:DataGrid:ShowGridLines", showGridLinesCheckBox.Checked);
                ConfigurationService.SaveSetting("UI:DataGrid:AlternateRowColors", alternateRowColorsCheckBox.Checked);

                // UI settings
                ConfigurationService.SaveSetting("UI:EnableAnimations", enableAnimationsCheckBox.Checked);
                ConfigurationService.SaveSetting("UI:ShowStatusBar", showStatusBarCheckBox.Checked);
                ConfigurationService.SaveSetting("UI:ShowToolbar", showToolbarCheckBox.Checked);

                LoggingService.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to save settings: {Message}", ex.Message);
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FontScaleTrackBar_ValueChanged(object sender, EventArgs e)
        {
            fontScaleLabel.Text = $"{fontScaleTrackBar.Value}%";
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ModernThemeManager.ApplyTheme(this);
        }
    }
}
