using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
        public UninstallForm(List<string> items) {
            InitializeComponent();
            foreach (string s in items)
                records.Add(new Record(s));
            ReloadItems();
        }

        // This scans for existance, sorts, and populates the list view
        private void ReloadItems() {
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
                ListViewItem listItem = listView1.Items.Add(rec.Path, 0);
                if (!rec.StillExists) rec.Checked = false;
                else listItem.Checked = rec.Checked;
                if (!rec.StillExists)
                    listItem.BackColor = Color.LightGray;
                else if (rec.Type == 0) listItem.BackColor = Color.GhostWhite;
                else if (rec.Type == 1) listItem.BackColor = Color.LightYellow;
                else listItem.BackColor = Color.WhiteSmoke;
            }
            columnHeader1.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e) {
            if (!records[e.Item.Index].StillExists) e.Item.Checked = false;
            else records[e.Item.Index].Checked = e.Item.Checked;
        }
    }
}
