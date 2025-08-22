using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

namespace SqlServerManager
{
    public class ConnectionDialog : Form
    {
        private TextBox serverTextBox;
        private ComboBox authenticationComboBox;
        private TextBox usernameTextBox;
        private TextBox passwordTextBox;
        private TextBox databaseTextBox;
        private Button testButton;
        private Button okButton;
        private Button cancelButton;
        private CheckBox savePasswordCheckBox;
        private CheckBox trustServerCertCheckBox;
        private ComboBox savedConnectionsComboBox;
        private Label serverLabel;
        private Label authLabel;
        private Label usernameLabel;
        private Label passwordLabel;
        private Label databaseLabel;
        private Label savedConnectionsLabel;
        private Label timeoutLabel;
        private NumericUpDown timeoutNumeric;
        private ProgressBar connectionProgressBar;
        private Label statusLabel;
        private Button cancelConnectionButton;
        private CancellationTokenSource cancellationTokenSource;
        
        public string ConnectionString { get; private set; }
        
        public string ServerName
        {
            get => serverTextBox.Text;
            set => serverTextBox.Text = value;
        }

        public ConnectionDialog()
        {
            InitializeComponent();
            LoadSavedConnections();
            
            // Apply theme and fonts
            try
            {
                ThemeManager.ApplyThemeToDialog(this);
                FontManager.ApplyFontSize(this, FontManager.CurrentFontSize / 10f);
            }
            catch
            {
                // Use default styling if theme/font managers not available
                this.Font = new Font("Segoe UI", 9F);
            }
        }

