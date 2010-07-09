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
        RecordStore.Record record;
        public UninstallForm(RecordStore.Record _record) {
            record = _record;
            InitializeComponent();
            PopulateItems();
        }

        private void PopulateItems(ListView list, List<string> items) {
            foreach (string s in items) {
                list.Items.Add(s, 0);
                list.Items[0].BackColor = Color.AliceBlue;
            }
        }

        private void PopulateItems() {
            PopulateItems(listView1, record.newItems);
            //PopulateItems(checkedListBox2, record.deletedItems);
        }
    }
}
