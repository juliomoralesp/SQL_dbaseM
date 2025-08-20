using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using LiveCharts;
using LiveCharts.WinForms;

namespace SqlServerManager.Core.Monitoring
{
    public partial class PerformanceMonitor : UserControl
    {
        private Control performanceChart;
        private DataGridView metricsGrid;
        private DataGridView alertsGrid;
        private Timer refreshTimer;
        private Label statusLabel;
        private Panel alertPanel;
        
        // Chart data series
        private ChartValues<double> cpuValues;
        private ChartValues<double> memoryValues;
        private ChartValues<double> diskIOValues;
        private ChartValues<double> activeConnectionsValues;
        
        private string connectionString;
        private bool isMonitoring = false;
        private List<PerformanceAlert> activeAlerts;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter memoryCounter;
        
        public event EventHandler<PerformanceAlertEventArgs> AlertTriggered;
        
        public PerformanceMonitor()
        {
            InitializeComponent();
            InitializePerformanceCounters();
            activeAlerts = new List<PerformanceAlert>();
        }
        
        private void InitializeComponent()
        {
            this.Size = new Size(1000, 700);
            this.Dock = DockStyle.Fill;
            
            // Main split container
            var mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };
            
            // Top section - Charts and controls
            CreateChartsSection(mainSplitter.Panel1);
            
            // Bottom section - Metrics and alerts
            CreateMetricsSection(mainSplitter.Panel2);
            
            this.Controls.Add(mainSplitter);
        }
        
