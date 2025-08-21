using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using SqlServerManager.UI;

namespace SqlServerManager.Core.ServerManagement
{
    public partial class MultiServerManager : UserControl
    {
        private TreeView serverTree;
        private TabControl resultsTabControl;
        private TextBox queryTextBox;
        private DataGridView serverStatusGrid;
        private SplitContainer mainSplitter;
        private ToolStrip toolbar;
        
        private Dictionary<string, ServerConnection> serverConnections;
        private List<ServerGroup> serverGroups;
        private Timer healthCheckTimer;
        private bool isMonitoringHealth = false;
        
        public event EventHandler<MultiServerQueryEventArgs> MultiQueryExecuted;
        
        public MultiServerManager()
        {
            InitializeComponent();
            serverConnections = new Dictionary<string, ServerConnection>();
            serverGroups = new List<ServerGroup>();
            LoadServerConfiguration();
        }
        
        private void InitializeComponent()
        {
            this.Size = new Size(1200, 800);
            this.Dock = DockStyle.Fill;
            
            CreateToolbar();
            CreateMainInterface();
        }
        
        private void CreateToolbar()
        {
            toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            
            var addServerButton = new ToolStripButton("Add Server", null, AddServer_Click);
            addServerButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            var addGroupButton = new ToolStripButton("Add Group", null, AddGroup_Click);
            addGroupButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            var executeQueryButton = new ToolStripButton("Execute on Selected", null, ExecuteQuery_Click);
            executeQueryButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            executeQueryButton.BackColor = Color.FromArgb(0, 120, 215);
            executeQueryButton.ForeColor = Color.White;
            
            var executeAllButton = new ToolStripButton("Execute on All", null, ExecuteAllQuery_Click);
            executeAllButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            executeAllButton.BackColor = Color.FromArgb(196, 43, 28);
            executeAllButton.ForeColor = Color.White;
            
            var healthCheckButton = new ToolStripButton("Start Health Monitoring", null, ToggleHealthMonitoring_Click);
            healthCheckButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            var refreshButton = new ToolStripButton("Refresh All", null, RefreshAll_Click);
            refreshButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            
            toolbar.Items.AddRange(new ToolStripItem[] {
                addServerButton,
                addGroupButton,
                new ToolStripSeparator(),
                executeQueryButton,
                executeAllButton,
                new ToolStripSeparator(),
                healthCheckButton,
                refreshButton
            });
            
            this.Controls.Add(toolbar);
        }
        
        private void CreateMainInterface()
        {
            mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 350
            };
            
            // Left panel - Server tree and status
            CreateServerPanel(mainSplitter.Panel1);
            
            // Right panel - Query execution and results
            CreateQueryPanel(mainSplitter.Panel2);
            
            this.Controls.Add(mainSplitter);
        }
        
        private void CreateServerPanel(Control parent)
        {
            var serverSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };
            
            // Server tree
            var treePanel = new Panel { Dock = DockStyle.Fill };
            var treeLabel = new Label
            {
                Text = "Registered Servers",
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            
            serverTree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                ShowLines = true,
                ShowRootLines = true,
                CheckBoxes = true,
                ImageList = CreateServerImageList()
            };
            serverTree.AfterCheck += ServerTree_AfterCheck;
            serverTree.NodeMouseDoubleClick += ServerTree_NodeDoubleClick;
            
            treePanel.Controls.Add(serverTree);
            treePanel.Controls.Add(treeLabel);
            serverSplitter.Panel1.Controls.Add(treePanel);
            
            // Server status grid
            var statusPanel = new Panel { Dock = DockStyle.Fill };
            var statusLabel = new Label
            {
                Text = "Server Health Status",
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            
            serverStatusGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(70, 70, 70),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            
            InitializeStatusGrid();
            
            statusPanel.Controls.Add(serverStatusGrid);
            statusPanel.Controls.Add(statusLabel);
            serverSplitter.Panel2.Controls.Add(statusPanel);
            
            parent.Controls.Add(serverSplitter);
        }
        
        private void CreateQueryPanel(Control parent)
        {
            var querySplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200
            };
            
