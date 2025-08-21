using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SqlServerManager.UI
{
    /// <summary>
    /// Centralized loading indicator management for consistent UX across the application
    /// </summary>
    public static class LoadingManager
    {
        private static readonly Dictionary<Control, LoadingOverlay> _activeOverlays = new Dictionary<Control, LoadingOverlay>();

        /// <summary>
        /// Show a loading indicator over the specified control
        /// </summary>
        public static void ShowLoading(Control parent, string message = "Loading...", ProgressStyle style = ProgressStyle.Ring)
        {
            if (parent == null) return;

            // Remove existing overlay if present
            HideLoading(parent);

            var overlay = new LoadingOverlay(message, style);
            _activeOverlays[parent] = overlay;

            // Add overlay to parent
            parent.Controls.Add(overlay);
            overlay.BringToFront();
            overlay.Show();

            // Set wait cursor
            parent.Cursor = Cursors.WaitCursor;
            
            LoggingService.LogDebug("Loading indicator shown for {ControlType} with message: {Message}", 
                parent.GetType().Name, message);
        }

        /// <summary>
        /// Hide the loading indicator for the specified control
        /// </summary>
        public static void HideLoading(Control parent)
        {
            if (parent == null) return;

            if (_activeOverlays.TryGetValue(parent, out var overlay))
            {
                try
                {
                    overlay.Hide();
                    parent.Controls.Remove(overlay);
                    overlay.Dispose();
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning("Error disposing loading overlay for {ControlType}: {Exception}", parent.GetType().Name, ex.Message);
                }
                finally
                {
                    _activeOverlays.Remove(parent);
                }
            }

            // Reset cursor
            parent.Cursor = Cursors.Default;
            
            LoggingService.LogDebug("Loading indicator hidden for {ControlType}", parent.GetType().Name);
        }

        /// <summary>
        /// Execute an async operation with automatic loading indicator
        /// </summary>
        public static async Task ExecuteWithLoadingAsync(Control parent, Func<Task> operation, 
            string message = "Loading...", ProgressStyle style = ProgressStyle.Ring)
        {
            ShowLoading(parent, message, style);
            try
            {
                await operation();
                LoggingService.LogInformation("Async operation completed successfully for {ControlType}", parent.GetType().Name);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Async operation failed for {ControlType}", parent.GetType().Name);
                throw; // Re-throw to let the caller handle it
            }
            finally
            {
                HideLoading(parent);
            }
        }

        /// <summary>
        /// Execute an async operation with automatic loading indicator and return result
        /// </summary>
        public static async Task<T> ExecuteWithLoadingAsync<T>(Control parent, Func<Task<T>> operation, 
            string message = "Loading...", ProgressStyle style = ProgressStyle.Ring)
        {
            ShowLoading(parent, message, style);
            try
            {
                var result = await operation();
                LoggingService.LogInformation("Async operation completed successfully for {ControlType}", parent.GetType().Name);
                return result;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Async operation failed for {ControlType}", parent.GetType().Name);
                throw; // Re-throw to let the caller handle it
            }
            finally
            {
                HideLoading(parent);
            }
        }

        /// <summary>
        /// Show progress for a long-running operation with determinate progress
        /// </summary>
        public static IProgressReporter ShowProgress(Control parent, string message = "Processing...", 
            int maximum = 100)
        {
            if (parent == null) return new NullProgressReporter();

            // Remove existing overlay if present
            HideLoading(parent);

            var overlay = new LoadingOverlay(message, ProgressStyle.Bar, false, maximum);
            _activeOverlays[parent] = overlay;

            // Add overlay to parent
            parent.Controls.Add(overlay);
            overlay.BringToFront();
            overlay.Show();

            // Set wait cursor
            parent.Cursor = Cursors.WaitCursor;

            LoggingService.LogDebug("Progress indicator shown for {ControlType} with message: {Message}", 
                parent.GetType().Name, message);

            return overlay;
        }

        /// <summary>
        /// Clean up all active loading indicators (typically called on application shutdown)
        /// </summary>
        public static void Cleanup()
        {
            var overlaysToRemove = new List<Control>(_activeOverlays.Keys);
            foreach (var parent in overlaysToRemove)
            {
                HideLoading(parent);
            }
        }
    }

    /// <summary>
    /// Loading overlay that covers the parent control
    /// </summary>
    internal class LoadingOverlay : Panel, IProgressReporter
    {
        private readonly ModernProgressIndicator _progressIndicator;
        private readonly Label _messageLabel;

        public LoadingOverlay(string message, ProgressStyle style, bool isIndeterminate = true, int maximum = 100)
        {
            // Setup overlay panel
            BackColor = Color.FromArgb(128, ModernThemeManager.CurrentColors.BackgroundPrimary.R, 
                ModernThemeManager.CurrentColors.BackgroundPrimary.G, 
                ModernThemeManager.CurrentColors.BackgroundPrimary.B);
            Dock = DockStyle.Fill;
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Create progress indicator
            _progressIndicator = new ModernProgressIndicator
            {
                Style = style,
                IsIndeterminate = isIndeterminate,
                Maximum = maximum,
                StatusText = message,
                Size = new Size(200, 50),
                ShowPercentage = !isIndeterminate
            };

            // Create message label (for additional context if needed)
            _messageLabel = new Label
            {
                Text = message,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = ModernThemeManager.GetScaledFont(SystemFonts.DefaultFont),
                ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary,
                BackColor = Color.Transparent,
                Size = new Size(300, 30)
            };

            // Add controls
            Controls.Add(_progressIndicator);
            if (style != ProgressStyle.Circle && style != ProgressStyle.Ring) // These have built-in text
            {
                Controls.Add(_messageLabel);
            }

            // Position controls
            ResizeControls();
            Resize += (s, e) => ResizeControls();
        }

        private void ResizeControls()
        {
            if (_progressIndicator != null)
            {
                _progressIndicator.Location = new Point(
                    (Width - _progressIndicator.Width) / 2,
                    (Height - _progressIndicator.Height) / 2);
            }

            if (_messageLabel != null && _messageLabel.Visible)
            {
                _messageLabel.Location = new Point(
                    (Width - _messageLabel.Width) / 2,
                    _progressIndicator.Bottom + 10);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw semi-transparent background
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            base.OnPaint(e);
        }

        // IProgressReporter implementation
        public void UpdateProgress(int value, string message = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string>(UpdateProgress), value, message);
                return;
            }

            if (_progressIndicator != null)
            {
                _progressIndicator.Value = value;
                if (!string.IsNullOrEmpty(message))
                {
                    _progressIndicator.StatusText = message;
                    if (_messageLabel != null && _messageLabel.Visible)
                    {
                        _messageLabel.Text = message;
                    }
                }
            }
        }

        public void SetMaximum(int maximum)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SetMaximum), maximum);
                return;
            }

            if (_progressIndicator != null)
            {
                _progressIndicator.Maximum = maximum;
            }
        }

        public void Complete(string finalMessage = "Complete")
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Complete), finalMessage);
                return;
            }

            UpdateProgress(_progressIndicator?.Maximum ?? 100, finalMessage);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _progressIndicator?.Dispose();
                _messageLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Interface for progress reporting
    /// </summary>
    public interface IProgressReporter
    {
        void UpdateProgress(int value, string message = null);
        void SetMaximum(int maximum);
        void Complete(string finalMessage = "Complete");
    }

    /// <summary>
    /// Null implementation of progress reporter for cases where parent is null
    /// </summary>
    internal class NullProgressReporter : IProgressReporter
    {
        public void UpdateProgress(int value, string message = null) { }
        public void SetMaximum(int maximum) { }
        public void Complete(string finalMessage = "Complete") { }
    }
}
