using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Win32;

namespace MyUninstaller7 {
    struct Record {
        public string title;
        public DateTime dateTime;
        public Color color;
        public List<string> newItems, deletedItems;
    }
    class RecordStore {
        public static List<string> GetRegUnin(Record record) {
            List<string> regEntries = new List<string>();
            const string RegUninPrefix = "HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\";
            foreach (string path in record.newItems)
                if (path.StartsWith(RegUninPrefix)) regEntries.Add(path);
            return regEntries;
        }
        public Record CreateRecord(List<string> newItems, List<string> deletedItems) {
            Record record = new Record();
            record.dateTime = DateTime.Now;
            record.newItems = new List<string>(newItems);
            record.deletedItems = new List<string>(deletedItems);
            record.color = Color.FromArgb(196, 255, 196);
            List<string> uninsts = GetRegUnin(record);
            List<string> names = new List<string>();
            foreach (string entry in uninsts) {
                RegistryKey rk = Utils.utils.openRegKey(entry);
                string name = (string)rk.GetValue("DisplayName");
                if (name != null) names.Add(name);
                rk.Close();
            }
            if (names.Count > 0) record.title = names.Aggregate((a, b) => a + ", " + b);
            else record.title = "(DisplayName not detected)";
            return record;
        }
    }
}