        private void CreateChartsSection(Control parent)
        {
            // Control panel
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            
            var startButton = new Button
            {
                Text = "Start Monitoring",
                Location = new Point(10, 8),
                Size = new Size(120, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            startButton.Click += StartButton_Click;
            
            var stopButton = new Button
            {
                Text = "Stop Monitoring",
                Location = new Point(140, 8),
                Size = new Size(120, 25),
                BackColor = Color.FromArgb(196, 43, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            stopButton.Click += StopButton_Click;
            
            var refreshIntervalLabel = new Label
            {
                Text = "Refresh (sec):",
                Location = new Point(280, 12),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };
            
            var refreshIntervalCombo = new ComboBox
            {
                Location = new Point(365, 8),
                Size = new Size(60, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            refreshIntervalCombo.Items.AddRange(new object[] { 1, 2, 5, 10, 30 });
            refreshIntervalCombo.SelectedIndex = 2; // Default to 5 seconds
            refreshIntervalCombo.SelectedIndexChanged += RefreshInterval_Changed;
            
            statusLabel = new Label
            {
                Location = new Point(450, 12),
                Size = new Size(200, 20),
                Text = "Monitoring stopped",
                ForeColor = Color.Gray
            };
            
            controlPanel.Controls.AddRange(new Control[] {
                startButton, stopButton, refreshIntervalLabel, refreshIntervalCombo, statusLabel
            });
            
            // Performance chart
            InitializeChart();
            
            var chartPanel = new Panel { Dock = DockStyle.Fill };
            chartPanel.Controls.Add(performanceChart);
            parent.Controls.Add(chartPanel);
            parent.Controls.Add(controlPanel);
        }
        
        private void InitializeChart()
        {
            cpuValues = new ChartValues<double>();
            memoryValues = new ChartValues<double>();
            diskIOValues = new ChartValues<double>();
            activeConnectionsValues = new ChartValues<double>();
            
            // Create a simple placeholder panel for charts
            // In a production environment, this would be replaced with actual charting functionality
            var chartPlaceholder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            var chartLabel = new Label
            {
                Text = "Performance Charts\n\n(Charts would display here)\n\nCPU, Memory, Disk I/O, and Active Connections metrics will be collected and can be viewed in the metrics grid below.",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10)
            };
            
            chartPlaceholder.Controls.Add(chartLabel);
            performanceChart = chartPlaceholder;
        }
        
        private void CreateMetricsSection(Control parent)
        {
            var bottomSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 500
            };
            
            // Metrics grid
            var metricsPanel = new Panel { Dock = DockStyle.Fill };
            var metricsLabel = new Label
            {
                Text = "Current Metrics",
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            metricsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(70, 70, 70)
            };
            
            metricsPanel.Controls.Add(metricsGrid);
            metricsPanel.Controls.Add(metricsLabel);
            bottomSplitter.Panel1.Controls.Add(metricsPanel);
            
            // Alerts panel
            var alertsPanel = new Panel { Dock = DockStyle.Fill };
            var alertsLabel = new Label
            {
                Text = "Performance Alerts",
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            alertsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(70, 70, 70)
            };
            
            alertsPanel.Controls.Add(alertsGrid);
            alertsPanel.Controls.Add(alertsLabel);
            bottomSplitter.Panel2.Controls.Add(alertsPanel);
            
            parent.Controls.Add(bottomSplitter);
            
            InitializeGrids();
        }
        
        private void InitializeGrids()
        {
            // Initialize metrics grid
            var metricsTable = new DataTable();
            metricsTable.Columns.Add("Metric", typeof(string));
            metricsTable.Columns.Add("Current Value", typeof(string));
            metricsTable.Columns.Add("Average", typeof(string));
            metricsTable.Columns.Add("Peak", typeof(string));
            metricsTable.Columns.Add("Status", typeof(string));
            
            metricsGrid.DataSource = metricsTable;
            
            // Initialize alerts grid
            var alertsTable = new DataTable();
            alertsTable.Columns.Add("Time", typeof(DateTime));
            alertsTable.Columns.Add("Type", typeof(string));
            alertsTable.Columns.Add("Metric", typeof(string));
            alertsTable.Columns.Add("Value", typeof(string));
            alertsTable.Columns.Add("Threshold", typeof(string));
            alertsTable.Columns.Add("Status", typeof(string));
            
            alertsGrid.DataSource = alertsTable;
        }
        
        private void InitializePerformanceCounters()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                // Handle cases where performance counters are not available
                System.Diagnostics.Debug.WriteLine($"Performance counters not available: {ex.Message}");
            }
        }
        
        private void StartButton_Click(object sender, EventArgs e)
        {
            StartMonitoring();
        }
        
        private void StopButton_Click(object sender, EventArgs e)
        {
            StopMonitoring();
        }
        
        private void RefreshInterval_Changed(object sender, EventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo != null && refreshTimer != null)
            {
                refreshTimer.Interval = (int)combo.SelectedItem * 1000;
            }
        }
        
        public void StartMonitoring()
        {
            if (isMonitoring) return;
            
            refreshTimer = new Timer { Interval = 5000 }; // Default 5 seconds
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
            
            isMonitoring = true;
            statusLabel.Text = "Monitoring active";
            statusLabel.ForeColor = Color.LightGreen;
        }
        
        public void StopMonitoring()
        {
            if (!isMonitoring) return;
            
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            
            isMonitoring = false;
            statusLabel.Text = "Monitoring stopped";
            statusLabel.ForeColor = Color.Gray;
        }
        
        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            await CollectMetrics();
        }
        
