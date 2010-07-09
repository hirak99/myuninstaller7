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
            public Record(string path) {
                Path = path;
                StillExists = Utils.utils.Exists(path);
                if (Utils.utils.IsRegistry(path)) Type = 0;
                else if (path[path.Length - 1] == '\\') Type = 1;
                else Type = 2;
            }
        }
        private List<Record> recsNew = new List<Record>();
        private List<Record> recsDeleted = new List<Record>();
        private RecordStore.RecordInfo ri;
        public UninstallForm(RecordStore.RecordInfo _ri) {
            InitializeComponent();
            ri = _ri;
            LoadItems();
            listViewResize(listView1, null);
            listViewResize(listView2, null);
        }

        private void LoadItems(List<string> strList, List<Record> list) {
            foreach (string s in strList)
                list.Add(new Record(s));
            list.Sort((a, b) => {
                if (a.StillExists && !b.StillExists) return -1;
                else if (!a.StillExists && b.StillExists) return 1;
                else if (a.Type == b.Type) return a.Path.CompareTo(b.Path);
                else return a.Type.CompareTo(b.Type);
            });
        }
        private void LoadItems() {
            LoadItems(ri.record.newItems, recsNew);
            LoadItems(ri.record.deletedItems, recsDeleted);
            PopulateListView(listView1, recsNew);
            PopulateListView(listView2, recsDeleted);
        }

        private void PopulateListView(ListView list, List<Record> items) {
            foreach (Record item in items) {
                ListViewItem listItem = list.Items.Add(item.Path, 0);
                if (!item.StillExists)
                    listItem.BackColor = Color.LightGray;
                else if (item.Type == 0) listItem.BackColor = Color.GhostWhite;
                else if (item.Type == 1) listItem.BackColor = Color.LightYellow;
                else listItem.BackColor = Color.WhiteSmoke;
            }
        }

        private void listViewResize(object sender, EventArgs e) {
            ListView lv = (ListView)sender;
            lv.Columns[0].Width = lv.ClientRectangle.Width;
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e) {
            ListView lv = (ListView)sender;
            Record rec;
            if (lv == listView1) rec = recsNew[e.Item.Index];
            else rec = recsDeleted[e.Item.Index];
            if (!rec.StillExists) e.Item.Checked = false;
        }
    }
}
