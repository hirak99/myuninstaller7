using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.IO;
using System.Globalization;

namespace MyUninstaller7 {
    class Record {
        public string DisplayName;
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
        private static string afterSpace(string line) {
            return line.Substring(line.IndexOf(' ')+1);
        }
        public static Record LoadFrom(TextReader inStream) {
            Record rec = new Record();
            rec.DisplayName = afterSpace(inStream.ReadLine());
            string datetime = afterSpace(inStream.ReadLine());
            rec.dateTime = DateTime.Parse(datetime);
            rec.color = ColorTranslator.FromHtml(afterSpace(inStream.ReadLine()));
            while (true) {
                string line = inStream.ReadLine();
                if (line==null) return null;
                if (inStream.ReadLine() == strLineBeforeData) break;
            }
            rec.newItems = new List<string>();
            rec.deletedItems = new List<string>();
            while (true) {
                string line = inStream.ReadLine();
                if (line == null) return rec;
                if (line[0] == 'A') rec.newItems.Add(line.Substring(2));
                else if (line[0] == 'D') rec.deletedItems.Add(line.Substring(2));
                else return null;
            }
        }
        private static string strLineBeforeData = "Installation data follows -";
        public void SaveTo(TextWriter outStream) {
            outStream.WriteLine("Title: " + DisplayName);
            outStream.WriteLine("Datetime: " + dateTime.ToString("yyyy MMM dd hh:mm:ss.ffff tt"));
            outStream.WriteLine("Color: " + ColorTranslator.ToHtml(color));
            outStream.WriteLine("\n" + strLineBeforeData);
            foreach (string item in newItems)
                outStream.WriteLine("A\t" + item);
            foreach (string item in deletedItems)
                outStream.WriteLine("D\t" + item);
        }
    }

    class RecordStore {
        public class RecordInfo {
            public Record record;
            public string fileName;
            public void SaveToFile() {
                using (StreamWriter sw = new StreamWriter(fileName)) {
                    record.SaveTo(sw);
                }
            }
        }
        public List<RecordInfo> recordInfos;
        private string parentDir;
        public RecordStore(string ParentFolder) {
            parentDir = Utils.utils.pathSlash(ParentFolder);
            LoadAllRecords();
        }
        private void LoadAllRecords() {
            recordInfos = new List<RecordInfo>();
            string[] files = Directory.GetFiles(parentDir, "InstallationRecord*.rec");
            foreach (string file in files) {
                using (StreamReader sr = new StreamReader(file)) {
                    Record rec = Record.LoadFrom(sr);
                    if (rec == null) break;
                    RecordInfo ri = new RecordInfo();
                    ri.fileName = file;
                    ri.record = rec;
                    recordInfos.Add(ri);
                }
            }
            recordInfos.Sort((a, b) => -a.record.dateTime.CompareTo(b.record.dateTime));
        }
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
            if (names.Count > 0) record.DisplayName = names.Aggregate((a, b) => a + ", " + b);
            else record.DisplayName = "(DisplayName not detected)";
            return record;
        }
        private string GetFreeFileName() {
            string name;
            for (int i = 1; ; ++i) {
                name = parentDir + "InstallationRecord" + i + ".rec";
                if (!File.Exists(name)) break;
            }
            return name;
        }
        public RecordInfo AddRecord(List<string> newItems, List<string> deletedItems) {
            RecordInfo recordInfo = new RecordInfo();
            recordInfo.record = CreateRecord(newItems, deletedItems);
            recordInfo.fileName = GetFreeFileName();
            recordInfos.Insert(0, recordInfo);
            recordInfo.SaveToFile();
            return recordInfo;
        }
    }
}