        private async Task CollectMetrics()
        {
            try
            {
                var metrics = await GetPerformanceMetrics();
                
                // Update chart data
                UpdateChartData(metrics);
                
                // Update metrics grid
                UpdateMetricsGrid(metrics);
                
                // Check for alerts
                CheckForAlerts(metrics);
                
                // Keep only last 60 data points (5 minutes at 5-second intervals)
                if (cpuValues.Count > 60)
                {
                    cpuValues.RemoveAt(0);
                    memoryValues.RemoveAt(0);
                    diskIOValues.RemoveAt(0);
                    activeConnectionsValues.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error collecting metrics: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }
        
        private async Task<PerformanceMetrics> GetPerformanceMetrics()
        {
            var metrics = new PerformanceMetrics();
            
            // Get system metrics
            if (cpuCounter != null)
            {
                metrics.CpuPercent = cpuCounter.NextValue();
            }
            
            if (memoryCounter != null)
            {
                var availableMemory = memoryCounter.NextValue();
                var totalMemory = GC.GetTotalMemory(false) / (1024 * 1024); // Convert to MB
                metrics.MemoryPercent = Math.Max(0, 100 - (availableMemory / totalMemory * 100));
            }
            
            // Get SQL Server specific metrics
            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        
                        // Get active connections
                        var connectionsQuery = @"
                            SELECT COUNT(*) 
                            FROM sys.dm_exec_sessions 
                            WHERE is_user_process = 1";
                        
                        using (var cmd = new SqlCommand(connectionsQuery, connection))
                        {
                            metrics.ActiveConnections = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }
                        
                        // Get buffer cache hit ratio
                        var cacheQuery = @"
                            SELECT cntr_value 
                            FROM sys.dm_os_performance_counters 
                            WHERE counter_name = 'Buffer cache hit ratio'";
                        
                        using (var cmd = new SqlCommand(cacheQuery, connection))
                        {
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                metrics.BufferCacheHitRatio = Convert.ToDouble(result);
                            }
                        }
                        
                        // Get page life expectancy
                        var pleQuery = @"
                            SELECT cntr_value 
                            FROM sys.dm_os_performance_counters 
                            WHERE counter_name = 'Page life expectancy'";
                        
                        using (var cmd = new SqlCommand(pleQuery, connection))
                        {
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                metrics.PageLifeExpectancy = Convert.ToInt32(result);
                            }
                        }
                        
                        // Get blocked processes
                        var blockedQuery = @"
                            SELECT COUNT(*)
                            FROM sys.dm_exec_requests
                            WHERE blocking_session_id > 0";
                        
                        using (var cmd = new SqlCommand(blockedQuery, connection))
                        {
                            metrics.BlockedProcesses = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting SQL Server metrics: {ex.Message}");
                }
            }
            
