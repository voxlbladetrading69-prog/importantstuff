using System;
using System.Windows.Forms;

namespace Opus
{
    public partial class AddDeviceDialog : Form
    {
        public string EnteredHwid => (HwidTextBox.TextContent ?? string.Empty).Trim();

        public AddDeviceDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EnteredHwid))
            {
                MessageBox.Show("Please enter a valid HWID.", "HWID Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
