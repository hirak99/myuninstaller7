using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Principal;
using System.IO;
using System.Diagnostics;

namespace MyUninstaller7 {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            EnsureCatalog();
            if (!Utils.utils.IsUserAdministrator()) {
                MessageBox.Show("It is preferred that you run this program as Admin. " +
                    "Please restart with Admin mode if you have the privelege. Not all changes will be detected otherwise.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (!Directory.Exists(recordStoreDir)) {
                Directory.CreateDirectory(recordStoreDir);
            }
            listView1_Resize(this, null);
            RefreshList();
            SetState(0);
        }

        private string stateFile1 = Utils.utils.ExeFolder() + @"state1.txt.gz";
        private string stateFile2 = Utils.utils.ExeFolder() + @"state2.txt.gz";
        private string recordStoreDir = Utils.utils.ExeFolder() + "Records";

        private RecordStore recordStore;

        /***
         * State = 0 means ready to note changes
         *       = 1 means started to note changes
         ***/
        private int State = 0;
        protected void SetState(int newState) {
            State = newState;
            toolStripButton1.Enabled = (State == 0);
            startNotingChangesToolStripMenuItem.Enabled = (State == 0);
            toolStripButton2.Enabled = (State == 1);
            endNotingChangesToolStripMenuItem.Enabled = (State == 1);
            toolStripButton3.Enabled = (State == 1);
            cancelNotingChangesToolStripMenuItem.Enabled = (State == 1);
            if (State == 0) toolStripStatusLabel1.Text = "Ready.";
            else toolStripStatusLabel1.Text = "Noting changes.";
        }

        // Creates the catalog if not already present
        private void EnsureCatalog() {
            string catfile = Utils.utils.ExeFolder()+"defcatalog.txt";
            if (File.Exists(catfile)) return;
            using (StreamWriter sw = new StreamWriter(catfile))
                sw.Write(MyUninstaller7.Properties.Resources.defcatalog);
            MessageBox.Show("The catalog file 'defcatalog.txt' was automatically created in" +
                "application folder. You can" +
                " edit this file to adjust which locations will be monitored during installation of" +
                " appliations.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshList() {
            recordStore = new RecordStore(recordStoreDir);
            listView1.Items.Clear();
            foreach (RecordStore.RecordInfo ri in recordStore.recordInfos) {
                listView1.Items.Add(ri.record.DisplayName);
                listView1.Items[listView1.Items.Count - 1].BackColor = ri.record.color;
            }
            if (listView1.Items.Count > 0) listView1.Items[0].Selected = true;
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            if (SaveStateForm.SaveStateWithProgress(Utils.utils.ExeFolder() + @"state1.txt.gz"))
                SetState(1);
            else toolStripStatusLabel1.Text = "Cancelled noting changes.";
        }

        private void toolStripButton2_Click(object sender, EventArgs e) {
            if (SaveStateForm.SaveStateWithProgress(stateFile2)) {
                SetState(0);
                //TODO: Comparison of the states goes here
                StateComparer sc = new StateComparer();
                sc.Compare(stateFile1, stateFile2);
                if (sc.onlyIn1.Count == 0 && sc.onlyIn2.Count == 0) {
                    MessageBox.Show("No change was detected.", "Uninstaller 7", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else {
                    RecordStore.RecordInfo ri = recordStore.AddRecord(sc.onlyIn2, sc.onlyIn1);
                    string recordName=ri.record.DisplayName;
                    if (Utils.utils.InputBox("Uninstaller 7", "Name the record :", ref recordName) == DialogResult.OK)
                        if (recordName != ri.record.DisplayName) {
                            ri.record.DisplayName = recordName;
                            ri.SaveToFile();
                        }
                    RefreshList();
                }
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e) {
            if (MessageBox.Show("Really cancel noting changes?", "Warning", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes) {
                File.Delete(stateFile1);
                SetState(0);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (State == 0) {
                try {
                    File.Delete(stateFile1);
                    File.Delete(stateFile2);
                } catch (Exception ex) {
                    MessageBox.Show("Could perform deletion of the state files. There could be a problem. Please check.\n" + ex.Message,
                        "Uninstaller 7", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void listView1_Resize(object sender, EventArgs e) {
            listView1.Columns[0].Width = listView1.ClientRectangle.Width;
        }

        //TODO: need to add view deleted items
        private void installedItemsToolStripMenuItem_Click(object sender, EventArgs e) {
            if (listView1.SelectedIndices.Count == 0) return;
            int index = listView1.SelectedIndices[0];
            UninstallForm uf = new UninstallForm(recordStore.recordInfos[index].record, sender.Equals(installedItemsToolStripMenuItem));
            uf.ShowDialog();
        }

        private void editRecordToolStripMenuItem_Click(object sender, EventArgs e) {
            if (listView1.SelectedIndices.Count == 0) return;
            int index = listView1.SelectedIndices[0];
            Process.Start("Notepad", recordStore.recordInfos[index].fileName);
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e) {
            RefreshList();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e) {
            if (listView1.SelectedIndices.Count == 0) {
                installedItemsToolStripMenuItem.Enabled = false;
                viewDeletedToolStripMenuItem.Enabled = false;
                renameToolStripMenuItem.Enabled = false;
                toolStripButton5.Enabled = false;
            }
            else {
                RecordStore.RecordInfo ri = recordStore.recordInfos[listView1.SelectedIndices[0]];
                string fileName = ri.fileName;
                fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                toolStripStatusLabel1.Text = "File " + fileName;
                installedItemsToolStripMenuItem.Enabled = (ri.record.newItems.Count > 0);
                viewDeletedToolStripMenuItem.Enabled = (ri.record.deletedItems.Count > 0);
                renameToolStripMenuItem.Enabled = true;
                toolStripButton5.Enabled = true;
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e) {
            RecordStore.RecordInfo ri = recordStore.recordInfos[listView1.SelectedIndices[0]];
            string newName = ri.record.DisplayName;
            if (Utils.utils.InputBox("Uninstaller 7", "Rename the record", ref newName) == DialogResult.OK
                && newName != ri.record.DisplayName) {
                ri.record.DisplayName = newName;
                ri.SaveToFile();
                listView1.SelectedItems[0].Text = newName;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            string message = "My Uninstaller Version " + Application.ProductVersion + "\n"
                + "Email: hirak99@gmail.com";
            MessageBox.Show(message, "My Uninstaller 7",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
