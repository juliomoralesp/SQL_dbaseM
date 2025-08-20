using System;
using System.Drawing;
using System.Windows.Forms;

namespace SqlServerManager.UI
{
    public partial class AddServerDialog : Form
    {
        private TextBox serverNameTextBox;
        private TextBox instanceTextBox;
        private ComboBox authenticationComboBox;
        private TextBox usernameTextBox;
        private TextBox passwordTextBox;
        private TextBox descriptionTextBox;
        private Button testConnectionButton;
        private Button okButton;
        private Button cancelButton;
        
        public string ServerName { get; private set; }
        public string InstanceName { get; private set; }
        public AuthenticationType AuthenticationType { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Description { get; private set; }

        public AddServerDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Add Server";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(20)
            };

            // Server Name
            var serverNameLabel = new Label { Text = "Server Name:", Anchor = AnchorStyles.Left, AutoSize = true };
            serverNameTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 200 };
            
            // Instance
            var instanceLabel = new Label { Text = "Instance (optional):", Anchor = AnchorStyles.Left, AutoSize = true };
            instanceTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 200 };
            
            // Authentication
            var authLabel = new Label { Text = "Authentication:", Anchor = AnchorStyles.Left, AutoSize = true };
            authenticationComboBox = new ComboBox 
            { 
                Anchor = AnchorStyles.Left | AnchorStyles.Right, 
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            authenticationComboBox.Items.AddRange(new object[] { "Windows Authentication", "SQL Server Authentication" });
            authenticationComboBox.SelectedIndex = 0;
            authenticationComboBox.SelectedIndexChanged += AuthenticationComboBox_SelectedIndexChanged;
            
            // Username
            var usernameLabel = new Label { Text = "Username:", Anchor = AnchorStyles.Left, AutoSize = true };
            usernameTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Enabled = false };
            
            // Password
            var passwordLabel = new Label { Text = "Password:", Anchor = AnchorStyles.Left, AutoSize = true };
            passwordTextBox = new TextBox 
            { 
                Anchor = AnchorStyles.Left | AnchorStyles.Right, 
                UseSystemPasswordChar = true,
                Enabled = false 
            };
            
            // Description
            var descriptionLabel = new Label { Text = "Description:", Anchor = AnchorStyles.Left, AutoSize = true };
            descriptionTextBox = new TextBox 
            { 
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                Height = 60 
            };

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Right
            };

            testConnectionButton = new Button
            {
                Text = "Test Connection",
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            testConnectionButton.Click += TestConnectionButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            okButton = new Button
            {
                Text = "OK",
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { cancelButton, okButton, testConnectionButton });

            // Set up table layout
            mainPanel.Controls.Add(serverNameLabel, 0, 0);
            mainPanel.Controls.Add(serverNameTextBox, 1, 0);
            mainPanel.Controls.Add(instanceLabel, 0, 1);
            mainPanel.Controls.Add(instanceTextBox, 1, 1);
            mainPanel.Controls.Add(authLabel, 0, 2);
            mainPanel.Controls.Add(authenticationComboBox, 1, 2);
            mainPanel.Controls.Add(usernameLabel, 0, 3);
            mainPanel.Controls.Add(usernameTextBox, 1, 3);
            mainPanel.Controls.Add(passwordLabel, 0, 4);
            mainPanel.Controls.Add(passwordTextBox, 1, 4);
            mainPanel.Controls.Add(descriptionLabel, 0, 5);
            mainPanel.Controls.Add(descriptionTextBox, 1, 5);
            mainPanel.SetColumnSpan(buttonPanel, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 7);

            // Configure column styles
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            this.Controls.Add(mainPanel);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void AuthenticationComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool enableCredentials = authenticationComboBox.SelectedIndex == 1; // SQL Server Authentication
            usernameTextBox.Enabled = enableCredentials;
            passwordTextBox.Enabled = enableCredentials;
            
            if (!enableCredentials)
            {
                usernameTextBox.Clear();
                passwordTextBox.Clear();
            }
        }

        private async void TestConnectionButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(serverNameTextBox.Text))
            {
                MessageBox.Show("Please enter a server name.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (authenticationComboBox.SelectedIndex == 1 && 
                (string.IsNullOrWhiteSpace(usernameTextBox.Text) || string.IsNullOrWhiteSpace(passwordTextBox.Text)))
            {
                MessageBox.Show("Please enter username and password for SQL Server authentication.", 
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var originalText = testConnectionButton.Text;
            testConnectionButton.Text = "Testing...";
            testConnectionButton.Enabled = false;

            try
            {
                var connectionString = BuildConnectionString();
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    MessageBox.Show("Connection successful!", "Test Connection", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Test Connection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                testConnectionButton.Text = originalText;
                testConnectionButton.Enabled = true;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(serverNameTextBox.Text))
            {
                MessageBox.Show("Please enter a server name.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (authenticationComboBox.SelectedIndex == 1 && 
                (string.IsNullOrWhiteSpace(usernameTextBox.Text) || string.IsNullOrWhiteSpace(passwordTextBox.Text)))
            {
                MessageBox.Show("Please enter username and password for SQL Server authentication.", 
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Set properties
            ServerName = serverNameTextBox.Text.Trim();
            InstanceName = instanceTextBox.Text.Trim();
            AuthenticationType = authenticationComboBox.SelectedIndex == 0 
                ? AuthenticationType.Windows 
                : AuthenticationType.SqlServer;
            Username = usernameTextBox.Text.Trim();
            Password = passwordTextBox.Text;
            Description = descriptionTextBox.Text.Trim();
        }

        private string BuildConnectionString()
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder();
            
            var server = serverNameTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(instanceTextBox.Text))
            {
                server += "\\" + instanceTextBox.Text.Trim();
            }
            
            builder.DataSource = server;
            
            if (authenticationComboBox.SelectedIndex == 0) // Windows Authentication
            {
                builder.IntegratedSecurity = true;
            }
            else // SQL Server Authentication
            {
                builder.UserID = usernameTextBox.Text.Trim();
                builder.Password = passwordTextBox.Text;
            }
            
            builder.ConnectTimeout = 15;
            builder.TrustServerCertificate = true;
            
            return builder.ConnectionString;
        }

        public SqlServerManager.Core.ServerManagement.ServerConnection GetServerConnection()
        {
            return new SqlServerManager.Core.ServerManagement.ServerConnection
            {
                Name = ServerName,
                ServerName = string.IsNullOrEmpty(InstanceName) ? ServerName : $"{ServerName}\\{InstanceName}",
                AuthenticationType = AuthenticationType == AuthenticationType.Windows 
                    ? SqlServerManager.Core.ServerManagement.AuthenticationType.Windows 
                    : SqlServerManager.Core.ServerManagement.AuthenticationType.SqlServer,
                Username = Username,
                Password = Password,
                IsEnabled = true
            };
        }
    }

    public enum AuthenticationType
    {
        Windows,
        SqlServer
    }
}
