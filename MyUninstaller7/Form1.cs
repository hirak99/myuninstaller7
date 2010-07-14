using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;
using System.Collections.Generic;

namespace MyUninstaller7 {
    public partial class Form1 : Form {
        private IniFile ini = new IniFile(Utils.utils.ExeFolder() + "myuninstaller.ini");
        public Form1() {
            InitializeComponent();
            EnsureCatalog();
            if (!Utils.utils.IsUserAdministrator()) {
                MessageBox.Show("It is preferred that you run this program as Admin. " +
                    "Please restart with Admin mode if you have the privelege. All changes may not be detected otherwise.",
                    "Uninstaller 7", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (!Directory.Exists(recordStoreDir)) {
                Directory.CreateDirectory(recordStoreDir);
            }
            AddToStartup(true);
            listView1_Resize(this, null);
            PopulateItems();
            SetState(ini.ReadInt("Uninstaller7", "IsNotingChanges", 0));

            /* Debugging code to test comparison speed
            MessageBox.Show("Click ok to start comparing");
            string _stateFile1 = Utils.utils.ExeFolder() + @"_state1.txt.gz";
            string _stateFile2 = Utils.utils.ExeFolder() + @"_state2.txt.gz";
            StateComparerProgress.CompareStateWithProgress(_stateFile1, _stateFile2);
            MessageBox.Show("Done comparing");*/
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
            foreach (ToolStripMenuItem button in new ToolStripMenuItem[]{
                startNotingChangesToolStripMenuItem, cancelNotingChangesToolStripMenuItem
            })
                button.Enabled = (State == 0);
            foreach (ToolStripButton button in new ToolStripButton[]{
                toolStripButton2, toolStripButton3
            })
                button.Enabled = (State == 1);
            foreach (ToolStripMenuItem button in new ToolStripMenuItem[]{
                endNotingChangesToolStripMenuItem
            })
                button.Enabled = (State == 1);
            listView1_SelectedIndexChanged(this, null);
            toolStripButton2.Enabled = (State == 1);
            endNotingChangesToolStripMenuItem.Enabled = (State == 1);
            toolStripButton3.Enabled = (State == 1);
            cancelNotingChangesToolStripMenuItem.Enabled = (State == 1);
            if (State == 0) toolStripStatusLabel1.Text = "Ready.";
            else toolStripStatusLabel1.Text = "Noting changes.";
            ini.WriteInt("Uninstaller7", "IsNotingChanges", State);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetDriveType(string lpRootPathName);
        // Creates the catalog if not already present
        private void EnsureCatalog() {
            string catfile = Utils.utils.ExeFolder()+"defcatalog.txt";
            if (File.Exists(catfile)) return;
            using (StreamWriter sw = new StreamWriter(catfile)) {
                sw.Write(MyUninstaller7.Properties.Resources.defcatalog);
                sw.WriteLine();
                foreach (string drive in Environment.GetLogicalDrives())
                    if (GetDriveType(drive) == 3) sw.WriteLine("2\t" + drive);
            }
            MessageBox.Show("The catalog file 'defcatalog.txt' was automatically created in" +
                "application folder. You can" +
                " edit this file to adjust which locations will be monitored during installation of" +
                " appliations.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PopulateItems() {
            int index;
            if (listView1.SelectedIndices.Count == 0) index = 0;
            else index = listView1.SelectedIndices[0];
            recordStore = new RecordStore(recordStoreDir);
            listView1.Items.Clear();
            foreach (RecordStore.Record rec in recordStore.records) {
                listView1.Items.Add(rec.DisplayName);
                if (rec.color.HasValue)
                    listView1.Items[listView1.Items.Count - 1].BackColor = rec.color.Value;
            }
            if (listView1.Items.Count > 0) listView1.Items[0].Selected = true;
            if (listView1.Items.Count == index) index--;
            if (index >= 0) {
                listView1.Items[index].Selected = true;
                listView1.Items[index].Focused = true;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            if (StateSaverProgress.SaveStateWithProgress(Utils.utils.ExeFolder() + @"state1.txt.gz"))
                SetState(1);
            else toolStripStatusLabel1.Text = "Cancelled noting changes.";
        }

        private void toolStripButton2_Click(object sender, EventArgs e) {
            if (StateSaverProgress.SaveStateWithProgress(stateFile2)) {
                SetState(0);
                //TODO: Comparison of the states goes here
                StateComparer sc = StateComparerProgress.CompareStateWithProgress(stateFile1, stateFile2);
                if (sc.onlyIn1.Count == 0 && sc.onlyIn2.Count == 0) {
                    MessageBox.Show("No change was detected.", "Uninstaller 7", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else {
                    RecordStore.Record record = recordStore.AddRecord(sc.onlyIn2, sc.onlyIn1);
                    string recordName=record.DisplayName;
                    if (Utils.utils.InputBox("Uninstaller 7", "Name the record :", ref recordName) == DialogResult.OK)
                        if (recordName != record.DisplayName) {
                            record.DisplayName = recordName;
                            record.SaveToFile();
                        }
                    PopulateItems();
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

        private void AddToStartup(bool add) {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)) {
                if (add)
                    rk.SetValue("MyUninstaller7", "\"" + Application.ExecutablePath + "\"");
                else {
                    try {
                        rk.DeleteValue("MyUninstaller7");
                    } catch (ArgumentException) { }
                }
            }
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
            else if (e.CloseReason == CloseReason.WindowsShutDown) {
                AddToStartup(true);
            }
            else {
                if (MessageBox.Show("Uninstaller is already noting changes - it will resume when you start next time.",
                    "Uninstaller 7",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information) == DialogResult.Cancel) e.Cancel = true;
            }
        }

        private void listView1_Resize(object sender, EventArgs e) {
            listView1.Columns[0].Width = listView1.ClientRectangle.Width;
        }

        //TODO: need to add view deleted items
        private void installedItemsToolStripMenuItem_Click(object sender, EventArgs e) {
            if (listView1.SelectedIndices.Count == 0) return;
            bool viewNewItems = sender.Equals(installedItemsToolStripMenuItem) ||
                sender.Equals(toolStripButton6);
            int index = listView1.SelectedIndices[0];
            RecordStore.Record rec = recordStore.records[index];
            List<string> items = viewNewItems ? rec.newItems : rec.deletedItems;
            if (items.Count == 0) {
                MessageBox.Show("There were no "+ (viewNewItems?"added":"deleted") + " items detected for this installation.",
                    "Uninstaller 7",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else {
                UninstallForm uf = new UninstallForm(rec, viewNewItems);
                uf.ShowDialog();
            }
        }

        private void editRecordToolStripMenuItem_Click(object sender, EventArgs e) {
            if (listView1.SelectedIndices.Count == 0) return;
            int index = listView1.SelectedIndices[0];
            Process.Start("Notepad", recordStore.records[index].fileName);
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e) {
            PopulateItems();
        }

        private void SetToolColorTo(Color color) {
            Image icon = Utils.utils.ColorIcon(color);
            toolStripButton7.Image = icon;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e) {
            bool isSelected = (listView1.SelectedIndices.Count > 0);
            toolStripButton5.Enabled = isSelected;
            toolStripButton7.Enabled = isSelected;
            toolStripButton8.Enabled = isSelected;
            renameToolStripMenuItem.Enabled = isSelected;
            selectColorToolStripMenuItem.Enabled = isSelected;
            editRecordToolStripMenuItem.Enabled = isSelected;
            deleteRecordToolStripMenuItem.Enabled = isSelected;
            if (isSelected) SetToolColorTo(listView1.SelectedItems[0].BackColor);
            else SetToolColorTo(Color.Gray);
            if (listView1.SelectedIndices.Count == 0 || State == 1) {
                installedItemsToolStripMenuItem.Enabled = false;
                viewDeletedToolStripMenuItem.Enabled = false;
                toolStripButton6.Enabled = false;
            }
            else {
                RecordStore.Record rec = recordStore.records[listView1.SelectedIndices[0]];
                string fileName = rec.fileName;
                toolStripStatusLabel1.Text = "(" + rec.newItems.Count + "/-" + rec.deletedItems.Count + ") " + rec.dateTime.ToString("dd-MMM-yyyy") + " \"" + Path.GetFileName(rec.fileName) + "\"";
                installedItemsToolStripMenuItem.Enabled = true;
                viewDeletedToolStripMenuItem.Enabled = true;
                toolStripButton6.Enabled = true;
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e) {
            RecordStore.Record rec = recordStore.records[listView1.SelectedIndices[0]];
            string newName = rec.DisplayName;
            if (Utils.utils.InputBox("Uninstaller 7", "Rename the record", ref newName) == DialogResult.OK
                && newName != rec.DisplayName) {
                rec.DisplayName = newName;
                rec.SaveToFile();
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

        private void deleteRecordToolStripMenuItem_Click(object sender, EventArgs e) {
            RecordStore.Record rec = recordStore.records[listView1.SelectedIndices[0]];
            if (MessageBox.Show("Delete record '" + rec.DisplayName + "'?",
                "Uninstaller 7",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) == DialogResult.OK) {
                    File.Delete(rec.fileName);
                    PopulateItems();
            }
        }

        private void selectColorToolStripMenuItem_Click(object sender, EventArgs e) {
            RecordStore.Record ri = recordStore.records[listView1.SelectedIndices[0]];
            Color? result = ColorChooser.Choose();
            if (result != null) {
                ri.color = (result.Value.IsSystemColor ? null : result);
                listView1.SelectedItems[0].BackColor = result.Value;
                ri.SaveToFile();
                SetToolColorTo(result.Value);
            }
        }
    }
}
