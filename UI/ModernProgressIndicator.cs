using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SqlServerManager.UI;

namespace SqlServerManager.UI
{
    public class ModernProgressIndicator : UserControl
    {
        private System.Windows.Forms.Timer animationTimer;
        private float animationProgress = 0f;
        private int progressValue = 0;
        private int maximumValue = 100;
        private string statusText = "Loading...";
        private bool isIndeterminate = false;
        private bool showPercentage = true;
        private Color progressColor;
        private Color backgroundColor;
        private ProgressStyle style = ProgressStyle.Bar;

        public event EventHandler ProgressChanged;
        
        public int Value
        {
            get => progressValue;
            set
            {
                if (value >= 0 && value <= maximumValue)
                {
                    progressValue = value;
                    ProgressChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public int Maximum
        {
            get => maximumValue;
            set
            {
                if (value > 0)
                {
                    maximumValue = value;
                    if (progressValue > maximumValue)
                        progressValue = maximumValue;
                    Invalidate();
                }
            }
        }

        public string StatusText
        {
            get => statusText;
            set
            {
                statusText = value ?? "";
                Invalidate();
            }
        }

        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set
            {
                isIndeterminate = value;
                if (value)
                    StartAnimation();
                else
                    StopAnimation();
                Invalidate();
            }
        }

        public bool ShowPercentage
        {
            get => showPercentage;
            set
            {
                showPercentage = value;
                Invalidate();
            }
        }

        public ProgressStyle Style
        {
            get => style;
            set
            {
                style = value;
                Invalidate();
            }
        }

        public ModernProgressIndicator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | 
                    ControlStyles.ResizeRedraw, true);

            this.Size = new Size(300, 40);
            progressColor = Color.FromArgb(0, 120, 215); // Windows blue
            backgroundColor = Color.FromArgb(240, 240, 240);
            
            animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 30 // ~33 FPS
            };
            animationTimer.Tick += AnimationTimer_Tick;

            UpdateColors();
        }

        private void UpdateColors()
        {
            progressColor = ModernThemeManager.CurrentColors.AccentColor;
            backgroundColor = ModernThemeManager.CurrentColors.BackgroundSecondary;
            this.BackColor = ModernThemeManager.CurrentColors.BackgroundPrimary;
            this.ForeColor = ModernThemeManager.CurrentColors.ForegroundPrimary;
        }

        private void StartAnimation()
        {
            if (!animationTimer.Enabled)
                animationTimer.Start();
        }

        private void StopAnimation()
        {
            animationTimer.Stop();
            animationProgress = 0f;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            animationProgress += 0.05f;
            if (animationProgress >= 1f)
                animationProgress = 0f;
            
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            switch (style)
            {
                case ProgressStyle.Bar:
                    DrawProgressBar(g);
                    break;
                case ProgressStyle.Circle:
                    DrawProgressCircle(g);
                    break;
                case ProgressStyle.Ring:
                    DrawProgressRing(g);
                    break;
            }
        }

        private void DrawProgressBar(Graphics g)
        {
            var rect = new Rectangle(0, 0, Width, Height);
            var progressRect = rect;
            progressRect.Height = Math.Max(20, Height / 2);
            progressRect.Y = (Height - progressRect.Height) / 2;
            
            // Background
            using (var bgBrush = new SolidBrush(backgroundColor))
            {
                g.FillRoundedRectangle(bgBrush, progressRect, 10);
            }

            // Progress fill
            if (isIndeterminate)
            {
                DrawIndeterminateBar(g, progressRect);
            }
            else
            {
                var fillWidth = (int)((float)progressRect.Width * progressValue / maximumValue);
                if (fillWidth > 0)
                {
                    var fillRect = new Rectangle(progressRect.X, progressRect.Y, fillWidth, progressRect.Height);
                    using (var progressBrush = new LinearGradientBrush(
                        fillRect, 
                        progressColor, 
                        Color.FromArgb(Math.Min(255, progressColor.R + 30),
                                      Math.Min(255, progressColor.G + 30),
                                      Math.Min(255, progressColor.B + 30)),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRoundedRectangle(progressBrush, fillRect, 10);
                    }
                }
            }

            // Text
            DrawProgressText(g, rect);
        }

        private void DrawIndeterminateBar(Graphics g, Rectangle rect)
        {
            var fillWidth = rect.Width / 3;
            var x = (int)((rect.Width - fillWidth) * animationProgress);
            var fillRect = new Rectangle(rect.X + x, rect.Y, fillWidth, rect.Height);
            
            // Clip to the background rectangle
            var clip = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            fillRect.Intersect(clip);
            
            if (fillRect.Width > 0)
            {
                using (var progressBrush = new LinearGradientBrush(
                    fillRect,
                    Color.Transparent,
                    progressColor,
                    LinearGradientMode.Horizontal))
                {
                    var blend = new ColorBlend();
                    blend.Colors = new[] { Color.Transparent, progressColor, progressColor, Color.Transparent };
                    blend.Positions = new[] { 0.0f, 0.3f, 0.7f, 1.0f };
                    progressBrush.InterpolationColors = blend;
                    
                    g.FillRoundedRectangle(progressBrush, fillRect, 10);
                }
            }
        }

        private void DrawProgressCircle(Graphics g)
        {
            var size = Math.Min(Width, Height) - 10;
            var rect = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);
            var centerX = rect.X + rect.Width / 2f;
            var centerY = rect.Y + rect.Height / 2f;
            var radius = size / 2f - 5;

            // Background circle
            using (var bgBrush = new SolidBrush(backgroundColor))
            {
                g.FillEllipse(bgBrush, rect);
            }

            // Progress arc
            if (isIndeterminate)
            {
                var startAngle = animationProgress * 360f;
                var sweepAngle = 90f;
                
                using (var pen = new Pen(progressColor, 4))
                {
                    g.DrawArc(pen, rect, startAngle, sweepAngle);
                }
            }
            else
            {
                var sweepAngle = (float)(360.0 * progressValue / maximumValue);
                if (sweepAngle > 0)
                {
                    using (var pen = new Pen(progressColor, 4))
                    {
                        g.DrawArc(pen, rect, -90, sweepAngle);
                    }
                }
            }

            // Center text
            DrawCenterText(g, rect);
        }

