using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SqlServerManager.Core.ServerManagement;

namespace SqlServerManager.UI
{
    public partial class ServerDiscoveryDialog : Form
    {
        private readonly ServerDiscoveryService _discoveryService;
        private CancellationTokenSource _cancellationTokenSource;

        // Controls
        private ListView _serverListView;
        private Button _discoverLocalButton;
        private Button _discoverNetworkButton;
        private Button _discoverAllButton;
        private Button _refreshButton;
        private Button _connectButton;
        private Button _cancelButton;
        private ModernProgressIndicator _progressIndicator;
        private Label _statusLabel;
        private CheckBox _showDetailsCheckBox;
        private GroupBox _discoveryOptionsGroup;
        private GroupBox _serverListGroup;
        private Panel _buttonPanel;
        private Panel _progressPanel;

        public SqlServerInstance SelectedInstance { get; private set; }
        public bool UserCancelled { get; private set; } = true;

        public ServerDiscoveryDialog()
        {
            _discoveryService = new ServerDiscoveryService();

            InitializeComponent();
            ApplyModernTheme();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Discover SQL Server Instances";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;

            // Discovery options group
            _discoveryOptionsGroup = new GroupBox
            {
                Text = "Discovery Options",
                Location = new Point(12, 12),
                Size = new Size(760, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _discoverLocalButton = new Button
            {
                Text = "Discover Local",
                Location = new Point(12, 25),
                Size = new Size(120, 35),
                UseVisualStyleBackColor = true
            };
            _discoverLocalButton.Click += DiscoverLocalButton_Click;

            _discoverNetworkButton = new Button
            {
                Text = "Discover Network",
                Location = new Point(145, 25),
                Size = new Size(120, 35),
                UseVisualStyleBackColor = true
            };
            _discoverNetworkButton.Click += DiscoverNetworkButton_Click;

            _discoverAllButton = new Button
            {
                Text = "Discover All",
                Location = new Point(278, 25),
                Size = new Size(120, 35),
                UseVisualStyleBackColor = true
            };
            _discoverAllButton.Click += DiscoverAllButton_Click;

            _refreshButton = new Button
            {
                Text = "Refresh",
                Location = new Point(411, 25),
                Size = new Size(80, 35),
                UseVisualStyleBackColor = true
            };
            _refreshButton.Click += RefreshButton_Click;

            _showDetailsCheckBox = new CheckBox
            {
                Text = "Show Details",
                Location = new Point(510, 32),
                Size = new Size(100, 20),
                Checked = false
            };
            _showDetailsCheckBox.CheckedChanged += ShowDetailsCheckBox_CheckedChanged;

            _discoveryOptionsGroup.Controls.AddRange(new Control[]
            {
                _discoverLocalButton, _discoverNetworkButton, _discoverAllButton,
                _refreshButton, _showDetailsCheckBox
            });

            // Server list group
            _serverListGroup = new GroupBox
            {
                Text = "Discovered Servers",
                Location = new Point(12, 100),
                Size = new Size(760, 340),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _serverListView = new ListView
            {
                Location = new Point(12, 25),
                Size = new Size(736, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false
            };

            // Add columns
            _serverListView.Columns.Add("Server Name", 200);
            _serverListView.Columns.Add("Instance", 120);
            _serverListView.Columns.Add("Version", 120);
            _serverListView.Columns.Add("Local", 60);
            _serverListView.Columns.Add("Method", 150);
            _serverListView.Columns.Add("IP Address", 120);
            _serverListView.Columns.Add("Port", 60);
            _serverListView.Columns.Add("Discovered", 120);

            _serverListView.SelectedIndexChanged += ServerListView_SelectedIndexChanged;
            _serverListView.DoubleClick += ServerListView_DoubleClick;

            _serverListGroup.Controls.Add(_serverListView);

            // Progress panel
            _progressPanel = new Panel
            {
                Location = new Point(12, 450),
                Size = new Size(760, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false
            };

            _progressIndicator = new ModernProgressIndicator
            {
                Location = new Point(0, 10),
                Size = new Size(200, 20),
                Style = ProgressStyle.Bar,
                IsIndeterminate = true
            };

            _statusLabel = new Label
            {
                Location = new Point(210, 15),
                Size = new Size(540, 20),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Text = "Discovering servers..."
            };

            _progressPanel.Controls.AddRange(new Control[] { _progressIndicator, _statusLabel });

            // Button panel
            _buttonPanel = new Panel
            {
                Location = new Point(12, 500),
                Size = new Size(760, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _connectButton = new Button
            {
                Text = "Connect",
                Size = new Size(80, 30),
                Enabled = false,
                DialogResult = DialogResult.OK
            };
            _connectButton.Click += ConnectButton_Click;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel,
                UseVisualStyleBackColor = true
            };
            _cancelButton.Click += CancelButton_Click;

            // Center the buttons in the panel
            int totalButtonWidth = _connectButton.Width + _cancelButton.Width + 10; // 10px spacing between buttons
            int startX = (_buttonPanel.Width - totalButtonWidth) / 2;
            
            _connectButton.Location = new Point(startX, 5);
            _cancelButton.Location = new Point(startX + _connectButton.Width + 10, 5);
            
            _buttonPanel.Controls.AddRange(new Control[] { _connectButton, _cancelButton });
            
            // Ensure buttons are visible and properly positioned
            _connectButton.BringToFront();
            _cancelButton.BringToFront();

            // Add all controls to form
            this.Controls.AddRange(new Control[]
            {
                _discoveryOptionsGroup, _serverListGroup, _progressPanel, _buttonPanel
            });
            
            // Set button panel as top-most to ensure buttons are visible
            _buttonPanel.BringToFront();
            
            // Set form properties for proper dialog behavior
            this.AcceptButton = _connectButton;
            this.CancelButton = _cancelButton;

            this.ResumeLayout(false);
        }

        private void ApplyModernTheme()
        {
            ModernThemeManager.ApplyTheme(this);
        }

        private async void DiscoverLocalButton_Click(object sender, EventArgs e)
        {
            await RunDiscovery("local");
        }

        private async void DiscoverNetworkButton_Click(object sender, EventArgs e)
        {
            await RunDiscovery("network");
        }

        private async void DiscoverAllButton_Click(object sender, EventArgs e)
        {
            await RunDiscovery("all");
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            _serverListView.Items.Clear();
            await RunDiscovery("all");
        }

        private async Task RunDiscovery(string discoveryType)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try
            {
                SetDiscoveryInProgress(true, $"Discovering {discoveryType} servers...");

                List<SqlServerInstance> instances;
                
                switch (discoveryType.ToLower())
                {
                    case "local":
                        instances = await _discoveryService.DiscoverLocalInstancesAsync(cancellationToken);
                        break;
                    case "network":
                        instances = await _discoveryService.DiscoverNetworkInstancesAsync(cancellationToken);
                        break;
                    case "all":
                    default:
                        instances = await _discoveryService.DiscoverAllInstancesAsync(cancellationToken);
                        break;
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    PopulateServerList(instances);
                    SetDiscoveryInProgress(false, $"Found {instances.Count} server(s)");
                }
            }
            catch (OperationCanceledException)
            {
                SetDiscoveryInProgress(false, "Discovery cancelled");
            }
            catch (Exception ex)
            {
                SetDiscoveryInProgress(false, $"Discovery failed: {ex.Message}");
                // Log server discovery error
                MessageBox.Show($"Server discovery failed: {ex.Message}", "Discovery Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetDiscoveryInProgress(bool inProgress, string statusText = "")
        {
            _progressPanel.Visible = inProgress;
            
            if (inProgress)
            {
                _progressIndicator.IsIndeterminate = true;
            }
            else
            {
                _progressIndicator.IsIndeterminate = false;
            }

            _statusLabel.Text = statusText;

            // Disable discovery buttons during discovery
            _discoverLocalButton.Enabled = !inProgress;
            _discoverNetworkButton.Enabled = !inProgress;
            _discoverAllButton.Enabled = !inProgress;
            _refreshButton.Enabled = !inProgress;
        }

        private void PopulateServerList(List<SqlServerInstance> instances)
        {
            _serverListView.Items.Clear();

            foreach (var instance in instances.OrderBy(i => i.ServerName))
            {
                var item = new ListViewItem(instance.ServerName)
                {
                    Tag = instance
                };

                item.SubItems.Add(instance.InstanceName ?? "");
                item.SubItems.Add(instance.Version ?? "Unknown");
                item.SubItems.Add(instance.IsLocal ? "Yes" : "No");
                item.SubItems.Add(instance.DiscoveryMethod ?? "");
                item.SubItems.Add(instance.IPAddress ?? "");
                item.SubItems.Add(instance.TcpPort?.ToString() ?? "");
                item.SubItems.Add(instance.DiscoveredAt.ToString("HH:mm:ss"));

                // Set icon/color based on instance type
                if (instance.IsLocal)
                {
                    item.ForeColor = Color.Blue;
                }
                else
                {
                    item.ForeColor = Color.Black;
                }

                _serverListView.Items.Add(item);
            }

            // Auto-resize columns if showing details
            if (_showDetailsCheckBox.Checked)
            {
                foreach (ColumnHeader column in _serverListView.Columns)
                {
                    column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
        }

        private void ShowDetailsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_showDetailsCheckBox.Checked)
            {
                // Show all columns
                foreach (ColumnHeader column in _serverListView.Columns)
                {
                    column.Width = -2; // Auto-resize
                }
            }
            else
            {
                // Show only essential columns
                _serverListView.Columns[0].Width = 200; // Server Name
                _serverListView.Columns[1].Width = 120; // Instance
                _serverListView.Columns[2].Width = 120; // Version
                _serverListView.Columns[3].Width = 60;  // Local
                
                // Hide other columns
                for (int i = 4; i < _serverListView.Columns.Count; i++)
                {
                    _serverListView.Columns[i].Width = 0;
                }
            }
        }

        private void ServerListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            _connectButton.Enabled = _serverListView.SelectedItems.Count > 0;
        }

        private void ServerListView_DoubleClick(object sender, EventArgs e)
        {
            if (_serverListView.SelectedItems.Count > 0)
            {
                ConnectToSelectedServer();
            }
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            ConnectToSelectedServer();
        }

        private void ConnectToSelectedServer()
        {
            if (_serverListView.SelectedItems.Count > 0)
            {
                SelectedInstance = (SqlServerInstance)_serverListView.SelectedItems[0].Tag;
                UserCancelled = false;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            UserCancelled = true;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _progressIndicator?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
