using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace SqlServerManager.UI
{
    public static class PrintUtility
    {
        private static DataTable printDataTable;
        private static string printTitle;
        private static Font printFont;
        private static Font titleFont;
        private static int currentRow;
        private static int pageNumber;
        private static Rectangle printBounds;
        private static float rowHeight;
        private static int[] columnWidths;
        private static string textToPrint;

        /// <summary>
        /// Print a DataGridView with data
        /// </summary>
        public static void PrintDataGridView(DataGridView dataGridView, string title = "Data Report")
        {
            if (dataGridView.DataSource is DataTable dataTable && dataTable.Rows.Count > 0)
            {
                PrintDataTable(dataTable, title);
            }
            else if (dataGridView.Rows.Count > 0)
            {
                // Convert DataGridView rows to DataTable
                var dt = new DataTable();
                
                // Add columns
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    dt.Columns.Add(column.HeaderText);
                }

                // Add rows
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        var dataRow = dt.NewRow();
                        for (int i = 0; i < dataGridView.Columns.Count; i++)
                        {
                            dataRow[i] = row.Cells[i].Value?.ToString() ?? "";
                        }
                        dt.Rows.Add(dataRow);
                    }
                }

                PrintDataTable(dt, title);
            }
            else
            {
                MessageBox.Show("No data to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Print a DataTable
        /// </summary>
        public static void PrintDataTable(DataTable dataTable, string title = "Data Report")
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var printDialog = new PrintDialog())
                using (var printDocument = new PrintDocument())
                {
                    // Setup print document
                    printDocument.PrintPage += PrintDataTable_PrintPage;
                    printDialog.Document = printDocument;

                    // Set up print variables
                    printDataTable = dataTable;
                    printTitle = title;
                    printFont = new Font("Arial", 9);
                    titleFont = new Font("Arial", 14, FontStyle.Bold);
                    currentRow = 0;
                    pageNumber = 1;

                    // Show print dialog
                    if (printDialog.ShowDialog() == DialogResult.OK)
                    {
                        printDocument.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Print Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Print plain text content
        /// </summary>
        public static void PrintText(string text, string title = "Text Document")
        {
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("No text to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var printDialog = new PrintDialog())
                using (var printDocument = new PrintDocument())
                {
                    // Setup print document
                    printDocument.PrintPage += PrintText_PrintPage;
                    printDialog.Document = printDocument;

                    // Set up print variables
                    textToPrint = text;
                    printTitle = title;
                    printFont = new Font("Consolas", 10);
                    titleFont = new Font("Arial", 14, FontStyle.Bold);
                    pageNumber = 1;

                    // Show print dialog
                    if (printDialog.ShowDialog() == DialogResult.OK)
                    {
                        printDocument.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Print Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Show print preview for a DataGridView
        /// </summary>
        public static void PrintPreviewDataGridView(DataGridView dataGridView, string title = "Data Report")
        {
            if (dataGridView.DataSource is DataTable dataTable && dataTable.Rows.Count > 0)
            {
                PrintPreviewDataTable(dataTable, title);
            }
            else
            {
                MessageBox.Show("No data to preview.", "Print Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Show print preview for a DataTable
        /// </summary>
        public static void PrintPreviewDataTable(DataTable dataTable, string title = "Data Report")
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to preview.", "Print Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var printDocument = new PrintDocument())
                using (var printPreviewDialog = new PrintPreviewDialog())
                {
                    // Setup print document
                    printDocument.PrintPage += PrintDataTable_PrintPage;
                    printPreviewDialog.Document = printDocument;

                    // Set up print variables
                    printDataTable = dataTable;
                    printTitle = title;
                    printFont = new Font("Arial", 9);
                    titleFont = new Font("Arial", 14, FontStyle.Bold);
                    currentRow = 0;
                    pageNumber = 1;

                    // Configure preview dialog
                    printPreviewDialog.WindowState = FormWindowState.Maximized;
                    printPreviewDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing print preview: {ex.Message}", "Print Preview Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Show print preview for text content
        /// </summary>
        public static void PrintPreviewText(string text, string title = "Text Document")
        {
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("No text to preview.", "Print Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var printDocument = new PrintDocument())
                using (var printPreviewDialog = new PrintPreviewDialog())
                {
                    // Setup print document
                    printDocument.PrintPage += PrintText_PrintPage;
                    printPreviewDialog.Document = printDocument;

                    // Set up print variables
                    textToPrint = text;
                    printTitle = title;
                    printFont = new Font("Consolas", 10);
                    titleFont = new Font("Arial", 14, FontStyle.Bold);
                    pageNumber = 1;

                    // Configure preview dialog
                    printPreviewDialog.WindowState = FormWindowState.Maximized;
                    printPreviewDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing print preview: {ex.Message}", "Print Preview Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void PrintDataTable_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                printBounds = e.MarginBounds;
                var yPos = printBounds.Top;
                var graphics = e.Graphics;

                // Print title
                var titleSize = graphics.MeasureString(printTitle, titleFont);
                var titleX = printBounds.Left + (printBounds.Width - titleSize.Width) / 2;
                graphics.DrawString(printTitle, titleFont, Brushes.Black, titleX, yPos);
                yPos += (int)titleSize.Height + 20;

                // Print date and page number
                var dateStr = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                var pageStr = $"Page {pageNumber}";
                graphics.DrawString(dateStr, printFont, Brushes.Black, printBounds.Left, yPos);
                var pageSize = graphics.MeasureString(pageStr, printFont);
                graphics.DrawString(pageStr, printFont, Brushes.Black, 
                    printBounds.Right - pageSize.Width, yPos);
                yPos += (int)graphics.MeasureString(dateStr, printFont).Height + 10;

                // Calculate column widths if not done yet
                if (columnWidths == null)
                {
                    CalculateColumnWidths(graphics);
                }

                // Calculate row height
                rowHeight = graphics.MeasureString("Ay", printFont).Height + 4;

                // Print column headers if this is the first row on the page
                if (currentRow == 0 || yPos == printBounds.Top + titleSize.Height + 50)
                {
                    var headerY = yPos;
                    var headerX = printBounds.Left;

                    // Draw header background
                    graphics.FillRectangle(Brushes.LightGray, 
                        headerX, headerY, printBounds.Width, rowHeight);

                    for (int col = 0; col < printDataTable.Columns.Count; col++)
                    {
                        var headerText = printDataTable.Columns[col].ColumnName;
                        var headerRect = new RectangleF(headerX, headerY + 2, columnWidths[col], rowHeight - 4);
                        
                        graphics.DrawString(headerText, printFont, Brushes.Black, headerRect);
                        graphics.DrawRectangle(Pens.Black, headerX, headerY, columnWidths[col], (int)rowHeight);
                        
                        headerX += columnWidths[col];
                    }

                    yPos += (int)rowHeight;
                }

                // Print data rows
                while (currentRow < printDataTable.Rows.Count && yPos + rowHeight <= printBounds.Bottom)
                {
                    var dataRow = printDataTable.Rows[currentRow];
                    var cellX = printBounds.Left;

                    for (int col = 0; col < printDataTable.Columns.Count; col++)
                    {
                        var cellValue = dataRow[col]?.ToString() ?? "";
                        var cellRect = new RectangleF(cellX + 2, yPos + 2, columnWidths[col] - 4, rowHeight - 4);
                        
                        // Truncate text if too long
                        var cellText = TruncateText(graphics, cellValue, columnWidths[col] - 4);
                        
                        graphics.DrawString(cellText, printFont, Brushes.Black, cellRect);
                        graphics.DrawRectangle(Pens.Black, cellX, (int)yPos, columnWidths[col], (int)rowHeight);
                        
                        cellX += columnWidths[col];
                    }

                    yPos += (int)rowHeight;
                    currentRow++;
                }

                // Check if more pages are needed
                if (currentRow < printDataTable.Rows.Count)
                {
                    e.HasMorePages = true;
                    pageNumber++;
                }
                else
                {
                    e.HasMorePages = false;
                    // Reset for next print job
                    currentRow = 0;
                    pageNumber = 1;
                    columnWidths = null;
                }
            }
            catch (Exception ex)
            {
                // Handle printing error
                MessageBox.Show($"Error during printing: {ex.Message}", "Print Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.HasMorePages = false;
            }
        }

        private static void PrintText_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                printBounds = e.MarginBounds;
                var yPos = printBounds.Top;
                var graphics = e.Graphics;

                // Print title
                var titleSize = graphics.MeasureString(printTitle, titleFont);
                var titleX = printBounds.Left + (printBounds.Width - titleSize.Width) / 2;
                graphics.DrawString(printTitle, titleFont, Brushes.Black, titleX, yPos);
                yPos += (int)titleSize.Height + 20;

                // Print date and page number
                var dateStr = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                var pageStr = $"Page {pageNumber}";
                graphics.DrawString(dateStr, printFont, Brushes.Black, printBounds.Left, yPos);
                var pageSize = graphics.MeasureString(pageStr, printFont);
                graphics.DrawString(pageStr, printFont, Brushes.Black, 
                    printBounds.Right - pageSize.Width, yPos);
                yPos += (int)graphics.MeasureString(dateStr, printFont).Height + 20;

                // Calculate line height
                var lineHeight = graphics.MeasureString("Ay", printFont).Height;

                // Split text into lines
                var lines = textToPrint.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                var linesPerPage = (int)((printBounds.Bottom - yPos) / lineHeight);

                // Calculate starting line for this page
                var startLine = (pageNumber - 1) * linesPerPage;
                var endLine = Math.Min(startLine + linesPerPage, lines.Length);

                // Print lines
                for (int i = startLine; i < endLine; i++)
                {
                    if (yPos + lineHeight > printBounds.Bottom)
                        break;

                    var line = lines[i];
                    
                    // Wrap long lines
                    var wrappedLines = WrapText(graphics, line, printBounds.Width);
                    foreach (var wrappedLine in wrappedLines)
                    {
                        if (yPos + lineHeight > printBounds.Bottom)
                            break;

                        graphics.DrawString(wrappedLine, printFont, Brushes.Black, printBounds.Left, yPos);
                        yPos += (int)lineHeight;
                    }
                }

                // Check if more pages are needed
                if (endLine < lines.Length)
                {
                    e.HasMorePages = true;
                    pageNumber++;
                }
                else
                {
                    e.HasMorePages = false;
                    pageNumber = 1; // Reset for next print job
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during printing: {ex.Message}", "Print Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.HasMorePages = false;
            }
        }

        private static void CalculateColumnWidths(Graphics graphics)
        {
            columnWidths = new int[printDataTable.Columns.Count];
            var totalWidth = printBounds.Width - 10; // Leave some margin
            var availableWidth = totalWidth;

            // First pass: calculate minimum widths based on headers
            for (int col = 0; col < printDataTable.Columns.Count; col++)
            {
                var headerText = printDataTable.Columns[col].ColumnName;
                var headerWidth = (int)graphics.MeasureString(headerText, printFont).Width + 10;
                columnWidths[col] = headerWidth;
            }

            // Second pass: adjust for content (sample first 10 rows)
            var sampleRows = Math.Min(10, printDataTable.Rows.Count);
            for (int row = 0; row < sampleRows; row++)
            {
                for (int col = 0; col < printDataTable.Columns.Count; col++)
                {
                    var cellValue = printDataTable.Rows[row][col]?.ToString() ?? "";
                    if (cellValue.Length > 0)
                    {
                        var cellWidth = (int)graphics.MeasureString(cellValue, printFont).Width + 10;
                        columnWidths[col] = Math.Max(columnWidths[col], cellWidth);
                    }
                }
            }

            // Third pass: ensure columns fit within page width
            var totalCalculatedWidth = columnWidths.Sum();
            if (totalCalculatedWidth > totalWidth)
            {
                // Scale down proportionally
                var scaleFactor = (double)totalWidth / totalCalculatedWidth;
                for (int col = 0; col < columnWidths.Length; col++)
                {
                    columnWidths[col] = Math.Max(50, (int)(columnWidths[col] * scaleFactor));
                }
            }
            else if (totalCalculatedWidth < totalWidth)
            {
                // Distribute extra space proportionally
                var extraSpace = totalWidth - totalCalculatedWidth;
                var spacePerColumn = extraSpace / columnWidths.Length;
                for (int col = 0; col < columnWidths.Length; col++)
                {
                    columnWidths[col] += spacePerColumn;
                }
            }
        }

        private static string TruncateText(Graphics graphics, string text, int maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var textWidth = graphics.MeasureString(text, printFont).Width;
            if (textWidth <= maxWidth)
                return text;

            // Binary search for the best fit
            var low = 0;
            var high = text.Length;
            var bestFit = "";

            while (low <= high)
            {
                var mid = (low + high) / 2;
                var candidate = text.Substring(0, mid) + "...";
                var candidateWidth = graphics.MeasureString(candidate, printFont).Width;

                if (candidateWidth <= maxWidth)
                {
                    bestFit = candidate;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return bestFit;
        }

        private static string[] WrapText(Graphics graphics, string text, int maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return new[] { "" };

            var words = text.Split(' ');
            var lines = new System.Collections.Generic.List<string>();
            var currentLine = "";

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var testWidth = graphics.MeasureString(testLine, printFont).Width;

                if (testWidth <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        // Single word is too long, break it
                        lines.Add(word);
                        currentLine = "";
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines.ToArray();
        }
    }
}