        private void DrawProgressRing(Graphics g)
        {
            var size = Math.Min(Width, Height) - 10;
            var rect = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);
            var thickness = Math.Max(4, size / 10);

            // Background ring
            using (var pen = new Pen(backgroundColor, thickness))
            {
                g.DrawEllipse(pen, rect);
            }

            // Progress arc
            if (isIndeterminate)
            {
                var startAngle = animationProgress * 360f;
                var sweepAngle = 120f;
                
                using (var pen = new Pen(progressColor, thickness))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, rect, startAngle, sweepAngle);
                }
            }
            else
            {
                var sweepAngle = (float)(360.0 * progressValue / maximumValue);
                if (sweepAngle > 0)
                {
                    using (var pen = new Pen(progressColor, thickness))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawArc(pen, rect, -90, sweepAngle);
                    }
                }
            }

            // Center text
            DrawCenterText(g, rect);
        }

        private void DrawProgressText(Graphics g, Rectangle rect)
        {
            if (string.IsNullOrEmpty(statusText) && !showPercentage)
                return;

            var text = statusText;
            if (showPercentage && !isIndeterminate)
            {
                var percentage = (int)((float)progressValue / maximumValue * 100);
                text = string.IsNullOrEmpty(text) ? $"{percentage}%" : $"{text} ({percentage}%)";
            }

            var font = ModernThemeManager.GetScaledFont(this.Font);
            var textSize = g.MeasureString(text, font);
            var textRect = new RectangleF(
                rect.X + (rect.Width - textSize.Width) / 2,
                rect.Y + rect.Height + 5,
                textSize.Width,
                textSize.Height);

            using (var brush = new SolidBrush(this.ForeColor))
            {
                g.DrawString(text, font, brush, textRect);
            }
        }

        private void DrawCenterText(Graphics g, Rectangle rect)
        {
            if (!showPercentage || isIndeterminate)
            {
                if (!string.IsNullOrEmpty(statusText))
                {
            var font = ModernThemeManager.GetScaledFont(this.Font);
                    var textSize = g.MeasureString(statusText, font);
                    var textRect = new RectangleF(
                        rect.X + (rect.Width - textSize.Width) / 2,
                        rect.Y + (rect.Height - textSize.Height) / 2,
                        textSize.Width,
                        textSize.Height);

                    using (var brush = new SolidBrush(this.ForeColor))
                    {
                        g.DrawString(statusText, font, brush, textRect);
                    }
                }
                return;
            }

            var percentage = (int)((float)progressValue / maximumValue * 100);
            var percentText = $"{percentage}%";
            
            var percentFont = ModernThemeManager.GetScaledFont(this.Font);
            var percentSize = g.MeasureString(percentText, percentFont);
            var percentRect = new RectangleF(
                rect.X + (rect.Width - percentSize.Width) / 2,
                rect.Y + (rect.Height - percentSize.Height) / 2,
                percentSize.Width,
                percentSize.Height);

            using (var brush = new SolidBrush(this.ForeColor))
            {
                g.DrawString(percentText, percentFont, brush, percentRect);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateColors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public enum ProgressStyle
    {
        Bar,
        Circle,
        Ring
    }

    // Extension method for rounded rectangles
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int cornerRadius)
        {
            using (var path = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int cornerRadius)
        {
            using (var path = CreateRoundedRectanglePath(rect, cornerRadius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int cornerRadius)
        {
            var path = new GraphicsPath();
            var diameter = cornerRadius * 2;

            // Top left corner
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            
            // Top right corner
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            
            // Bottom right corner
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            
            // Bottom left corner
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            
            path.CloseFigure();
            return path;
        }
    }
}
