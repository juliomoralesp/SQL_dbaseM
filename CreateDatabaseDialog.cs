using System;
using System.Drawing;
using System.Windows.Forms;

namespace SqlServerManager
{
    public class CreateDatabaseDialog : Form
    {
        private TextBox databaseNameTextBox;
        private Label nameLabel;
        private Button okButton;
        private Button cancelButton;
        
        public string DatabaseName => databaseNameTextBox.Text;

        public CreateDatabaseDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Create New Database";
            this.Size = new Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            nameLabel = new Label();
            nameLabel.Text = "Database Name:";
            nameLabel.Location = new Point(20, 25);
            nameLabel.Size = new Size(100, 20);

            databaseNameTextBox = new TextBox();
            databaseNameTextBox.Location = new Point(125, 23);
            databaseNameTextBox.Size = new Size(230, 25);

            okButton = new Button();
            okButton.Text = "Create";
            okButton.Location = new Point(195, 65);
            okButton.Size = new Size(75, 30);
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += OkButton_Click;

            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(280, 65);
            cancelButton.Size = new Size(75, 30);
            cancelButton.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                nameLabel, databaseNameTextBox,
                okButton, cancelButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(databaseNameTextBox.Text))
            {
                MessageBox.Show("Please enter a database name.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
            }
        }
    }
}
