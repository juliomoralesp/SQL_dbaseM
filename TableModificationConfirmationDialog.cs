using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SqlServerManager
{
    public class TableModificationConfirmationDialog : Form
    {
        private TextBox sqlPreviewTextBox;
        private Button executeButton;
        private Button cancelButton;
        private Label instructionLabel;
        
        public TableModificationConfirmationDialog(List<string> alterStatements)
        {
            InitializeComponent();
            LoadStatements(alterStatements);
            
            // Apply theme and font
            ThemeManager.ApplyThemeToDialog(this);
            FontManager.ApplyFontSize(this, FontManager.CurrentFontSize / 10f);
        }
        
        private void InitializeComponent()
        {
            this.Text = "Confirm Table Modifications";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(600, 400);
            
            // Instruction label
            instructionLabel = new Label();
            instructionLabel.Text = "The following SQL statements will be executed to modify the table:";
            instructionLabel.Location = new Point(20, 20);
            instructionLabel.Size = new Size(640, 40);
            instructionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            instructionLabel.Font = FontManager.GetScaledFont(9, FontStyle.Bold);
            
            // SQL preview text box
            sqlPreviewTextBox = new TextBox();
            sqlPreviewTextBox.Location = new Point(20, 70);
            sqlPreviewTextBox.Size = new Size(640, 340);
            sqlPreviewTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            sqlPreviewTextBox.Multiline = true;
            sqlPreviewTextBox.ScrollBars = ScrollBars.Both;
            sqlPreviewTextBox.ReadOnly = true;
            sqlPreviewTextBox.Font = new Font("Courier New", FontManager.CurrentFontSize - 1, FontStyle.Regular);
            sqlPreviewTextBox.BackColor = SystemColors.Control;
            sqlPreviewTextBox.BorderStyle = BorderStyle.Fixed3D;
            
            // Execute button
            executeButton = new Button();
            executeButton.Text = "Execute Changes";
            executeButton.Location = new Point(480, 430);
            executeButton.Size = new Size(120, 30);
            executeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            executeButton.DialogResult = DialogResult.OK;
            executeButton.Font = FontManager.GetScaledFont(9, FontStyle.Bold);
            executeButton.BackColor = Color.FromArgb(0, 120, 215);
            executeButton.ForeColor = Color.White;
            executeButton.FlatStyle = FlatStyle.Flat;
            
            // Cancel button
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(610, 430);
            cancelButton.Size = new Size(80, 30);
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Font = FontManager.GetScaledFont(9);
            
            // Add controls
            this.Controls.AddRange(new Control[] {
                instructionLabel, sqlPreviewTextBox, executeButton, cancelButton
            });
            
            this.AcceptButton = executeButton;
            this.CancelButton = cancelButton;
        }
        
        private void LoadStatements(List<string> statements)
        {
            if (statements == null || statements.Count == 0)
            {
                sqlPreviewTextBox.Text = "-- No changes detected";
                executeButton.Enabled = false;
                return;
            }
            
            var filteredStatements = statements.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            sqlPreviewTextBox.Text = string.Join("\r\n\r\n", filteredStatements);
            
            // Update instruction label with count
            instructionLabel.Text = $"The following {filteredStatements.Count} SQL statement(s) will be executed:";
            
            // Enable execute button only if there are actual SQL statements (not just comments)
            bool hasExecutableStatements = filteredStatements.Any(s => 
                !s.TrimStart().StartsWith("--") && !string.IsNullOrWhiteSpace(s));
            executeButton.Enabled = hasExecutableStatements;
            
            if (!hasExecutableStatements)
            {
                executeButton.Text = "No Changes";
            }
        }
    }
}
