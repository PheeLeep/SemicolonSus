using System;
using System.Windows.Forms;

namespace SemicolonSus {
    public partial class SettingsDialog : Form {
        bool initialized = false;
        public SettingsDialog() {
            InitializeComponent();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e) {
            if (initialized) {
                Properties.Settings.Default.IsRandomized = checkBox1.Checked;
                Properties.Settings.Default.Save();
            }
            groupBox1.Enabled = checkBox1.Checked;
        }

        private void TrackBar1_Scroll(object sender, EventArgs e) {
            if (initialized) {
                Properties.Settings.Default.RandomizedChance = 100 - trackBar1.Value;
                Properties.Settings.Default.Save();
            }
            groupBox1.Text = "Chance of Trolling Semicolons (~%)".Replace("~", trackBar1.Value.ToString());
        }

        private void SettingsDialog_Load(object sender, EventArgs e) {
            trackBar1.Value = 100 - Properties.Settings.Default.RandomizedChance;
            groupBox1.Text = "Chance of Trolling Semicolons (~%)".Replace("~", trackBar1.Value.ToString());
            checkBox1.Checked = Properties.Settings.Default.IsRandomized;
            groupBox1.Enabled = checkBox1.Checked;
            initialized = true;
        }
    }
}
