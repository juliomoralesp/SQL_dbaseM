using System;
using System.Drawing;
using System.Windows.Forms;

namespace SqlServerManager
{
    public class InputDialog : Form
    {
        private TextBox inputTextBox;
        private Label promptLabel;
        private Button okButton;
        private Button cancelButton;
        
        public string InputValue => inputTextBox.Text;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent(title, prompt, defaultValue);
        }

        private void InitializeComponent(string title, string prompt, string defaultValue)
        {
            this.Text = title;
            this.Size = new Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            promptLabel = new Label();
            promptLabel.Text = prompt;
            promptLabel.Location = new Point(20, 25);
            promptLabel.Size = new Size(100, 20);

            inputTextBox = new TextBox();
            inputTextBox.Location = new Point(20, 50);
            inputTextBox.Size = new Size(340, 25);
            inputTextBox.Text = defaultValue;

            okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(195, 85);
            okButton.Size = new Size(75, 25);
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += OkButton_Click;

            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(280, 85);
            cancelButton.Size = new Size(75, 25);
            cancelButton.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                promptLabel, inputTextBox,
                okButton, cancelButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputTextBox.Text))
            {
                MessageBox.Show("Please enter a value.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
            }
        }
    }
}