        private void InitializeComponent()
        {
            savedConnectionsLabel = new Label();
            savedConnectionsComboBox = new ComboBox();
            serverLabel = new Label();
            serverTextBox = new TextBox();
            authLabel = new Label();
            authenticationComboBox = new ComboBox();
            usernameLabel = new Label();
            usernameTextBox = new TextBox();
            passwordLabel = new Label();
            passwordTextBox = new TextBox();
            savePasswordCheckBox = new CheckBox();
            trustServerCertCheckBox = new CheckBox();
            databaseLabel = new Label();
            databaseTextBox = new TextBox();
            timeoutLabel = new Label();
            timeoutNumeric = new NumericUpDown();
            connectionProgressBar = new ProgressBar();
            statusLabel = new Label();
            cancelConnectionButton = new Button();
            testButton = new Button();
            okButton = new Button();
            cancelButton = new Button();
            ((System.ComponentModel.ISupportInitialize)timeoutNumeric).BeginInit();
            SuspendLayout();
            // 
            // savedConnectionsLabel
            // 
            savedConnectionsLabel.Location = new Point(20, 20);
            savedConnectionsLabel.Name = "savedConnectionsLabel";
            savedConnectionsLabel.Size = new Size(120, 20);
            savedConnectionsLabel.TabIndex = 0;
            savedConnectionsLabel.Text = "Recent Connections:";
            // 
            // savedConnectionsComboBox
            // 
            savedConnectionsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            savedConnectionsComboBox.Location = new Point(140, 18);
            savedConnectionsComboBox.Name = "savedConnectionsComboBox";
            savedConnectionsComboBox.Size = new Size(270, 33);
            savedConnectionsComboBox.TabIndex = 1;
            savedConnectionsComboBox.SelectedIndexChanged += SavedConnectionsComboBox_SelectedIndexChanged;
            // 
            // serverLabel
            // 
            serverLabel.Location = new Point(20, 60);
            serverLabel.Name = "serverLabel";
            serverLabel.Size = new Size(100, 20);
            serverLabel.TabIndex = 2;
            serverLabel.Text = "Server:";
            // 
            // serverTextBox
            // 
            serverTextBox.Location = new Point(140, 58);
            serverTextBox.Name = "serverTextBox";
            serverTextBox.Size = new Size(270, 31);
            serverTextBox.TabIndex = 3;
            serverTextBox.Text = "localhost";
            // 
            // authLabel
            // 
            authLabel.Location = new Point(20, 95);
            authLabel.Name = "authLabel";
            authLabel.Size = new Size(100, 20);
            authLabel.TabIndex = 4;
            authLabel.Text = "Authentication:";
            // 
            // authenticationComboBox
            // 
            authenticationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            authenticationComboBox.Items.AddRange(new object[] { "Windows Authentication", "SQL Server Authentication" });
            authenticationComboBox.Location = new Point(140, 93);
            authenticationComboBox.Name = "authenticationComboBox";
            authenticationComboBox.Size = new Size(270, 33);
            authenticationComboBox.TabIndex = 5;
            authenticationComboBox.SelectedIndexChanged += AuthenticationComboBox_SelectedIndexChanged;
            // 
            // usernameLabel
            // 
            usernameLabel.Enabled = false;
            usernameLabel.Location = new Point(20, 130);
            usernameLabel.Name = "usernameLabel";
            usernameLabel.Size = new Size(100, 20);
            usernameLabel.TabIndex = 6;
            usernameLabel.Text = "Username:";
            // 
            // usernameTextBox
            // 
            usernameTextBox.Enabled = false;
            usernameTextBox.Location = new Point(140, 128);
            usernameTextBox.Name = "usernameTextBox";
            usernameTextBox.Size = new Size(270, 31);
            usernameTextBox.TabIndex = 7;
            // 
            // passwordLabel
            // 
            passwordLabel.Enabled = false;
            passwordLabel.Location = new Point(20, 165);
            passwordLabel.Name = "passwordLabel";
            passwordLabel.Size = new Size(100, 20);
            passwordLabel.TabIndex = 8;
            passwordLabel.Text = "Password:";
            // 
            // passwordTextBox
            // 
            passwordTextBox.Enabled = false;
            passwordTextBox.Location = new Point(140, 163);
            passwordTextBox.Name = "passwordTextBox";
            passwordTextBox.Size = new Size(270, 31);
            passwordTextBox.TabIndex = 9;
            passwordTextBox.UseSystemPasswordChar = true;
            // 
            // savePasswordCheckBox
            // 
            savePasswordCheckBox.Enabled = false;
            savePasswordCheckBox.Location = new Point(140, 195);
            savePasswordCheckBox.Name = "savePasswordCheckBox";
            savePasswordCheckBox.Size = new Size(120, 25);
            savePasswordCheckBox.TabIndex = 10;
            savePasswordCheckBox.Text = "Save password";
            // 
            // trustServerCertCheckBox
            // 
            trustServerCertCheckBox.Checked = true;
            trustServerCertCheckBox.CheckState = CheckState.Checked;
            trustServerCertCheckBox.Location = new Point(270, 195);
            trustServerCertCheckBox.Name = "trustServerCertCheckBox";
            trustServerCertCheckBox.Size = new Size(140, 25);
            trustServerCertCheckBox.TabIndex = 11;
            trustServerCertCheckBox.Text = "Trust server certificate";
            // 
            // databaseLabel
            // 
            databaseLabel.Location = new Point(20, 230);
            databaseLabel.Name = "databaseLabel";
            databaseLabel.Size = new Size(115, 20);
            databaseLabel.TabIndex = 12;
            databaseLabel.Text = "Database (optional):";
            // 
            // databaseTextBox
            // 
            databaseTextBox.Location = new Point(140, 228);
            databaseTextBox.Name = "databaseTextBox";
            databaseTextBox.Size = new Size(270, 31);
            databaseTextBox.TabIndex = 13;
            databaseTextBox.Text = "master";
            // 
            // timeoutLabel
            // 
            timeoutLabel.Location = new Point(20, 260);
            timeoutLabel.Name = "timeoutLabel";
            timeoutLabel.Size = new Size(130, 20);
            timeoutLabel.TabIndex = 14;
            timeoutLabel.Text = "Connection Timeout (sec):";
            // 
            // timeoutNumeric
            // 
            timeoutNumeric.Location = new Point(150, 258);
            timeoutNumeric.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            timeoutNumeric.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            timeoutNumeric.Name = "timeoutNumeric";
            timeoutNumeric.Size = new Size(80, 31);
            timeoutNumeric.TabIndex = 15;
            timeoutNumeric.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // connectionProgressBar
            // 
            connectionProgressBar.Location = new Point(20, 290);
            connectionProgressBar.MarqueeAnimationSpeed = 50;
            connectionProgressBar.Name = "connectionProgressBar";
            connectionProgressBar.Size = new Size(390, 20);
            connectionProgressBar.Style = ProgressBarStyle.Marquee;
            connectionProgressBar.TabIndex = 17;
            connectionProgressBar.Visible = false;
            // 
            // statusLabel
            // 
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Location = new Point(20, 315);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(390, 20);
            statusLabel.TabIndex = 18;
            statusLabel.Text = "Ready to connect";
            // 
            // cancelConnectionButton
            // 
            cancelConnectionButton.Location = new Point(250, 258);
            cancelConnectionButton.Name = "cancelConnectionButton";
            cancelConnectionButton.Size = new Size(80, 31);
            cancelConnectionButton.TabIndex = 16;
            cancelConnectionButton.Text = "Cancel";
            cancelConnectionButton.Visible = false;
            cancelConnectionButton.Click += CancelConnectionButton_Click;
            // 
            // testButton
            // 
            testButton.Location = new Point(20, 350);
            testButton.Name = "testButton";
            testButton.Size = new Size(120, 30);
            testButton.TabIndex = 19;
            testButton.Text = "Test Connection";
            testButton.Click += TestButton_Click;
            // 
            // okButton
            // 
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(240, 350);
            okButton.Name = "okButton";
            okButton.Size = new Size(80, 30);
            okButton.TabIndex = 20;
            okButton.Text = "OK";
            okButton.Click += OkButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(330, 350);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(80, 30);
            cancelButton.TabIndex = 21;
            cancelButton.Text = "Cancel";
            // 
            // ConnectionDialog
            // 
            AcceptButton = okButton;
            CancelButton = cancelButton;
            ClientSize = new Size(428, 394);
            Controls.Add(savedConnectionsLabel);
            Controls.Add(savedConnectionsComboBox);
            Controls.Add(serverLabel);
            Controls.Add(serverTextBox);
            Controls.Add(authLabel);
            Controls.Add(authenticationComboBox);
            Controls.Add(usernameLabel);
            Controls.Add(usernameTextBox);
            Controls.Add(passwordLabel);
            Controls.Add(passwordTextBox);
            Controls.Add(savePasswordCheckBox);
            Controls.Add(trustServerCertCheckBox);
            Controls.Add(databaseLabel);
            Controls.Add(databaseTextBox);
            Controls.Add(timeoutLabel);
            Controls.Add(timeoutNumeric);
            Controls.Add(cancelConnectionButton);
            Controls.Add(connectionProgressBar);
            Controls.Add(statusLabel);
            Controls.Add(testButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConnectionDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Connect to SQL Server";
            ((System.ComponentModel.ISupportInitialize)timeoutNumeric).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void AuthenticationComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isSqlAuth = authenticationComboBox.SelectedIndex == 1;
            usernameLabel.Enabled = isSqlAuth;
            usernameTextBox.Enabled = isSqlAuth;
            passwordLabel.Enabled = isSqlAuth;
            passwordTextBox.Enabled = isSqlAuth;
            savePasswordCheckBox.Enabled = isSqlAuth;
            
            if (isSqlAuth && string.IsNullOrEmpty(usernameTextBox.Text))
            {
                usernameTextBox.Text = "sa";
            }
        }

        private void SavedConnectionsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (savedConnectionsComboBox.SelectedItem != null && 
                savedConnectionsComboBox.SelectedItem is SavedConnection savedConn)
            {
                serverTextBox.Text = savedConn.Server;
                authenticationComboBox.SelectedIndex = savedConn.UseWindowsAuth ? 0 : 1;
                if (!savedConn.UseWindowsAuth)
                {
                    usernameTextBox.Text = savedConn.Username;
                    passwordTextBox.Text = savedConn.Password ?? "";
                    savePasswordCheckBox.Checked = !string.IsNullOrEmpty(savedConn.Password);
                }
                databaseTextBox.Text = savedConn.Database;
            }
        }

