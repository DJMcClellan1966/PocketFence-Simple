#nullable disable
using System.Drawing;
using System.Windows.Forms;

namespace PocketFence.UI
{
    public partial class InputDialog : Form
    {
        public string InputText { get; private set; } = string.Empty;

        private Label _promptLabel;
        private TextBox _inputTextBox;
        private Button _okButton;
        private Button _cancelButton;

        public InputDialog(string prompt, string title = "Input", string defaultValue = "")
        {
            InitializeComponent();
            _promptLabel.Text = prompt;
            this.Text = title;
            _inputTextBox.Text = defaultValue;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            _promptLabel = new Label
            {
                Location = new Point(12, 15),
                Size = new Size(360, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _inputTextBox = new TextBox
            {
                Location = new Point(12, 45),
                Size = new Size(360, 23)
            };

            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(210, 80),
                Size = new Size(75, 23)
            };
            _okButton.Click += (sender, e) => 
            {
                InputText = _inputTextBox.Text;
                this.Close();
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(297, 80),
                Size = new Size(75, 23)
            };

            this.Controls.AddRange(new Control[] { _promptLabel, _inputTextBox, _okButton, _cancelButton });
            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        public static string Show(string prompt, string title = "Input", string defaultValue = "")
        {
            using (var dialog = new InputDialog(prompt, title, defaultValue))
            {
                return dialog.ShowDialog() == DialogResult.OK ? dialog.InputText : string.Empty;
            }
        }
    }
}