using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.IO;

namespace MyUninstaller7 {
    struct Record {
        public string title;
        public DateTime dateTime;
        public Color color;
        public List<string> newItems, deletedItems;
        // Returns list of entries with uninstallation information in this record
        public List<string> UninstallEntries() {
            List<string> regEntries = new List<string>();
            const string RegUninPrefix = "HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\";
            foreach (string path in newItems)
                if (path.StartsWith(RegUninPrefix)) regEntries.Add(path);
            return regEntries;
        }
        public void SaveTo(TextWriter outStream) {
            outStream.WriteLine("Title: " + title);
            outStream.WriteLine("Datetime: " + dateTime.ToString("yyyy MMM dd HH:mm:ss.ffff"));
            outStream.WriteLine("Color: #" + ColorTranslator.ToHtml(color));
            outStream.WriteLine("\nInstallation data follows -");
            foreach (string item in newItems)
                outStream.WriteLine("A\t" + item);
            foreach (string item in deletedItems)
                outStream.WriteLine("D\t" + item);
        }
    }
    class RecordStore {
        struct RecordInfo {
            public Record record;
            public string fileName;
        }
        private List<RecordInfo> recordInfos = new List<RecordInfo>();
        public static Record CreateRecord(List<string> newItems, List<string> deletedItems) {
            Record record = new Record();
            record.dateTime = DateTime.Now;
            record.newItems = new List<string>(newItems);
            record.deletedItems = new List<string>(deletedItems);
            record.color = Color.FromArgb(196, 255, 196);
            List<string> uninsts = record.UninstallEntries();
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
        private string GetFreeFileName() {
            string dir = Utils.utils.ExeFolder();
            string[] files = Directory.GetFiles(dir);
            string name;
            for (int i = 1; ; ++i) {
                name = "InstallationRecord" + i + ".rec";
                if (!files.Contains(name)) break;
            }
            return dir + name;
        }
        public void AddRecord(List<string> newItems, List<string> deletedItems) {
            RecordInfo recordInfo = new RecordInfo();
            recordInfo.record = CreateRecord(newItems, deletedItems);
            recordInfo.fileName = GetFreeFileName();
            using (StreamWriter sw = new StreamWriter(recordInfo.fileName)) {
                recordInfo.record.SaveTo(sw);
            }
            recordInfos.Add(recordInfo);
        }
    }
}