            metrics.Timestamp = DateTime.Now;
            return metrics;
        }
        
        private void UpdateChartData(PerformanceMetrics metrics)
        {
            cpuValues.Add(metrics.CpuPercent);
            memoryValues.Add(metrics.MemoryPercent);
            diskIOValues.Add(metrics.DiskIOPerSecond);
            activeConnectionsValues.Add(metrics.ActiveConnections);
        }
        
        private void UpdateMetricsGrid(PerformanceMetrics metrics)
        {
            var table = (DataTable)metricsGrid.DataSource;
            table.Clear();
            
            table.Rows.Add("CPU Usage", $"{metrics.CpuPercent:F1}%", "N/A", "N/A", GetStatus(metrics.CpuPercent, 80));
            table.Rows.Add("Memory Usage", $"{metrics.MemoryPercent:F1}%", "N/A", "N/A", GetStatus(metrics.MemoryPercent, 85));
            table.Rows.Add("Active Connections", metrics.ActiveConnections.ToString(), "N/A", "N/A", "Normal");
            table.Rows.Add("Buffer Cache Hit Ratio", $"{metrics.BufferCacheHitRatio:F2}%", "N/A", "N/A", 
                GetStatus(100 - metrics.BufferCacheHitRatio, 5)); // Alert if below 95%
            table.Rows.Add("Page Life Expectancy", $"{metrics.PageLifeExpectancy} sec", "N/A", "N/A", 
                GetStatus(300 - metrics.PageLifeExpectancy, 0)); // Alert if below 300 seconds
            table.Rows.Add("Blocked Processes", metrics.BlockedProcesses.ToString(), "N/A", "N/A", 
                metrics.BlockedProcesses > 0 ? "Warning" : "Normal");
        }
        
        private string GetStatus(double value, double threshold)
        {
            if (value > threshold)
                return "Warning";
            else if (value > threshold * 0.8)
                return "Caution";
            else
                return "Normal";
        }
        
        private void CheckForAlerts(PerformanceMetrics metrics)
        {
            var alerts = new List<PerformanceAlert>();
            
            // CPU threshold
            if (metrics.CpuPercent > 80)
            {
                alerts.Add(new PerformanceAlert
                {
                    Type = "CPU",
                    Metric = "CPU Usage",
                    Value = metrics.CpuPercent,
                    Threshold = 80,
                    Message = "High CPU usage detected",
                    Timestamp = DateTime.Now
                });
            }
            
            // Memory threshold
            if (metrics.MemoryPercent > 85)
            {
                alerts.Add(new PerformanceAlert
                {
                    Type = "Memory",
                    Metric = "Memory Usage",
                    Value = metrics.MemoryPercent,
                    Threshold = 85,
                    Message = "High memory usage detected",
                    Timestamp = DateTime.Now
                });
            }
            
            // Blocked processes
            if (metrics.BlockedProcesses > 0)
            {
                alerts.Add(new PerformanceAlert
                {
                    Type = "Blocking",
                    Metric = "Blocked Processes",
                    Value = metrics.BlockedProcesses,
                    Threshold = 0,
                    Message = $"{metrics.BlockedProcesses} blocked processes detected",
                    Timestamp = DateTime.Now
                });
            }
            
            // Buffer cache hit ratio
            if (metrics.BufferCacheHitRatio < 95)
            {
                alerts.Add(new PerformanceAlert
                {
                    Type = "Cache",
                    Metric = "Buffer Cache Hit Ratio",
                    Value = metrics.BufferCacheHitRatio,
                    Threshold = 95,
                    Message = "Low buffer cache hit ratio",
                    Timestamp = DateTime.Now
                });
            }
            
            // Add alerts to grid and fire events
            foreach (var alert in alerts)
            {
                AddAlert(alert);
                AlertTriggered?.Invoke(this, new PerformanceAlertEventArgs(alert));
            }
        }
        
        private void AddAlert(PerformanceAlert alert)
        {
            var table = (DataTable)alertsGrid.DataSource;
            table.Rows.Add(
                alert.Timestamp,
                alert.Type,
                alert.Metric,
                $"{alert.Value:F2}",
                $"{alert.Threshold:F2}",
                "Active"
            );
            
            // Keep only last 100 alerts
            while (table.Rows.Count > 100)
            {
                table.Rows.RemoveAt(0);
            }
            
            activeAlerts.Add(alert);
        }
        
        public void SetConnectionString(string connectionString)
        {
            this.connectionString = connectionString;
        }
        
        public List<PerformanceAlert> GetActiveAlerts()
        {
            return new List<PerformanceAlert>(activeAlerts);
        }
        
        public void ClearAlerts()
        {
            activeAlerts.Clear();
            var table = (DataTable)alertsGrid.DataSource;
            table.Clear();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopMonitoring();
                cpuCounter?.Dispose();
                memoryCounter?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    
    public class PerformanceMetrics
    {
        public DateTime Timestamp { get; set; }
        public double CpuPercent { get; set; }
        public double MemoryPercent { get; set; }
        public double DiskIOPerSecond { get; set; }
        public int ActiveConnections { get; set; }
        public double BufferCacheHitRatio { get; set; }
        public int PageLifeExpectancy { get; set; }
        public int BlockedProcesses { get; set; }
    }
    
    public class PerformanceAlert
    {
        public string Type { get; set; }
        public string Metric { get; set; }
        public double Value { get; set; }
        public double Threshold { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class PerformanceAlertEventArgs : EventArgs
    {
        public PerformanceAlert Alert { get; }
        
        public PerformanceAlertEventArgs(PerformanceAlert alert)
        {
            Alert = alert;
        }
    }
}