            // Query input
            var queryInputPanel = new Panel { Dock = DockStyle.Fill };
            var queryLabel = new Label
            {
                Text = "Multi-Server Query Execution",
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            
            queryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 11),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Text = "-- Enter your SQL query here\n-- It will be executed on all selected servers\nSELECT @@SERVERNAME as ServerName, @@VERSION as Version"
            };
            
            queryInputPanel.Controls.Add(queryTextBox);
            queryInputPanel.Controls.Add(queryLabel);
            querySplitter.Panel1.Controls.Add(queryInputPanel);
            
            // Results area
            CreateResultsArea(querySplitter.Panel2);
            
            parent.Controls.Add(querySplitter);
        }
        
        private void CreateResultsArea(Control parent)
        {
            resultsTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            
            // Summary tab
            var summaryTab = new TabPage("Summary");
            var summaryGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(70, 70, 70)
            };
            summaryTab.Controls.Add(summaryGrid);
            resultsTabControl.TabPages.Add(summaryTab);
            
            parent.Controls.Add(resultsTabControl);
        }
        
        private ImageList CreateServerImageList()
        {
            var imageList = new ImageList();
            imageList.Images.Add("server_online", CreateServerIcon(Color.Green));
            imageList.Images.Add("server_offline", CreateServerIcon(Color.Red));
            imageList.Images.Add("server_warning", CreateServerIcon(Color.Orange));
            imageList.Images.Add("group", CreateGroupIcon());
            return imageList;
        }
        
        private Bitmap CreateServerIcon(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(new SolidBrush(color), 2, 4, 12, 8);
                g.DrawRectangle(Pens.Black, 2, 4, 12, 8);
                g.FillRectangle(Brushes.Black, 6, 6, 1, 1);
                g.FillRectangle(Brushes.Black, 9, 6, 1, 1);
            }
            return bitmap;
        }
        
        private Bitmap CreateGroupIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.Yellow, 2, 4, 12, 8);
                g.DrawRectangle(Pens.Black, 2, 4, 12, 8);
                g.DrawLine(Pens.Black, 8, 4, 8, 12);
            }
            return bitmap;
        }
        
        private void InitializeStatusGrid()
        {
            var statusTable = new DataTable();
            statusTable.Columns.Add("Server", typeof(string));
            statusTable.Columns.Add("Status", typeof(string));
            statusTable.Columns.Add("Response Time", typeof(string));
            statusTable.Columns.Add("Version", typeof(string));
            statusTable.Columns.Add("Last Check", typeof(DateTime));
            statusTable.Columns.Add("Error", typeof(string));
            
            serverStatusGrid.DataSource = statusTable;
            
            // Color code the status column
            serverStatusGrid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 1 && e.Value != null) // Status column
                {
                    switch (e.Value.ToString())
                    {
                        case "Online":
                            e.CellStyle.BackColor = Color.Green;
                            e.CellStyle.ForeColor = Color.White;
                            break;
                        case "Offline":
                            e.CellStyle.BackColor = Color.Red;
                            e.CellStyle.ForeColor = Color.White;
                            break;
                        case "Warning":
                            e.CellStyle.BackColor = Color.Orange;
                            e.CellStyle.ForeColor = Color.Black;
                            break;
                    }
                }
            };
        }
        
        private void LoadServerConfiguration()
        {
            // Load saved server configurations
            // In a real implementation, this would load from a config file or database
            
            // Add default local server group
            var localGroup = new ServerGroup
            {
                Name = "Local Servers",
                Description = "Local SQL Server instances"
            };
            serverGroups.Add(localGroup);
            
            // Add some sample servers
            var localServer = new ServerConnection
            {
                Name = "Local SQL Server",
                ServerName = ".\\SQLEXPRESS",
                AuthenticationType = AuthenticationType.Windows,
                GroupName = "Local Servers",
                IsEnabled = true
            };
            serverConnections.Add(localServer.Name, localServer);
            
            RefreshServerTree();
        }
        
        private void RefreshServerTree()
        {
            serverTree.Nodes.Clear();
            
            // Add groups
            foreach (var group in serverGroups)
            {
                var groupNode = new TreeNode(group.Name)
                {
                    ImageKey = "group",
                    SelectedImageKey = "group",
                    Tag = group
                };
                
                // Add servers in this group
                var serversInGroup = serverConnections.Values
                    .Where(s => s.GroupName == group.Name)
                    .ToList();
                
                foreach (var server in serversInGroup)
                {
                    var serverNode = new TreeNode(server.Name)
                    {
                        ImageKey = GetServerImageKey(server),
                        SelectedImageKey = GetServerImageKey(server),
                        Tag = server
                    };
                    groupNode.Nodes.Add(serverNode);
                }
                
                serverTree.Nodes.Add(groupNode);
                groupNode.Expand();
            }
            
            // Add ungrouped servers
            var ungroupedServers = serverConnections.Values
                .Where(s => string.IsNullOrEmpty(s.GroupName))
                .ToList();
            
            if (ungroupedServers.Any())
            {
                var ungroupedNode = new TreeNode("Ungrouped Servers")
                {
                    ImageKey = "group",
                    SelectedImageKey = "group"
                };
                
                foreach (var server in ungroupedServers)
                {
                    var serverNode = new TreeNode(server.Name)
                    {
                        ImageKey = GetServerImageKey(server),
                        SelectedImageKey = GetServerImageKey(server),
                        Tag = server
                    };
                    ungroupedNode.Nodes.Add(serverNode);
                }
                
                serverTree.Nodes.Add(ungroupedNode);
                ungroupedNode.Expand();
            }
        }
        
        private string GetServerImageKey(ServerConnection server)
        {
            switch (server.Status)
            {
                case ServerStatus.Online:
                    return "server_online";
                case ServerStatus.Offline:
                    return "server_offline";
                case ServerStatus.Warning:
                    return "server_warning";
                default:
                    return "server_offline";
            }
        }
        
        private void AddServer_Click(object sender, EventArgs e)
        {
            using (var dialog = new AddServerDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var newServer = dialog.GetServerConnection();
                    serverConnections[newServer.Name] = newServer;
                    RefreshServerTree();
                    SaveServerConfiguration();
                }
            }
        }
        
        private void AddGroup_Click(object sender, EventArgs e)
        {
            using (var dialog = new InputDialog("Group Name", "Enter the name for the new group:"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var groupName = dialog.InputValue;
                    if (!serverGroups.Any(g => g.Name == groupName))
                    {
                        serverGroups.Add(new ServerGroup
                        {
                            Name = groupName,
                            Description = $"Server group: {groupName}"
                        });
                        RefreshServerTree();
                        SaveServerConfiguration();
                    }
                }
            }
        }
        
        private async void ExecuteQuery_Click(object sender, EventArgs e)
        {
            var selectedServers = GetSelectedServers();
            if (selectedServers.Count == 0)
            {
                MessageBox.Show("Please select at least one server.", "No Servers Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(queryTextBox.Text))
            {
                MessageBox.Show("Please enter a SQL query.", "No Query", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            await ExecuteMultiServerQuery(selectedServers, queryTextBox.Text);
        }
        
        private async void ExecuteAllQuery_Click(object sender, EventArgs e)
        {
            var allServers = serverConnections.Values
                .Where(s => s.IsEnabled && s.Status == ServerStatus.Online)
                .ToList();
            
            if (allServers.Count == 0)
            {
                MessageBox.Show("No online servers available.", "No Servers Available", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var result = MessageBox.Show(
                $"This will execute the query on {allServers.Count} servers. Continue?",
                "Execute on All Servers",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                await ExecuteMultiServerQuery(allServers, queryTextBox.Text);
            }
        }
        
        private void ToggleHealthMonitoring_Click(object sender, EventArgs e)
        {
            var button = sender as ToolStripButton;
            
            if (isMonitoringHealth)
            {
                StopHealthMonitoring();
                button.Text = "Start Health Monitoring";
            }
            else
            {
                StartHealthMonitoring();
                button.Text = "Stop Health Monitoring";
            }
        }
        
        private void StartHealthMonitoring()
        {
            if (healthCheckTimer == null)
            {
                healthCheckTimer = new Timer { Interval = 30000 }; // 30 seconds
                healthCheckTimer.Tick += HealthCheckTimer_Tick;
            }
            
            healthCheckTimer.Start();
            isMonitoringHealth = true;
            
            // Perform initial health check
            _ = Task.Run(PerformHealthCheck);
        }
        
        private void StopHealthMonitoring()
        {
            healthCheckTimer?.Stop();
            isMonitoringHealth = false;
        }
        
        private async void HealthCheckTimer_Tick(object sender, EventArgs e)
        {
            await PerformHealthCheck();
        }
        
        private async Task PerformHealthCheck()
        {
            var tasks = serverConnections.Values.Select(CheckServerHealth);
            await Task.WhenAll(tasks);
            
            // Update UI on main thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateHealthDisplay));
            }
            else
            {
                UpdateHealthDisplay();
            }
        }
        
        private async Task CheckServerHealth(ServerConnection server)
        {
            var startTime = DateTime.Now;
            
            try
            {
                using (var connection = new SqlConnection(server.GetConnectionString(5))) // 5 second timeout
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("SELECT @@VERSION", connection))
                    {
                        server.Version = (await command.ExecuteScalarAsync())?.ToString() ?? "Unknown";
                    }
                    
                    server.Status = ServerStatus.Online;
                    server.ResponseTime = (DateTime.Now - startTime).TotalMilliseconds;
                    server.LastError = null;
                }
            }
            catch (Exception ex)
            {
                server.Status = ServerStatus.Offline;
                server.ResponseTime = (DateTime.Now - startTime).TotalMilliseconds;
                server.LastError = ex.Message;
            }
            
            server.LastHealthCheck = DateTime.Now;
        }
        
        private void UpdateHealthDisplay()
        {
            var statusTable = (DataTable)serverStatusGrid.DataSource;
            statusTable.Clear();
            
            foreach (var server in serverConnections.Values)
            {
                statusTable.Rows.Add(
                    server.Name,
                    server.Status.ToString(),
                    $"{server.ResponseTime:F0}ms",
                    server.Version ?? "Unknown",
                    server.LastHealthCheck,
                    server.LastError ?? ""
                );
            }
            
            RefreshServerTree();
        }
        
        private async void RefreshAll_Click(object sender, EventArgs e)
        {
            await PerformHealthCheck();
        }
        
        private List<ServerConnection> GetSelectedServers()
        {
            var selectedServers = new List<ServerConnection>();
            
            foreach (TreeNode groupNode in serverTree.Nodes)
            {
                foreach (TreeNode serverNode in groupNode.Nodes)
                {
                    if (serverNode.Checked && serverNode.Tag is ServerConnection server)
                    {
                        selectedServers.Add(server);
                    }
                }
            }
            
            return selectedServers;
        }
        
        private async Task ExecuteMultiServerQuery(List<ServerConnection> servers, string query)
        {
            var results = new ConcurrentBag<MultiServerQueryResult>();
            var tasks = servers.Select(server => ExecuteQueryOnServer(server, query, results));
            
            // Clear existing results
            resultsTabControl.TabPages.Clear();
            
            // Add summary tab
            var summaryTab = new TabPage("Summary");
            var summaryGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(70, 70, 70)
            };
            summaryTab.Controls.Add(summaryGrid);
            resultsTabControl.TabPages.Add(summaryTab);
            
            // Execute queries
            await Task.WhenAll(tasks);
            
            // Update summary
            UpdateQuerySummary(results.ToList(), summaryGrid);
            
            // Add individual result tabs
            foreach (var result in results.OrderBy(r => r.ServerName))
            {
                AddResultTab(result);
            }
            
            MultiQueryExecuted?.Invoke(this, new MultiServerQueryEventArgs(servers, query, results.ToList()));
        }
        
        private async Task ExecuteQueryOnServer(ServerConnection server, string query, ConcurrentBag<MultiServerQueryResult> results)
        {
            var result = new MultiServerQueryResult
            {
                ServerName = server.Name,
                Query = query,
                StartTime = DateTime.Now
            };
            
            try
            {
                using (var connection = new SqlConnection(server.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 30;
                        
                        if (query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                result.Data = new DataTable();
                                await Task.Run(() => adapter.Fill(result.Data));
                                result.RowsAffected = result.Data.Rows.Count;
                            }
                        }
                        else
                        {
                            result.RowsAffected = await command.ExecuteNonQueryAsync();
                        }
                        
                        result.Success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                result.Duration = DateTime.Now - result.StartTime;
                results.Add(result);
            }
        }
        
        private void UpdateQuerySummary(List<MultiServerQueryResult> results, DataGridView summaryGrid)
        {
            var summaryTable = new DataTable();
            summaryTable.Columns.Add("Server", typeof(string));
            summaryTable.Columns.Add("Status", typeof(string));
            summaryTable.Columns.Add("Rows", typeof(int));
            summaryTable.Columns.Add("Duration", typeof(string));
            summaryTable.Columns.Add("Error", typeof(string));
            
            foreach (var result in results)
            {
                summaryTable.Rows.Add(
                    result.ServerName,
                    result.Success ? "Success" : "Failed",
                    result.RowsAffected,
                    $"{result.Duration.TotalMilliseconds:F0}ms",
                    result.ErrorMessage ?? ""
                );
            }
            
            summaryGrid.DataSource = summaryTable;
            
            // Color code the status column
            summaryGrid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 1 && e.Value != null) // Status column
                {
                    switch (e.Value.ToString())
                    {
                        case "Success":
                            e.CellStyle.BackColor = Color.Green;
                            e.CellStyle.ForeColor = Color.White;
                            break;
                        case "Failed":
                            e.CellStyle.BackColor = Color.Red;
                            e.CellStyle.ForeColor = Color.White;
                            break;
                    }
                }
            };
        }
        
        private void AddResultTab(MultiServerQueryResult result)
        {
            var tabPage = new TabPage(result.ServerName);
            
            if (result.Success && result.Data != null)
            {
                var resultGrid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    DataSource = result.Data,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    BackgroundColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    GridColor = Color.FromArgb(70, 70, 70)
                };
                tabPage.Controls.Add(resultGrid);
            }
            else
            {
                var errorLabel = new Label
                {
                    Text = result.ErrorMessage ?? "Query executed successfully",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = result.Success ? Color.Green : Color.Red,
                    ForeColor = Color.White
                };
                tabPage.Controls.Add(errorLabel);
            }
            
            resultsTabControl.TabPages.Add(tabPage);
        }
        
        private void ServerTree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Auto-check/uncheck child nodes when parent is checked
            if (e.Node.Tag is ServerGroup)
            {
                foreach (TreeNode childNode in e.Node.Nodes)
                {
                    childNode.Checked = e.Node.Checked;
                }
            }
        }
        
        private void ServerTree_NodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is ServerConnection server)
            {
                // Open connection dialog or properties
                MessageBox.Show($"Server: {server.Name}\nStatus: {server.Status}\nVersion: {server.Version}",
                    "Server Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void SaveServerConfiguration()
        {
            // In a real implementation, save to config file or database
        }
        
        public void AddServerConnection(ServerConnection server)
        {
            serverConnections[server.Name] = server;
            RefreshServerTree();
            SaveServerConfiguration();
        }
        
        public void RemoveServerConnection(string serverName)
        {
            if (serverConnections.ContainsKey(serverName))
            {
                serverConnections.Remove(serverName);
                RefreshServerTree();
                SaveServerConfiguration();
            }
        }
        
        public List<ServerConnection> GetAllServers()
        {
            return serverConnections.Values.ToList();
        }
    }
    
    public class ServerConnection
    {
        public string Name { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public AuthenticationType AuthenticationType { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string GroupName { get; set; }
        public bool IsEnabled { get; set; } = true;
        public ServerStatus Status { get; set; } = ServerStatus.Unknown;
        public double ResponseTime { get; set; }
        public string Version { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public string LastError { get; set; }
        
        public string GetConnectionString(int? timeout = null)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = ServerName,
                InitialCatalog = DatabaseName ?? "master",
                ConnectTimeout = timeout ?? 30
            };
            
            if (AuthenticationType == AuthenticationType.Windows)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = Username;
                builder.Password = Password;
            }
            
            return builder.ConnectionString;
        }
    }
    
    public class ServerGroup
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> ServerNames { get; set; } = new List<string>();
    }
    
    public class MultiServerQueryResult
    {
        public string ServerName { get; set; }
        public string Query { get; set; }
        public bool Success { get; set; }
        public DataTable Data { get; set; }
        public int RowsAffected { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
    }
    
    public enum ServerStatus
    {
        Unknown,
        Online,
        Offline,
        Warning
    }
    
    public enum AuthenticationType
    {
        Windows,
        SqlServer
    }
    
    public class MultiServerQueryEventArgs : EventArgs
    {
        public List<ServerConnection> Servers { get; }
        public string Query { get; }
        public List<MultiServerQueryResult> Results { get; }
        
        public MultiServerQueryEventArgs(List<ServerConnection> servers, string query, List<MultiServerQueryResult> results)
        {
            Servers = servers;
            Query = query;
            Results = results;
        }
    }
    
    public class ServerHealthEventArgs : EventArgs
    {
        public ServerConnection Server { get; }
        public ServerStatus PreviousStatus { get; }
        public ServerStatus CurrentStatus { get; }
        
        public ServerHealthEventArgs(ServerConnection server, ServerStatus previousStatus, ServerStatus currentStatus)
        {
            Server = server;
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
        }
    }
}
