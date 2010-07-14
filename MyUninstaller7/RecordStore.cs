﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace MyUninstaller7 {
    public class RecordStore {
        public class Record {
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
            public void SaveTo(TextWriter outStream) {
                outStream.WriteLine("Title: " + DisplayName);
                outStream.WriteLine("Datetime: " + dateTime.ToString("yyyy MMM dd hh:mm:ss.ffff tt"));
                outStream.WriteLine("Color: " + (color.HasValue?ColorTranslator.ToHtml(color.Value):""));
                outStream.WriteLine("\n" + strLineBeforeData);
                foreach (string item in newItems)
                    outStream.WriteLine("A\t" + item);
                foreach (string item in deletedItems)
                    outStream.WriteLine("D\t" + item);
            }
        }

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
            parentDir = Utils.utils.PathSlash(ParentFolder);
            LoadAllRecords();
        }
        private void LoadAllRecords() {
            recordInfos = new List<RecordInfo>();
            string[] files = Directory.GetFiles(parentDir, "*.rec");
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
            record.color = null; // Color.FromArgb(196, 255, 196);
            record.DisplayName = record.SuggestDisplayName();
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
