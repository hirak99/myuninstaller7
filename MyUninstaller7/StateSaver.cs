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
using System.Diagnostics;

namespace MyUninstaller7 {
    class StateSaver {
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
            public void Load() {
                List<string> lines = new List<string>();
                string catalog = Utils.utils.ExeFolder() + @"defcatalog.txt";
                using (StreamReader sr = new StreamReader(catalog)) {
                    entries = new SortedList<string, int>();
                    while (!sr.EndOfStream) {
                        string line = sr.ReadLine();
                        if (line.Length == 0 || line[0] == '#') continue;
                        string[] words = line.Split('\t');
                        words[1] = Utils.utils.ResolveEnvironment(Utils.utils.pathSlash(words[1]));
                        entries.Add(words[1],int.Parse(words[0]));
                    }
                }
            }
            public Catalog() { Load(); }
        }
        private Catalog catalog;

        private TextWriter writer;
        private void StoreItem(string item) {
            writer.WriteLine(item);
        }

        private int nDepth = 0;
        private void StoreRegistry(RegistryKey master, string path, bool recurse, string fullpath) {
            if (IsCancelled()) return;
            RegistryKey rk;
            try {
                if (master == null) rk = Utils.utils.openRegKey(path);
                else rk = master.OpenSubKey(path);
            } catch (SecurityException) {
                return;
            }
            if (rk == null) return;
            if (catalog.shouldIgnore(fullpath)) return;
            if (nDepth++ <= 1) ReportProgress(fullpath);
            StoreItem(fullpath);
            try {
                string[] subkeys = rk.GetSubKeyNames();
                Array.Sort(subkeys);
                foreach (string subkey in subkeys) {
                    string newfullpath = fullpath + subkey + @"\";
                    if (!recurse) StoreItem(newfullpath);
                    else StoreRegistry(rk, subkey, recurse, newfullpath);
                }
            } catch (UnauthorizedAccessException) { }
            rk.Close();
            nDepth--;
        }
        private void StoreRegistry(string path, bool recurse) {
            StoreRegistry(null, path, recurse, path);
        }
        //[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        private void StoreFolder(string path, bool recurse) {
            if (IsCancelled()) return;
            if (catalog.shouldIgnore(path)) return;
            string[] files;
            try {
                files = Directory.GetFiles(path);
            } catch (DirectoryNotFoundException) {
                return;
            } catch (UnauthorizedAccessException) {
                return;
            }
            if (nDepth++ <= 1) ReportProgress(path);
            StoreItem(path);
            Array.Sort(files);
            foreach (string file in files) StoreItem(file);
            string[] dirs = Directory.GetDirectories(path);
            Array.Sort(dirs);
            foreach(string dir_ in dirs) {
                string dir = Utils.utils.pathSlash(dir_);
                if (!recurse) StoreItem(dir);
                else StoreFolder(dir, recurse);
            }
            nDepth--;
        }
        private string outFile;
        public StateSaver(string outFile_) {
            outFile = outFile_;
        }

        #region Progress Reporter
        // IsCancelled connects to the BackgroundWorker's cancelled property. If it is true, the operation should be cancelled asap.
        private Func<bool> IsCancelled;
        // This connects to BackgroundWorker's ReportProgress. Use this to update the UI.
        private void ReportProgress(string path) {
            status.path = path;
            reporter(0, status);
        }
        public struct Status {
            public string path;
            public int current, total;
        }
        private Status status;
        private Action<int, object> reporter;
        #endregion

        /*****
         * This is the main function. This takes the ReportProgress function as an argument which comes from the BackgroundWorker master thread,
         *   which will be used to pass intermediate results. Note the very Action<T> class here, which is same as Func<T,TResult> without TResult,
         *   and can be used to pass void functions without having to write a custom delegate.
         * Note: This should not be directly called. Instead use the SaveStateWithProgress from SaveStateForm
         *****/
        public bool SaveState(Action<int,object> reporter_, Func<bool> cancelled_) {
            reporter = reporter_;
            IsCancelled = cancelled_;
            using (GZipWriter gzs = new GZipWriter(outFile)) {
                writer = gzs.Writer;
                catalog = new Catalog();
                status.total = catalog.entries.Count(a => a.Value != 0);
                status.current = 0;
                for (int i = 0; i < catalog.entries.Count; ++i) {
                    if (catalog.entries.Values[i] == 0) continue;
                    status.current++;
                    if (Utils.utils.isRegistry(catalog.entries.Keys[i]))
                        StoreRegistry(catalog.entries.Keys[i], catalog.entries.Values[i] == 1);
                    else
                        StoreFolder(catalog.entries.Keys[i], catalog.entries.Values[i] == 1);
                    if (IsCancelled()) return false;
                    Debug.Assert(IsCancelled() || nDepth == 0);
                }
            }
            return !IsCancelled();
        }
    }
}
