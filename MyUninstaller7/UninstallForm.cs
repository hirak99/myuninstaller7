using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace MyUninstaller7 {
    public partial class UninstallForm : Form {
        private class Record {
            public string Path;
            public bool StillExists;
            // 0 - RegKey, 1 - Folder, 2 - File
            public int Type;
            public bool Checked = false;
            public Record(string path) {
                Path = path;
                if (Utils.utils.IsRegistry(path)) Type = 0;
                else if (path[path.Length - 1] == '\\') Type = 1;
                else Type = 2;
            }
        }
        private List<Record> records = new List<Record>();
        private bool _ShowingInstalled;

        private List<string> UninstallEntries;
        public UninstallForm(RecordStore.Record rec, bool Installed) {
            InitializeComponent();
            _ShowingInstalled = Installed;
            Text = (Installed ? "Installed items" : "Deleted items");
            label1.Text = (_ShowingInstalled ? "Installation" : "Deletion") + " log for '" + rec.DisplayName + "':";
            if (!Installed) {
                for (int i=1; i<toolStrip1.Items.Count; ++i)
                    toolStrip1.Items[i].Visible = false;
                listView1.CheckBoxes = false;
                button2.Visible = false;
                button3.Visible = false;
            }
            if (Installed) UninstallEntries = rec.UninstallEntries();
            for (int i = 0; i < 3; ++i)
                imageList1.Images.Add(Utils.utils.FadeImage((Image)imageList1.Images[i].Clone()));
            foreach (string s in (Installed ? rec.newItems : rec.deletedItems))
                records.Add(new Record(s));
            PopulateItems();
        }

        // This scans for existance, sorts, and populates the list view
        private void PopulateItems() {
            foreach (Record rec in records)
                rec.StillExists = Utils.utils.Exists(rec.Path);
            records.Sort((a, b) => {
                if (a.StillExists && !b.StillExists) return -1;
                else if (!a.StillExists && b.StillExists) return 1;
                else if (a.Type == b.Type) return a.Path.CompareTo(b.Path);
                else return a.Type.CompareTo(b.Type);
            });
            listView1.Items.Clear();
            foreach (Record rec in records) {
                int imageIndex = rec.Type;
                if (!rec.StillExists) imageIndex += 3;
                // Adding it triggers the Checked
                bool isChecked = rec.Checked;
                ListViewItem listItem = listView1.Items.Add(rec.Path, imageIndex);
                if (!rec.StillExists) isChecked = false;
                listItem.Checked = isChecked;
                // The above line should automatically set rec.Checked
                Debug.Assert(rec.Checked == isChecked);
                if (!rec.StillExists)
                    listItem.ForeColor = Color.LightGray;
                if (rec.Type == 0) listItem.BackColor = Color.GhostWhite;
                else if (rec.Type == 1) listItem.BackColor = Color.LightYellow;
                else listItem.BackColor = Color.WhiteSmoke;
            }
            columnHeader1.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e) {
            if (!records[e.Item.Index].StillExists) e.Item.Checked = false;
            else records[e.Item.Index].Checked = e.Item.Checked;
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            PopulateItems();
        }

        private void markUnmark_Click(object sender, EventArgs e) {
            // Mark/unmark all
            bool selectedOnly = sender.Equals(button3);
            bool anyChecked = false;
            for (int i = 0; i < records.Count; ++i) {
                if (!selectedOnly || listView1.Items[i].Selected) {
                    if (records[i].Checked && records[i].StillExists) {
                        anyChecked = true;
                        break;
                    }
                }
            }
            bool desiredState = !anyChecked;
            for (int i=0; i<records.Count; ++i) {
                Record rec=records[i];
                if ((!selectedOnly || listView1.Items[i].Selected) && rec.StillExists) {
                    rec.Checked = desiredState;
                    listView1.Items[i].Checked = desiredState;
                }
            }
        }
    }
}
