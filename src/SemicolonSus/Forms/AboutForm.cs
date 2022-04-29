using System.Windows.Forms;
using System.Diagnostics;
namespace SemicolonSus {
    public partial class AboutForm : Form {
        public AboutForm() {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            try {
                string repoLink = "https://github.com/PheeLeep/SemicolonSus";
                Process.Start(repoLink);
            } catch {
                // Ignore this error.
            }
        }

        private void LicenseLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            try {
                string repoLink = "https://github.com/PheeLeep/SemicolonSus/blob/master/LICENSE";
                Process.Start(repoLink);
            } catch {
                // Ignore this error.
            }
        }
    }
}
