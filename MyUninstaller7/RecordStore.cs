using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;

namespace MyUninstaller7 {
    public class RecordStore {
        public class Record {
            // fileName is probably the only item not loaded from file
            public string fileName;

            public string DisplayName;
            public DateTime dateTime;
            // Default value of color is set in RecordStore.CreateRecord
            public Color? color;
            public List<string> newItems, deletedItems;
            // Returns list of entries with uninstallation information in this record
            public List<string> UninstallEntries() {
                List<string> regEntries = new List<string>();
                string[] RegUninRegEx = new string[]{
                @"^HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\.*$",
                @"^HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\.*$",
                @"^HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\[^\\]*\\Products\\[^\\]*\\$",
            };
                // If more locations are added, they must be handled in the foreach below manually. The following check serves as a reminder.
                Debug.Assert(RegUninRegEx.Length == 3);
                foreach (string path in newItems) {
                    if (Regex.Match(path, RegUninRegEx[0], RegexOptions.Multiline | RegexOptions.IgnoreCase).Success) {
                        regEntries.Add(path);
                    }
                    else if (Regex.Match(path, RegUninRegEx[1], RegexOptions.Multiline | RegexOptions.IgnoreCase).Success) {
                        regEntries.Add(path);
                    }
                    else if (Regex.Match(path, RegUninRegEx[2], RegexOptions.Multiline | RegexOptions.IgnoreCase).Success) {
                        regEntries.Add(path + "InstallProperties\\");
                    }
                }
                return regEntries;
            }
            public List<string> UninstallValuesOf(string RegValueName) {
                List<string> uninsts = UninstallEntries();
                List<string> values = new List<string>();
                foreach (string entry in uninsts) {
                    RegistryKey rk = Utils.utils.OpenRegKey(entry);
                    if (rk == null) continue;
                    string value = (string)rk.GetValue(RegValueName);
                    if (value != null) values.Add(value);
                    rk.Close();
                }
                return values;
            }
            public string SuggestDisplayName() {
                List<string> names = UninstallValuesOf("DisplayName");
                if (names.Count > 0) return names.Aggregate((a, b) => a + ", " + b);
                else return "(DisplayName not detected)";
            }
            private static string afterSpace(string line) {
                return line.Substring(line.IndexOf(' ') + 1);
            }
            public static Record LoadFrom(TextReader inStream) {
                Record rec = new Record();
                rec.DisplayName = afterSpace(inStream.ReadLine());
                string datetime = afterSpace(inStream.ReadLine());
                rec.dateTime = DateTime.Parse(datetime);
                string colorStr = afterSpace(inStream.ReadLine());
                if (colorStr.Trim().Length == 0) rec.color = null;
                else rec.color = ColorTranslator.FromHtml(colorStr);
                while (true) {
                    string line = inStream.ReadLine();
                    if (line == null) return null;
                    if (inStream.ReadLine() == strLineBeforeData) break;
                }
                rec.newItems = new List<string>();
                rec.deletedItems = new List<string>();
                while (true) {
                    string line = inStream.ReadLine();
                    if (line == null) break;
                    if (line[0] == 'A') rec.newItems.Add(line.Substring(2));
                    else if (line[0] == 'D') rec.deletedItems.Add(line.Substring(2));
                    else return null;
                }
                return rec;
            }
            private static string strLineBeforeData = "Installation data follows -";
            private void SaveTo(TextWriter outStream) {
                outStream.WriteLine("Title: " + DisplayName);
                outStream.WriteLine("Datetime: " + dateTime.ToString("yyyy MMM dd hh:mm:ss.ffff tt"));
                outStream.WriteLine("Color: " + (color.HasValue?ColorTranslator.ToHtml(color.Value):""));
                outStream.WriteLine("\n" + strLineBeforeData);
                foreach (string item in newItems)
                    outStream.WriteLine("A\t" + item);
                foreach (string item in deletedItems)
                    outStream.WriteLine("D\t" + item);
            }
            public void SaveToFile() {
                using (StreamWriter sw = new StreamWriter(fileName)) {
                    SaveTo(sw);
                }
            }
        }

        public List<Record> records;

        private string parentDir;
        public RecordStore(string ParentFolder) {
            parentDir = Utils.utils.PathSlash(ParentFolder);
            LoadAllRecords();
        }
        private void LoadAllRecords() {
            records = new List<Record>();
            string[] files = Directory.GetFiles(parentDir, "*.rec");
            foreach (string file in files) {
                using (StreamReader sr = new StreamReader(file)) {
                    Record rec = Record.LoadFrom(sr);
                    if (rec == null) break;
                    rec.fileName = file;
                    records.Add(rec);
                }
            }
            records.Sort((a, b) => -a.dateTime.CompareTo(b.dateTime));
        }
        public static Record CreateRecord(List<string> newItems, List<string> deletedItems) {
            Record record = new Record();
            record.dateTime = DateTime.Now;
            record.newItems = new List<string>(newItems);
            record.deletedItems = new List<string>(deletedItems);
            record.color = null; // Color.FromArgb(196, 255, 196);
            record.DisplayName = record.SuggestDisplayName();
            return record;
        }
        private static string MakeValidFileName(string FileName) {
            string s = "["+Regex.Escape(new string(Path.GetInvalidFileNameChars()))+"]";
            return Regex.Replace(FileName, s, "_");
        }
        private string GetFreeFileName(string wantedName) {
            string namePrefix = MakeValidFileName(wantedName);
            string name;
            for (int i = 0; ; ++i) {
                name = parentDir + namePrefix + (i == 0 ? "" : "_" + i.ToString()) + ".rec";
                if (!File.Exists(name)) break;
            }
            return name;
        }
        public Record AddRecord(List<string> newItems, List<string> deletedItems) {
            Record record = CreateRecord(newItems, deletedItems);
            record.fileName = GetFreeFileName(record.DisplayName);
            records.Insert(0, record);
            try {
                record.SaveToFile();
            } catch (Exception ex) {
                string original = record.fileName;
                record.fileName = GetFreeFileName("_failsafe");
                MessageBox.Show("Error while writing to '" + original + "':\n" + ex.Message + "\n\nPress OK to attempt to recover and write to '" + record.fileName + "'.",
                    "Uninstaller 7",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                record.SaveToFile();
                MessageBox.Show("Write successful! Please note the name of the original file attempted to be written to - '" + original + "'.",
                    "Uninstaller 7",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            return record;
        }
    }
}
