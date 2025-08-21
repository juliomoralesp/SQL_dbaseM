using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace SqlServerManager.UI
{
    public partial class ConnectionStringBuilderDialog : Form
    {
        private TabControl _tabControl;
        private Button _testButton;
        private Button _okButton;
        private Button _cancelButton;
        private TextBox _connectionStringTextBox;
        private Label _statusLabel;
        
        // Basic tab controls
        private ComboBox _serverComboBox;
        private ComboBox _authenticationComboBox;
        private TextBox _usernameTextBox;
        private TextBox _passwordTextBox;
        private ComboBox _databaseComboBox;
        private CheckBox _trustServerCertificateCheckBox;
        private CheckBox _encryptCheckBox;
        private CheckBox _integratedSecurityCheckBox;
        
        // Advanced tab controls
        private NumericUpDown _connectionTimeoutNumeric;
        private NumericUpDown _commandTimeoutNumeric;
        private CheckBox _marsCheckBox;
        private CheckBox _poolingCheckBox;
        private NumericUpDown _minPoolSizeNumeric;
        private NumericUpDown _maxPoolSizeNumeric;
        private TextBox _applicationNameTextBox;
        private TextBox _workstationIdTextBox;
        
        private SqlConnectionStringBuilder _connectionStringBuilder;
        private CancellationTokenSource _cancellationTokenSource;

        public string ConnectionString { get; private set; } = string.Empty;
        public bool TestConnection { get; set; } = true;

        public ConnectionStringBuilderDialog()
        {
            InitializeComponent();
            InitializeConnectionStringBuilder();
            LoadDefaultValues();
        }

        public ConnectionStringBuilderDialog(string existingConnectionString) : this()
        {
            if (!string.IsNullOrEmpty(existingConnectionString))
            {
                try
                {
                    _connectionStringBuilder.ConnectionString = existingConnectionString;
                    LoadValuesFromConnectionString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error parsing connection string: {ex.Message}", 
                        "Connection String Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form properties
            Text = "Connection String Builder";
            Size = new Size(600, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            // Create main controls
            CreateTabControl();
            CreateBasicTab();
            CreateAdvancedTab();
            CreateConnectionStringTab();
            CreateButtons();
            CreateStatusLabel();

            ResumeLayout(false);
        }

        private void CreateTabControl()
        {
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8)
            };
            Controls.Add(_tabControl);
        }

        private void CreateBasicTab()
        {
            var basicTab = new TabPage("Connection");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            var y = 12;
            var labelWidth = 150;
            var controlWidth = 300;

            // Server
            var serverLabel = new Label
            {
                Text = "Server name:",
                Location = new Point(12, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(serverLabel);

            _serverComboBox = new ComboBox
            {
                Location = new Point(labelWidth + 24, y),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _serverComboBox.Items.AddRange(new[] { "localhost", ".\\SQLEXPRESS", "(localdb)\\MSSQLLocalDB" });
            _serverComboBox.TextChanged += (s, e) => UpdateConnectionString();
            panel.Controls.Add(_serverComboBox);

            y += 35;

            // Authentication
            var authLabel = new Label
            {
                Text = "Authentication:",
                Location = new Point(12, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(authLabel);

            _authenticationComboBox = new ComboBox
            {
                Location = new Point(labelWidth + 24, y),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _authenticationComboBox.Items.AddRange(new[] { "Windows Authentication", "SQL Server Authentication" });
            _authenticationComboBox.SelectedIndexChanged += AuthenticationChanged;
            panel.Controls.Add(_authenticationComboBox);

            y += 35;

            // Username
            var usernameLabel = new Label
            {
                Text = "User name:",
                Location = new Point(12, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(usernameLabel);

            _usernameTextBox = new TextBox
            {
                Location = new Point(labelWidth + 24, y),
                Size = new Size(controlWidth, 23)
            };
            _usernameTextBox.TextChanged += (s, e) => UpdateConnectionString();
            panel.Controls.Add(_usernameTextBox);

            y += 30;

            // Password
            var passwordLabel = new Label
            {
                Text = "Password:",
                Location = new Point(12, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(passwordLabel);

            _passwordTextBox = new TextBox
            {
                Location = new Point(labelWidth + 24, y),
                Size = new Size(controlWidth, 23),
                UseSystemPasswordChar = true
            };
            _passwordTextBox.TextChanged += (s, e) => UpdateConnectionString();
            panel.Controls.Add(_passwordTextBox);

            y += 35;

            // Database
            var databaseLabel = new Label
            {
                Text = "Select or enter database:",
                Location = new Point(12, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(databaseLabel);

            _databaseComboBox = new ComboBox
            {
                Location = new Point(labelWidth + 24, y),
                Size = new Size(controlWidth - 80, 23),
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _databaseComboBox.TextChanged += (s, e) => UpdateConnectionString();
            panel.Controls.Add(_databaseComboBox);

            var refreshDbButton = new Button
            {
                Text = "Refresh",
                Location = new Point(labelWidth + 24 + controlWidth - 75, y),
                Size = new Size(70, 23)
            };
            refreshDbButton.Click += RefreshDatabases;
            panel.Controls.Add(refreshDbButton);

            y += 35;

            // Security options
            var securityGroupBox = new GroupBox
            {
                Text = "Security Options",
                Location = new Point(12, y),
                Size = new Size(controlWidth + labelWidth + 12, 120)
            };

            _trustServerCertificateCheckBox = new CheckBox
            {
                Text = "Trust server certificate",
                Location = new Point(12, 25),
                Size = new Size(200, 23),
                Checked = false
            };
            _trustServerCertificateCheckBox.CheckedChanged += (s, e) => UpdateConnectionString();
            securityGroupBox.Controls.Add(_trustServerCertificateCheckBox);

            _encryptCheckBox = new CheckBox
            {
                Text = "Encrypt connection",
                Location = new Point(12, 50),
                Size = new Size(200, 23),
                Checked = true
            };
            _encryptCheckBox.CheckedChanged += (s, e) => UpdateConnectionString();
            securityGroupBox.Controls.Add(_encryptCheckBox);

            _integratedSecurityCheckBox = new CheckBox
            {
                Text = "Use integrated security",
                Location = new Point(12, 75),
                Size = new Size(200, 23),
                Checked = true,
                Enabled = false
            };
            securityGroupBox.Controls.Add(_integratedSecurityCheckBox);

            panel.Controls.Add(securityGroupBox);

            basicTab.Controls.Add(panel);
            _tabControl.TabPages.Add(basicTab);
        }

        private void CreateAdvancedTab()
        {
            var advancedTab = new TabPage("Advanced");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            var y = 12;
            var labelWidth = 150;
            var controlWidth = 100;

            // Connection timeout
            var connTimeoutLabel = new Label
            {
                Text = "Connection timeout:",
                Location = new Point(12, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(connTimeoutLabel);

            _connectionTimeoutNumeric = new NumericUpDown
            {
                Location = new Point(labelWidth + 24, y),
                Size = new Size(controlWidth, 23),
                Minimum = 0,
                Maximum = 300,
                Value = 15
            };
            _connectionTimeoutNumeric.ValueChanged += (s, e) => UpdateConnectionString();
            panel.Controls.Add(_connectionTimeoutNumeric);

            var connTimeoutSecondsLabel = new Label
            {
                Text = "seconds",
                Location = new Point(labelWidth + 24 + controlWidth + 8, y),
                Size = new Size(60, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(connTimeoutSecondsLabel);

            y += 35;

            // Command timeout
            var cmdTimeoutLabel = new Label
            {
                Text = "Command timeout:",
                Location = new Point(12, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(cmdTimeoutLabel);

            _commandTimeoutNumeric = new NumericUpDown
            {
                Location = new Point(labelWidth + 24, y),
                Size = new Size(controlWidth, 23),
                Minimum = 0,
                Maximum = 3600,
                Value = 30
            };
            _commandTimeoutNumeric.ValueChanged += (s, e) => UpdateConnectionString();
            panel.Controls.Add(_commandTimeoutNumeric);

            var cmdTimeoutSecondsLabel = new Label
            {
                Text = "seconds",
                Location = new Point(labelWidth + 24 + controlWidth + 8, y),
                Size = new Size(60, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(cmdTimeoutSecondsLabel);

            y += 35;

            // MARS
            _marsCheckBox = new CheckBox
            {
                Text = "Multiple Active Result Sets (MARS)",
                Location = new Point(12, y),
                Size = new Size(300, 23),
                Checked = false
            };
            _marsCheckBox.CheckedChanged += (s, e) => UpdateConnectionString();
            panel.Controls.Add(_marsCheckBox);

            y += 30;

            // Connection Pooling
            var poolingGroupBox = new GroupBox
            {
                Text = "Connection Pooling",
                Location = new Point(12, y),
                Size = new Size(450, 120)
            };

            _poolingCheckBox = new CheckBox
            {
                Text = "Enable connection pooling",
                Location = new Point(12, 25),
                Size = new Size(200, 23),
                Checked = true
            };
            _poolingCheckBox.CheckedChanged += PoolingChanged;
            poolingGroupBox.Controls.Add(_poolingCheckBox);

            var minPoolLabel = new Label
            {
                Text = "Min pool size:",
                Location = new Point(12, 55),
                Size = new Size(100, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            poolingGroupBox.Controls.Add(minPoolLabel);

            _minPoolSizeNumeric = new NumericUpDown
            {
                Location = new Point(120, 55),
                Size = new Size(80, 23),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            _minPoolSizeNumeric.ValueChanged += (s, e) => UpdateConnectionString();
            poolingGroupBox.Controls.Add(_minPoolSizeNumeric);

            var maxPoolLabel = new Label
            {
                Text = "Max pool size:",
                Location = new Point(220, 55),
                Size = new Size(100, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            poolingGroupBox.Controls.Add(maxPoolLabel);

            _maxPoolSizeNumeric = new NumericUpDown
            {
                Location = new Point(330, 55),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 1000,
                Value = 100
            };
            _maxPoolSizeNumeric.ValueChanged += (s, e) => UpdateConnectionString();
            poolingGroupBox.Controls.Add(_maxPoolSizeNumeric);

            panel.Controls.Add(poolingGroupBox);

            y += 140;

            // Application settings
            var appGroupBox = new GroupBox
            {
                Text = "Application Settings",
                Location = new Point(12, y),
                Size = new Size(450, 100)
            };

            var appNameLabel = new Label
            {
                Text = "Application Name:",
                Location = new Point(12, 25),
                Size = new Size(120, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            appGroupBox.Controls.Add(appNameLabel);

            _applicationNameTextBox = new TextBox
            {
                Location = new Point(140, 25),
                Size = new Size(200, 23),
                Text = "SQL Server Manager"
            };
            _applicationNameTextBox.TextChanged += (s, e) => UpdateConnectionString();
            appGroupBox.Controls.Add(_applicationNameTextBox);

            var workstationLabel = new Label
            {
                Text = "Workstation ID:",
                Location = new Point(12, 55),
                Size = new Size(120, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            appGroupBox.Controls.Add(workstationLabel);

            _workstationIdTextBox = new TextBox
            {
                Location = new Point(140, 55),
                Size = new Size(200, 23),
                Text = Environment.MachineName
            };
            _workstationIdTextBox.TextChanged += (s, e) => UpdateConnectionString();
            appGroupBox.Controls.Add(_workstationIdTextBox);

            panel.Controls.Add(appGroupBox);

            advancedTab.Controls.Add(panel);
            _tabControl.TabPages.Add(advancedTab);
        }

        private void CreateConnectionStringTab()
        {
            var connectionStringTab = new TabPage("Connection String");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            var label = new Label
            {
                Text = "Connection String:",
                Location = new Point(12, 12),
                Size = new Size(200, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(label);

            _connectionStringTextBox = new TextBox
            {
                Location = new Point(12, 40),
                Size = new Size(550, 300),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            panel.Controls.Add(_connectionStringTextBox);

            connectionStringTab.Controls.Add(panel);
            _tabControl.TabPages.Add(connectionStringTab);
        }

        private void CreateButtons()
        {
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(12)
            };

            _testButton = new Button
            {
                Text = "Test Connection",
                Size = new Size(120, 30),
                Location = new Point(12, 10)
            };
            _testButton.Click += TestConnection_Click;
            buttonPanel.Controls.Add(_testButton);

            _okButton = new Button
            {
                Text = "OK",
                Size = new Size(75, 30),
                Location = new Point(400, 10),
                DialogResult = DialogResult.OK
            };
            _okButton.Click += OkButton_Click;
            buttonPanel.Controls.Add(_okButton);

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(75, 30),
                Location = new Point(485, 10),
                DialogResult = DialogResult.Cancel
            };
            buttonPanel.Controls.Add(_cancelButton);

            Controls.Add(buttonPanel);
        }

        private void CreateStatusLabel()
        {
            _statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 5, 12, 5),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_statusLabel);
        }

        private void InitializeConnectionStringBuilder()
        {
            _connectionStringBuilder = new SqlConnectionStringBuilder();
        }

        private void LoadDefaultValues()
        {
            _serverComboBox.Text = "localhost";
            _authenticationComboBox.SelectedIndex = 0; // Windows Authentication
            _databaseComboBox.Text = "";
            _trustServerCertificateCheckBox.Checked = false;
            _encryptCheckBox.Checked = true;
            UpdateConnectionString();
        }

        private void LoadValuesFromConnectionString()
        {
            try
            {
                _serverComboBox.Text = _connectionStringBuilder.DataSource ?? "";
                _databaseComboBox.Text = _connectionStringBuilder.InitialCatalog ?? "";
                _usernameTextBox.Text = _connectionStringBuilder.UserID ?? "";
                _passwordTextBox.Text = _connectionStringBuilder.Password ?? "";
                _trustServerCertificateCheckBox.Checked = _connectionStringBuilder.TrustServerCertificate;
                _encryptCheckBox.Checked = _connectionStringBuilder.Encrypt;
                _connectionTimeoutNumeric.Value = _connectionStringBuilder.ConnectTimeout;
                _commandTimeoutNumeric.Value = _connectionStringBuilder.CommandTimeout;
                _marsCheckBox.Checked = _connectionStringBuilder.MultipleActiveResultSets;
                _poolingCheckBox.Checked = _connectionStringBuilder.Pooling;
                _minPoolSizeNumeric.Value = _connectionStringBuilder.MinPoolSize;
                _maxPoolSizeNumeric.Value = _connectionStringBuilder.MaxPoolSize;
                _applicationNameTextBox.Text = _connectionStringBuilder.ApplicationName ?? "SQL Server Manager";
                _workstationIdTextBox.Text = _connectionStringBuilder.WorkstationID ?? Environment.MachineName;

                // Set authentication type
                _authenticationComboBox.SelectedIndex = _connectionStringBuilder.IntegratedSecurity ? 0 : 1;
                AuthenticationChanged(null, null);

                UpdateConnectionString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading connection string values: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AuthenticationChanged(object sender, EventArgs e)
        {
            var isWindowsAuth = _authenticationComboBox.SelectedIndex == 0;
            _usernameTextBox.Enabled = !isWindowsAuth;
            _passwordTextBox.Enabled = !isWindowsAuth;
            _integratedSecurityCheckBox.Checked = isWindowsAuth;
            UpdateConnectionString();
        }

        private void PoolingChanged(object sender, EventArgs e)
        {
            var poolingEnabled = _poolingCheckBox.Checked;
            _minPoolSizeNumeric.Enabled = poolingEnabled;
            _maxPoolSizeNumeric.Enabled = poolingEnabled;
            UpdateConnectionString();
        }

        private void UpdateConnectionString()
        {
            try
            {
                _connectionStringBuilder.DataSource = _serverComboBox.Text.Trim();
                _connectionStringBuilder.InitialCatalog = _databaseComboBox.Text.Trim();
                _connectionStringBuilder.IntegratedSecurity = _authenticationComboBox.SelectedIndex == 0;
                
                if (_authenticationComboBox.SelectedIndex == 1) // SQL Server Authentication
                {
                    _connectionStringBuilder.UserID = _usernameTextBox.Text.Trim();
                    _connectionStringBuilder.Password = _passwordTextBox.Text;
                }
                else
                {
                    _connectionStringBuilder.UserID = "";
                    _connectionStringBuilder.Password = "";
                }

                _connectionStringBuilder.TrustServerCertificate = _trustServerCertificateCheckBox.Checked;
                _connectionStringBuilder.Encrypt = _encryptCheckBox.Checked;
                _connectionStringBuilder.ConnectTimeout = (int)_connectionTimeoutNumeric.Value;
                _connectionStringBuilder.CommandTimeout = (int)_commandTimeoutNumeric.Value;
                _connectionStringBuilder.MultipleActiveResultSets = _marsCheckBox.Checked;
                _connectionStringBuilder.Pooling = _poolingCheckBox.Checked;
                _connectionStringBuilder.MinPoolSize = (int)_minPoolSizeNumeric.Value;
                _connectionStringBuilder.MaxPoolSize = (int)_maxPoolSizeNumeric.Value;
                _connectionStringBuilder.ApplicationName = _applicationNameTextBox.Text.Trim();
                _connectionStringBuilder.WorkstationID = _workstationIdTextBox.Text.Trim();

                _connectionStringTextBox.Text = _connectionStringBuilder.ConnectionString;
                ConnectionString = _connectionStringBuilder.ConnectionString;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        private async void TestConnection_Click(object sender, EventArgs e)
        {
            await TestConnectionAsync();
        }

        private async Task TestConnectionAsync()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            _testButton.Enabled = false;
            _statusLabel.Text = "Testing connection...";
            _statusLabel.ForeColor = Color.Blue;

            try
            {
                using var connection = new SqlConnection(_connectionStringBuilder.ConnectionString);
                await connection.OpenAsync(_cancellationTokenSource.Token);
                
                _statusLabel.Text = "Connection successful!";
                _statusLabel.ForeColor = Color.Green;
                
                // Try to load databases if connection successful
                await LoadDatabasesAsync(connection);
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = "Connection test cancelled";
                _statusLabel.ForeColor = Color.Orange;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Connection failed: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _testButton.Enabled = true;
            }
        }

        private async Task LoadDatabasesAsync(SqlConnection connection)
        {
            try
            {
                const string query = "SELECT name FROM sys.databases WHERE state = 0 ORDER BY name";
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync(_cancellationTokenSource.Token);

                var databases = new List<string>();
                while (await reader.ReadAsync(_cancellationTokenSource.Token))
                {
                    databases.Add(reader.GetString(0));
                }

                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateDatabaseComboBox(databases)));
                }
                else
                {
                    UpdateDatabaseComboBox(databases);
                }
            }
            catch (Exception ex)
            {
                // Don't show error for database loading - connection test was successful
                Console.WriteLine($"Could not load databases: {ex.Message}");
            }
        }

        private void UpdateDatabaseComboBox(List<string> databases)
        {
            var currentDatabase = _databaseComboBox.Text;
            _databaseComboBox.Items.Clear();
            _databaseComboBox.Items.AddRange(databases.ToArray());
            _databaseComboBox.Text = currentDatabase;
        }

        private async void RefreshDatabases(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_serverComboBox.Text))
            {
                MessageBox.Show("Please enter a server name first.", "Server Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var connection = new SqlConnection(_connectionStringBuilder.ConnectionString);
                await connection.OpenAsync();
                await LoadDatabasesAsync(connection);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not refresh databases: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            ConnectionString = _connectionStringBuilder.ConnectionString;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
