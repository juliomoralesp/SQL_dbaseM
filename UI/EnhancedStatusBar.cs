using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SqlServerManager.UI
{
    public class EnhancedStatusBar : StatusStrip
    {
        private ToolStripStatusLabel _connectionStatusLabel;
        private ToolStripStatusLabel _connectionInfoLabel;
        private ToolStripStatusLabel _queryTimeLabel;
        private ToolStripStatusLabel _rowsAffectedLabel;
        private ToolStripProgressBar _progressBar;
        private ToolStripStatusLabel _memoryUsageLabel;
        private ToolStripStatusLabel _timeLabel;
        private System.Windows.Forms.Timer _updateTimer;
        private System.Windows.Forms.Timer _memoryTimer;

        public EnhancedStatusBar()
        {
            InitializeComponents();
            SetupTimers();
        }

        private void InitializeComponents()
        {
            SuspendLayout();

            // Connection Status Indicator
            _connectionStatusLabel = new ToolStripStatusLabel
            {
                Text = "●",
                ForeColor = Color.Red,
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                ToolTipText = "Disconnected"
            };

            // Connection Information
            _connectionInfoLabel = new ToolStripStatusLabel
            {
                Text = "No Connection",
                AutoSize = false,
                Width = 280, // Increased from 200
                TextAlign = ContentAlignment.MiddleLeft,
                BorderSides = ToolStripStatusLabelBorderSides.Right
            };

            // Query Execution Time
            _queryTimeLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                AutoSize = false,
                Width = 180, // Increased from 120
                TextAlign = ContentAlignment.MiddleLeft,
                BorderSides = ToolStripStatusLabelBorderSides.Right
            };

            // Rows Affected/Returned
            _rowsAffectedLabel = new ToolStripStatusLabel
            {
                Text = "",
                AutoSize = false,
                Width = 160, // Increased from 100
                TextAlign = ContentAlignment.MiddleLeft,
                BorderSides = ToolStripStatusLabelBorderSides.Right
            };

            // Progress Bar for background operations
            _progressBar = new ToolStripProgressBar
            {
                Size = new Size(150, 16),
                Visible = false,
                Style = ProgressBarStyle.Continuous
            };

            // Memory Usage
            _memoryUsageLabel = new ToolStripStatusLabel
            {
                Text = "Memory: 0 MB",
                AutoSize = false,
                Width = 130, // Increased from 100
                TextAlign = ContentAlignment.MiddleLeft,
                BorderSides = ToolStripStatusLabelBorderSides.Right
            };

            // Current Time
            _timeLabel = new ToolStripStatusLabel
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Add all items to status strip
            Items.AddRange(new ToolStripItem[]
            {
                _connectionStatusLabel,
                _connectionInfoLabel,
                _queryTimeLabel,
                _rowsAffectedLabel,
                _progressBar,
                _memoryUsageLabel,
                _timeLabel
            });

            ResumeLayout(false);
        }

        private void SetupTimers()
        {
            // Timer for updating time every second
            _updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000,
                Enabled = true
            };
            _updateTimer.Tick += (s, e) => UpdateTime();

            // Timer for updating memory usage every 5 seconds
            _memoryTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000,
                Enabled = true
            };
            _memoryTimer.Tick += (s, e) => UpdateMemoryUsage();
        }

        #region Connection Status Methods

        public void SetConnectionStatus(bool isConnected, string connectionInfo = "")
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetConnectionStatus(isConnected, connectionInfo)));
                return;
            }

            if (isConnected)
            {
                _connectionStatusLabel.Text = "●";
                _connectionStatusLabel.ForeColor = Color.Green;
                _connectionStatusLabel.ToolTipText = "Connected";
                _connectionInfoLabel.Text = !string.IsNullOrEmpty(connectionInfo) ? connectionInfo : "Connected";
            }
            else
            {
                _connectionStatusLabel.Text = "●";
                _connectionStatusLabel.ForeColor = Color.Red;
                _connectionStatusLabel.ToolTipText = "Disconnected";
                _connectionInfoLabel.Text = "No Connection";
            }
        }

        public void SetConnectionBusy(bool isBusy)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetConnectionBusy(isBusy)));
                return;
            }

            if (isBusy)
            {
                _connectionStatusLabel.ForeColor = Color.Orange;
                _connectionStatusLabel.ToolTipText = "Busy";
            }
            else
            {
                _connectionStatusLabel.ForeColor = Color.Green;
                _connectionStatusLabel.ToolTipText = "Connected";
            }
        }

        #endregion

        #region Query Execution Methods

        public void SetQueryExecuting(bool isExecuting, string queryType = "")
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetQueryExecuting(isExecuting, queryType)));
                return;
            }

            if (isExecuting)
            {
                _queryTimeLabel.Text = $"Executing {queryType}...";
                _queryTimeLabel.ForeColor = Color.Blue;
                SetConnectionBusy(true);
            }
            else
            {
                _queryTimeLabel.Text = "Ready";
                _queryTimeLabel.ForeColor = SystemColors.ControlText;
                SetConnectionBusy(false);
            }
        }

        public void SetQueryExecutionTime(TimeSpan executionTime, bool isError = false)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetQueryExecutionTime(executionTime, isError)));
                return;
            }

            if (isError)
            {
                _queryTimeLabel.Text = "Error";
                _queryTimeLabel.ForeColor = Color.Red;
            }
            else
            {
                _queryTimeLabel.Text = $"Executed: {FormatExecutionTime(executionTime)}";
                _queryTimeLabel.ForeColor = Color.DarkGreen;
            }

            SetConnectionBusy(false);
        }

        private string FormatExecutionTime(TimeSpan time)
        {
            if (time.TotalSeconds < 1)
                return $"{time.TotalMilliseconds:F0} ms";
            else if (time.TotalMinutes < 1)
                return $"{time.TotalSeconds:F2} sec";
            else
                return $"{time.TotalMinutes:F1} min";
        }

        #endregion

        #region Rows Affected/Returned Methods

        public void SetRowsAffected(int rows, string operation = "affected")
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetRowsAffected(rows, operation)));
                return;
            }

            if (rows >= 0)
            {
                _rowsAffectedLabel.Text = $"{rows:N0} {operation}";
                _rowsAffectedLabel.Visible = true;
            }
            else
            {
                _rowsAffectedLabel.Visible = false;
            }
        }

        public void ClearRowsAffected()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ClearRowsAffected));
                return;
            }

            _rowsAffectedLabel.Text = "";
            _rowsAffectedLabel.Visible = false;
        }

        #endregion

        #region Progress Bar Methods

        public void ShowProgress(bool show = true)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowProgress(show)));
                return;
            }

            _progressBar.Visible = show;
            if (show)
            {
                _progressBar.Style = ProgressBarStyle.Marquee;
                _progressBar.MarqueeAnimationSpeed = 50;
            }
        }

        public void SetProgress(int value, int maximum = 100)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetProgress(value, maximum)));
                return;
            }

            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Maximum = maximum;
            _progressBar.Value = Math.Min(value, maximum);
            _progressBar.Visible = true;
        }

        public void HideProgress()
        {
            ShowProgress(false);
        }

        #endregion

        #region Background Update Methods

        private void UpdateTime()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateTime));
                return;
            }

            _timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void UpdateMemoryUsage()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateMemoryUsage));
                return;
            }

            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    var memoryMB = process.WorkingSet64 / 1024 / 1024;
                    _memoryUsageLabel.Text = $"Memory: {memoryMB:N0} MB";
                    
                    // Change color based on memory usage
                    if (memoryMB > 500)
                        _memoryUsageLabel.ForeColor = Color.Red;
                    else if (memoryMB > 200)
                        _memoryUsageLabel.ForeColor = Color.Orange;
                    else
                        _memoryUsageLabel.ForeColor = SystemColors.ControlText;
                }
            }
            catch
            {
                _memoryUsageLabel.Text = "Memory: N/A";
                _memoryUsageLabel.ForeColor = SystemColors.ControlText;
            }
        }

        #endregion

        #region Search Results Methods
        
        public void SetSearchResults(int totalResults, int duplicatesSkipped = 0)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetSearchResults(totalResults, duplicatesSkipped)));
                return;
            }

            string text = $"{totalResults:N0} results";
            if (duplicatesSkipped > 0)
            {
                text += $" ({duplicatesSkipped:N0} duplicates skipped)";
            }
            
            _rowsAffectedLabel.Text = text;
            _rowsAffectedLabel.Visible = true;
        }
        
        public void SetOperationStatus(string operation, int processed, int duplicatesSkipped = 0)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetOperationStatus(operation, processed, duplicatesSkipped)));
                return;
            }

            string text = $"{processed:N0} {operation}";
            if (duplicatesSkipped > 0)
            {
                text += $" ({duplicatesSkipped:N0} duplicates skipped)";
            }
            
            _rowsAffectedLabel.Text = text;
            _rowsAffectedLabel.Visible = true;
        }
        
        #endregion
        
        #region Message Methods

        public void ShowMessage(string message, MessageType type = MessageType.Info, int timeoutMs = 3000)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowMessage(message, type, timeoutMs)));
                return;
            }

            var originalText = _queryTimeLabel.Text;
            var originalColor = _queryTimeLabel.ForeColor;

            _queryTimeLabel.Text = message;
            _queryTimeLabel.ForeColor = GetMessageColor(type);

            if (timeoutMs > 0)
            {
                var resetTimer = new System.Windows.Forms.Timer
                {
                    Interval = timeoutMs,
                    Enabled = true
                };
                resetTimer.Tick += (s, e) =>
                {
                    resetTimer.Stop();
                    resetTimer.Dispose();
                    
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            _queryTimeLabel.Text = originalText;
                            _queryTimeLabel.ForeColor = originalColor;
                        }));
                    }
                    else
                    {
                        _queryTimeLabel.Text = originalText;
                        _queryTimeLabel.ForeColor = originalColor;
                    }
                };
            }
        }

        private Color GetMessageColor(MessageType type)
        {
            return type switch
            {
                MessageType.Error => Color.Red,
                MessageType.Warning => Color.Orange,
                MessageType.Success => Color.Green,
                MessageType.Info => Color.Blue,
                _ => SystemColors.ControlText
            };
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
                _memoryTimer?.Stop();
                _memoryTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    public enum MessageType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
