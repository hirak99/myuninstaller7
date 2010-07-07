using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Permissions;
using Microsoft.Win32;
using System.Security;
using System.IO.Compression;

namespace MyUninstaller7 {
    class Uninstaller {
        class Catalog {
            // The value can be 0, 1, or 2 - meaning ignore, recurse, and don't recurse
            public SortedList<string, int> entries;
            public bool shouldIgnore(string path) {
                for (int i = 0; i < entries.Count; ++i) {
                    if (entries.Values[i] != 0) continue;
                    if (path.ToLower().StartsWith(entries.Keys[i].ToLower())) return true;
                }
                return false;
            }
            public void Update() {
                List<string> lines = new List<string>();
                string catalog = Utils.utils.ExeFolder() + @"defcatalog.txt";
                using (StreamReader sr = new StreamReader(catalog)) {
                    entries = new SortedList<string, int>();
                    while (!sr.EndOfStream) {
                        string line = sr.ReadLine();
                        if (line.Length == 0 || line[0] == '#') continue;
                        string[] words = line.Split('\t');
                        words[1] = Utils.utils.pathSlash(words[1]);
                        entries.Add(words[1],int.Parse(words[0]));
                    }
                }
            }
            public Catalog() { Update(); }
        }
        private Catalog catalog;
        private void StoreRegistry(RegistryKey master, TextWriter writer, string path, bool recurse, string fullpath) {
            RegistryKey rk;
            try {
                if (master == null) rk = Utils.utils.openRegKey(path);
                else rk = master.OpenSubKey(path);
            } catch (SecurityException) {
                return;
            }
            if (rk == null) return;
            if (catalog.shouldIgnore(path)) return;
            writer.WriteLine(fullpath);
            string[] subkeys = rk.GetSubKeyNames();
            Array.Sort(subkeys);
            foreach (string subkey in subkeys) {
                string newfullpath = fullpath + subkey + @"\";
                if (!recurse) writer.WriteLine(newfullpath);
                else StoreRegistry(rk, writer, subkey, recurse, newfullpath);
            }
            rk.Close();
        }
        private void StoreRegistry(TextWriter writer, string path, bool recurse) {
            StoreRegistry(null, writer, path, recurse, path);
        }
        //[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        private void StoreFolder(TextWriter writer, string path, bool recurse) {
            if (catalog.shouldIgnore(path)) return;
            writer.WriteLine(path);
            string[] files = Directory.GetFiles(path);
            Array.Sort(files);
            foreach (string file in files) writer.WriteLine(file);
            string[] dirs = Directory.GetDirectories(path);
            Array.Sort(dirs);
            foreach(string dir_ in dirs) {
                string dir = Utils.utils.pathSlash(dir_);
                if (!recurse) writer.WriteLine(dir);
                else StoreFolder(writer, dir, recurse);
            }
        }
        public void SaveState(string outFile) {
            using (GZipWriter gzs = new GZipWriter(outFile)) {
                TextWriter sw = gzs.Writer;
                catalog = new Catalog();
                for (int i = 0; i < catalog.entries.Count; ++i) {
                    if (catalog.entries.Values[i] == 0) continue;
                    if (Utils.utils.isRegistry(catalog.entries.Keys[i]))
                        StoreRegistry(sw, catalog.entries.Keys[i], catalog.entries.Values[i] == 1);
                    else
                        StoreFolder(sw, catalog.entries.Keys[i], catalog.entries.Values[i] == 1);
                }
            }
        }
    }
}
