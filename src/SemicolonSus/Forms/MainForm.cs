using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SemicolonSus {
    public partial class MainForm : Form {

        TrollObjectsHolder tholder;
        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            tholder = new TrollObjectsHolder(listView1);
            tholder.OperationCompleted += Tholder_OperationCompleted;
            tholder.ProgressDetailsUpdate += Tholder_ProgressDetailsUpdate;
            tholder.ItemsUpdated += Tholder_ItemsUpdated;
        }

        private void Tholder_ItemsUpdated(object sender, EventArgs e) {
            groupBox1.Text = tholder.Count.ToString() + " item/s.";
        }

        private void Tholder_ProgressDetailsUpdate(int current, int total, string text) {
            Invoke(new Action(() => {
                progressBar1.Maximum = total;
                progressBar1.Value = current;
                ProgressLabel.Text = "Progress: " + text;
                OutputRTB.Text += text + "\n";
            }));
        }

        private void Tholder_OperationCompleted(TrollObjectsHolder.OperationType type, bool isCancelled, Exception ex) {
            Invoke(new Action(() => {
                string output = "";
                switch (type) {
                    case TrollObjectsHolder.OperationType.Scan:
                        output = "Scan";
                        break;
                    case TrollObjectsHolder.OperationType.Trolling:
                        output = "Trolling file/s";
                        break;
                    case TrollObjectsHolder.OperationType.DeTrolling:
                        output = "De-trolling file/s";
                        break;
                }

                if (isCancelled) {
                    ProgressLabel.Text = "Progress: Cancelled!";
                    output += " was cancelled" + (ex != null ? " due to an error!\n\n" + ex.Message : "!");
                } else {
                    ProgressLabel.Text = "Progress: Cancelled!";
                    output += " was finished!";
                }
                ProgressLabel.Text = "Progress:";
                MessageBox.Show(this, output, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                progressBar1.Value = 0;
                panel1.Enabled = true;
                CancelBtn.Enabled = false;
            }));
        }

        private void AddFilesIndividuallyToolStripMenuItem_Click(object sender, EventArgs e) {
            if (!panel1.Enabled) return;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                tholder.Add(openFileDialog1.FileNames, openFileDialog1.Filter.Split('|')[1].Split(';'));
        }

        private void AddFilesFromFolderToolStripMenuItem_Click(object sender, EventArgs e) {
            if (!panel1.Enabled) return;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                tholder.Add(new string[] { folderBrowserDialog1.SelectedPath }, openFileDialog1.Filter.Split('|')[1].Split(';'));
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) {
            if (!tholder.IsRunning) Close();
        }

        private void CheckAllToolStripMenuItem_Click(object sender, EventArgs e) {
            EditCheck(0);
        }

        private void UncheckAllToolStripMenuItem_Click(object sender, EventArgs e) {
            EditCheck(1);
        }

        private void InverseCheckedItemsToolStripMenuItem_Click(object sender, EventArgs e) {
            EditCheck(2);
        }

        private void EditCheck(int checkType) {
            if (!panel1.Enabled) return;
            listView1.BeginUpdate();
            foreach (ListViewItem lvi in listView1.Items) {
                switch (checkType) {
                    case 0:
                        lvi.Checked = true;
                        break;
                    case 1:
                        lvi.Checked = false;
                        break;
                    case 2:
                        lvi.Checked = !lvi.Checked;
                        break;
                }

                listView1.EndUpdate();
            }
        }

        private void RemoveCheckedItemsToolStripMenuItem_Click(object sender, EventArgs e) {
            tholder.RemoveCheckedItems();
        }

        private void RemoveAllItemsToolStripMenuItem_Click(object sender, EventArgs e) {
            EditCheck(0);
            tholder.RemoveCheckedItems();
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e) {
            new SettingsDialog().ShowDialog(this);
        }

        private void HelpToolStripMenuItem1_Click(object sender, EventArgs e) {
            MessageBox.Show(this, Properties.Resources.help, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e) {
            new AboutForm().ShowDialog(this);
        }

        private void ScanButton_Click(object sender, EventArgs e) {
            try {
                panel1.Enabled = false;
                OutputRTB.Text = "";
                tholder.ScanIt();
                CancelBtn.Enabled = true;
            } catch (Exception ex) {
                panel1.Enabled = true;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void CancelBtn_Click(object sender, EventArgs e) {
            if (MessageBox.Show(this, "Do you want to cancel the operation?", "",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                tholder.Cancel();
            }
        }

        private void TrollButton_Click(object sender, EventArgs e) {
            try {
                panel1.Enabled = false;
                OutputRTB.Text = "";
                tholder.TrollInit(TrollObjectsHolder.TrollType.Troll);
                CancelBtn.Enabled = true;
            } catch (Exception ex) {
                panel1.Enabled = true;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void DeTrollButton_Click(object sender, EventArgs e) {
            try {
                panel1.Enabled = false;
                OutputRTB.Text = "";
                tholder.TrollInit(TrollObjectsHolder.TrollType.DeTroll);
                CancelBtn.Enabled = true;
            } catch (Exception ex) {
                panel1.Enabled = true;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void ListView1_DragEnter(object sender, DragEventArgs e) {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void ListView1_DragDrop(object sender, DragEventArgs e) {
            string[] objects = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (objects != null && objects.Length > 0)
                tholder.Add(objects, openFileDialog1.Filter.Split('|')[1].Split(';'));
        }

        private void OpenSelectedItemToolStripMenuItem_Click(object sender, EventArgs e) {
            if (listView1.SelectedItems.Count == 1) {
                try {
                    Process.Start(listView1.SelectedItems[0].SubItems[3].Text);
                } catch (Exception ex) {
                    MessageBox.Show(this, "Can't open the file.\n\nError: " 
                                    + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void OpenSelectedItemsFolderToolStripMenuItem_Click(object sender, EventArgs e) {
            if (listView1.SelectedItems.Count == 1) {
                try {
                    Process.Start("explorer.exe", "/select,\"" 
                                    + listView1.SelectedItems[0].SubItems[3].Text + "\"");
                } catch (Exception ex) {
                    MessageBox.Show(this, "Can't open the file.\n\nError: " 
                                    + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }
    }
}