        private string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder();
            builder.DataSource = serverTextBox.Text;
            
            if (authenticationComboBox.SelectedIndex == 0)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = usernameTextBox.Text;
                builder.Password = passwordTextBox.Text;
            }
            
            if (!string.IsNullOrEmpty(databaseTextBox.Text))
            {
                builder.InitialCatalog = databaseTextBox.Text;
            }
            
            builder.ConnectTimeout = (int)timeoutNumeric.Value; // Use user-specified timeout
            builder.TrustServerCertificate = trustServerCertCheckBox.Checked; // User-controlled certificate trust
            builder.MultipleActiveResultSets = true; // Enable MARS for TableDataEditor
            return builder.ToString();
        }

        private async void TestButton_Click(object sender, EventArgs e)
        {
            await TestConnectionAsync(isTestOnly: true);
        }

        private async void OkButton_Click(object sender, EventArgs e)
        {
            bool success = await TestConnectionAsync(isTestOnly: false);
            if (success)
            {
                // Save the connection with password if requested
                if (authenticationComboBox.SelectedIndex == 1 && savePasswordCheckBox.Checked)
                {
                    SaveConnectionWithPassword();
                }
                
                // Connection successful, close dialog
                this.DialogResult = DialogResult.OK;
            }
        }

        private async Task<bool> TestConnectionAsync(bool isTestOnly)
        {
            // Create cancellation token source
            cancellationTokenSource = new CancellationTokenSource();
            
            // Show progress UI
            ShowConnectionProgress(isTestOnly);
            
            try
            {
                var connectionString = BuildConnectionString();
                if (!isTestOnly)
                {
                    ConnectionString = connectionString;
                }
                
                statusLabel.Text = "Connecting to server...";
                statusLabel.ForeColor = Color.Blue;
                
                using (var connection = new SqlConnection(connectionString))
                {
                    // Use async open with cancellation token
                    await connection.OpenAsync(cancellationTokenSource.Token);
                    
                    statusLabel.Text = "Testing connection...";
                    
                    using (var command = new SqlCommand("SELECT @@VERSION", connection))
                    {
                        command.CommandTimeout = (int)timeoutNumeric.Value;
                        await command.ExecuteScalarAsync(cancellationTokenSource.Token);
                    }
                }
                
                // Success
                statusLabel.Text = "Connection successful!";
                statusLabel.ForeColor = Color.Green;
                
                if (isTestOnly)
                {
                    MessageBox.Show("Connection test successful!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                return true;
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Connection cancelled";
                statusLabel.ForeColor = Color.Orange;
                return false;
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Connection failed: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
                
                if (isTestOnly)
                {
                    MessageBox.Show($"Connection test failed:\n{ex.Message}", "Connection Failed", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Cannot connect to server:\n{ex.Message}", "Connection Failed", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                return false;
            }
            finally
            {
                HideConnectionProgress(isTestOnly);
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }
        
        private void ShowConnectionProgress(bool isTestOnly)
        {
            // Disable controls
            testButton.Enabled = false;
            okButton.Enabled = false;
            timeoutNumeric.Enabled = false;
            
            // Update button text
            if (isTestOnly)
            {
                testButton.Text = "Testing...";
            }
            else
            {
                okButton.Text = "Connecting...";
            }
            
            // Show progress
            connectionProgressBar.Visible = true;
            cancelConnectionButton.Visible = true;
        }
        
        private void HideConnectionProgress(bool isTestOnly)
        {
            // Re-enable controls
            testButton.Enabled = true;
            okButton.Enabled = true;
            timeoutNumeric.Enabled = true;
            
            // Reset button text
            testButton.Text = "Test Connection";
            okButton.Text = "OK";
            
            // Hide progress
            connectionProgressBar.Visible = false;
            cancelConnectionButton.Visible = false;
        }
        
        private void CancelConnectionButton_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                statusLabel.Text = "Cancelling connection...";
                statusLabel.ForeColor = Color.Orange;
            }
        }
        
        private void SaveConnectionWithPassword()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                // Create a special connection string that includes encrypted password hint
                var builder = new SqlConnectionStringBuilder();
                builder.DataSource = serverTextBox.Text;
                builder.IntegratedSecurity = false;
                builder.UserID = usernameTextBox.Text;
                builder.InitialCatalog = databaseTextBox.Text;
                
                // Store password separately in a more secure way (still not perfect, but better)
                string key = $"Pwd_{serverTextBox.Text}_{usernameTextBox.Text}";
                if (config.AppSettings.Settings[key] == null)
                {
                    config.AppSettings.Settings.Add(key, Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes(passwordTextBox.Text)));
                }
                else
                {
                    config.AppSettings.Settings[key].Value = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes(passwordTextBox.Text));
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch
            {
                // Silent fail for password saving
            }
        }

        private void LoadSavedConnections()
        {
            try
            {
                var savedConnections = ConfigurationManager.AppSettings["SavedConnections"];
                if (!string.IsNullOrEmpty(savedConnections))
                {
                    savedConnectionsComboBox.Items.Add("-- Select a saved connection --");
                    
                    var connections = savedConnections.Split('|');
                    foreach (var connStr in connections)
                    {
                        try
                        {
                            var builder = new SqlConnectionStringBuilder(connStr);
                            var savedConn = new SavedConnection
                            {
                                Server = builder.DataSource,
                                UseWindowsAuth = builder.IntegratedSecurity,
                                Username = builder.UserID,
                                Database = builder.InitialCatalog,
                                ConnectionString = connStr
                            };
                            
                            // Try to load saved password if SQL Auth
                            if (!builder.IntegratedSecurity && !string.IsNullOrEmpty(builder.UserID))
                            {
                                try
                                {
                                    string key = $"Pwd_{builder.DataSource}_{builder.UserID}";
                                    var encryptedPwd = ConfigurationManager.AppSettings[key];
                                    if (!string.IsNullOrEmpty(encryptedPwd))
                                    {
                                        savedConn.Password = System.Text.Encoding.UTF8.GetString(
                                            Convert.FromBase64String(encryptedPwd));
                                    }
                                }
                                catch
                                {
                                    // Ignore password loading errors
                                }
                            }
                            
                            savedConnectionsComboBox.Items.Add(savedConn);
                        }
                        catch
                        {
                            // Skip invalid connection strings
                        }
                    }
                    
                    if (savedConnectionsComboBox.Items.Count > 1)
                    {
                        savedConnectionsComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch
            {
                // No saved connections available
            }
        }

        private class SavedConnection
        {
            public string Server { get; set; }
            public bool UseWindowsAuth { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Database { get; set; }
            public string ConnectionString { get; set; }
            
            public override string ToString()
            {
                var auth = UseWindowsAuth ? "Windows Auth" : $"SQL Auth ({Username})";
                var pwdInfo = !UseWindowsAuth && !string.IsNullOrEmpty(Password) ? " [Saved]" : "";
                return $"{Server} - {auth}{pwdInfo} - {Database}";
            }
        }
    }
}
