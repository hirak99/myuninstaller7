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
            string recordStoreDir = Utils.utils.ExeFolder() + "Records";
            if (!Directory.Exists(recordStoreDir)) {
                Directory.CreateDirectory(recordStoreDir);
            }
            recordStore = new RecordStore(recordStoreDir);
            RefreshView();
            SetState(0);
        }

        private string stateFile1 = Utils.utils.ExeFolder() + @"state1.txt.gz";
        private string stateFile2 = Utils.utils.ExeFolder() + @"state2.txt.gz";

        private RecordStore recordStore;

        /***
         * State = 0 means ready to note changes
         *       = 1 means already noting changes
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

        private void RefreshView() {
            flexiListBox1.Items.Clear();
            foreach (RecordStore.RecordInfo ri in recordStore.recordInfos) {
                flexiListBox1.Items.Add(ColoredListBox.CreateItem(ri.record.color, ri.record.DisplayName));
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            if (SaveStateForm.SaveStateWithProgress(Utils.utils.ExeFolder() + @"state1.txt.gz"))
                SetState(1);
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
                    RefreshView();
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
            
        }
    }
}
