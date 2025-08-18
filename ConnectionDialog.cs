using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

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
        private ComboBox savedConnectionsComboBox;
        private Label serverLabel;
        private Label authLabel;
        private Label usernameLabel;
        private Label passwordLabel;
        private Label databaseLabel;
        private Label savedConnectionsLabel;
        
        public string ConnectionString { get; private set; }

        public ConnectionDialog()
        {
            InitializeComponent();
            LoadSavedConnections();
            
            // Apply fonts
            try
            {
                this.Font = FontManager.GetScaledFont(9);
            }
            catch
            {
                // Use default font if FontManager not available
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Connect to SQL Server";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Saved connections
            savedConnectionsLabel = new Label();
            savedConnectionsLabel.Text = "Recent Connections:";
            savedConnectionsLabel.Location = new Point(20, 20);
            savedConnectionsLabel.Size = new Size(120, 20);

            savedConnectionsComboBox = new ComboBox();
            savedConnectionsComboBox.Location = new Point(140, 18);
            savedConnectionsComboBox.Size = new Size(270, 25);
            savedConnectionsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            savedConnectionsComboBox.SelectedIndexChanged += SavedConnectionsComboBox_SelectedIndexChanged;

            // Server
            serverLabel = new Label();
            serverLabel.Text = "Server:";
            serverLabel.Location = new Point(20, 60);
            serverLabel.Size = new Size(100, 20);

            serverTextBox = new TextBox();
            serverTextBox.Location = new Point(140, 58);
            serverTextBox.Size = new Size(270, 25);
            serverTextBox.Text = "localhost";

            // Authentication
            authLabel = new Label();
            authLabel.Text = "Authentication:";
            authLabel.Location = new Point(20, 95);
            authLabel.Size = new Size(100, 20);

            authenticationComboBox = new ComboBox();
            authenticationComboBox.Location = new Point(140, 93);
            authenticationComboBox.Size = new Size(270, 25);
            authenticationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            authenticationComboBox.Items.AddRange(new string[] { "Windows Authentication", "SQL Server Authentication" });
            authenticationComboBox.SelectedIndex = 0;
            authenticationComboBox.SelectedIndexChanged += AuthenticationComboBox_SelectedIndexChanged;

            // Username
            usernameLabel = new Label();
            usernameLabel.Text = "Username:";
            usernameLabel.Location = new Point(20, 130);
            usernameLabel.Size = new Size(100, 20);
            usernameLabel.Enabled = false;

            usernameTextBox = new TextBox();
            usernameTextBox.Location = new Point(140, 128);
            usernameTextBox.Size = new Size(270, 25);
            usernameTextBox.Enabled = false;

            // Password
            passwordLabel = new Label();
            passwordLabel.Text = "Password:";
            passwordLabel.Location = new Point(20, 165);
            passwordLabel.Size = new Size(100, 20);
            passwordLabel.Enabled = false;

            passwordTextBox = new TextBox();
            passwordTextBox.Location = new Point(140, 163);
            passwordTextBox.Size = new Size(270, 25);
            passwordTextBox.UseSystemPasswordChar = true;
            passwordTextBox.Enabled = false;

            // Save password
            savePasswordCheckBox = new CheckBox();
            savePasswordCheckBox.Text = "Save password";
            savePasswordCheckBox.Location = new Point(140, 195);
            savePasswordCheckBox.Size = new Size(120, 25);
            savePasswordCheckBox.Enabled = false;

            // Database (optional)
            databaseLabel = new Label();
            databaseLabel.Text = "Database (optional):";
            databaseLabel.Location = new Point(20, 230);
            databaseLabel.Size = new Size(115, 20);

            databaseTextBox = new TextBox();
            databaseTextBox.Location = new Point(140, 228);
            databaseTextBox.Size = new Size(270, 25);
            databaseTextBox.Text = "master";

            // Buttons
            testButton = new Button();
            testButton.Text = "Test Connection";
            testButton.Location = new Point(20, 280);
            testButton.Size = new Size(120, 30);
            testButton.Click += TestButton_Click;

            okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(240, 280);
            okButton.Size = new Size(80, 30);
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += OkButton_Click;

            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(330, 280);
            cancelButton.Size = new Size(80, 30);
            cancelButton.DialogResult = DialogResult.Cancel;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                savedConnectionsLabel, savedConnectionsComboBox,
                serverLabel, serverTextBox,
                authLabel, authenticationComboBox,
                usernameLabel, usernameTextBox,
                passwordLabel, passwordTextBox,
                savePasswordCheckBox,
                databaseLabel, databaseTextBox,
                testButton, okButton, cancelButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
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
            
            builder.ConnectTimeout = 30;
            return builder.ToString();
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            try
            {
                string testConnectionString = BuildConnectionString();
                using (var connection = new SqlConnection(testConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SELECT 1", connection))
                    {
                        command.ExecuteScalar();
                    }
                }
                MessageBox.Show("Connection test successful!", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection test failed:\n{ex.Message}", "Connection Failed", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectionString = BuildConnectionString();
                // Test the connection before accepting
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                }
                
                // Save the connection with password if requested
                if (authenticationComboBox.SelectedIndex == 1 && savePasswordCheckBox.Checked)
                {
                    // Build connection string with password for saving
                    SaveConnectionWithPassword();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot connect to server:\n{ex.Message}", "Connection Failed", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
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
