using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Permissions;
using Microsoft.Win32;

namespace MyUninstaller7 {
    class Uninstaller {
        class Catalog {
            public List<string> entries;
            public List<int> actionCodes;
            public bool shouldIgnore(string path) {
                for (int i = 0; i < actionCodes.Count; ++i) {
                    if (actionCodes[i] != 0) continue;
                    if (path.ToLower().StartsWith(entries[i].ToLower())) return true;
                }
                return false;
            }
            public void Update() {
                entries = new List<string>();
                actionCodes = new List<int>();
                string catalog = Utils.utils.ExeFolder() + @"defcatalog.txt";
                using (StreamReader sr = new StreamReader(catalog)) {
                    while (!sr.EndOfStream) {
                        string line = sr.ReadLine();
                        if (line.Length == 0 || line[0] == '#') continue;
                        string[] words = line.Split('\t');
                        words[1] = Utils.utils.pathSlash(words[1]);
                        actionCodes.Add(int.Parse(words[0]));
                        entries.Add(words[1]);
                    }
                }
            }
            public Catalog() { Update(); }
        }
        private Catalog catalog;
        private void StoreRegistry(StreamWriter writer, string path, bool recurse) {
            RegistryKey rk = Utils.utils.openRegKey(path);
        }
        //[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        private void StoreFolder(StreamWriter writer, string path, bool recurse) {
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
            StreamWriter sw = new StreamWriter(outFile);
            catalog = new Catalog();
            for (int i = 0; i < catalog.entries.Count; ++i) {
                if (catalog.actionCodes[i] == 0) continue;
                if (Utils.utils.isRegistry(catalog.entries[i]))
                    StoreRegistry(sw, catalog.entries[i], catalog.actionCodes[i] == 1);
                else
                    StoreFolder(sw, catalog.entries[i], catalog.actionCodes[i] == 1);
            }
            sw.Close();
        }
    }
}
