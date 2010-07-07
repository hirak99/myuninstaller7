using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.IO.Compression;

namespace MyUninstaller7 {
    class Utils {
        private Utils() { }
        private string appFolder=null;
        public string ExeFolder() {
            if (appFolder == null) {
                appFolder = Application.ExecutablePath;
                appFolder = appFolder.Substring(0, appFolder.LastIndexOf('\\') + 1);
            }
            return appFolder;
        }
        public string pathSlash(string path) {
            path = path.Trim();
            if (path[path.Length - 1] != '\\') path = path + @"\";
            return path;
        }
        private RegistryKey regRoot(string first4) {
            if (first4 == "HKLM") return Registry.LocalMachine;
            else if (first4 == "HKCR") return Registry.ClassesRoot;
            else if (first4 == "HKCU") return Registry.CurrentUser;
            else return null;
        }
        public bool isRegistry(string path) {
            if (path.Length < 4) return false;
            string first4 = path.Substring(0, 4);
            RegistryKey master = regRoot(first4);
            return master != null;
        }
        public RegistryKey openRegKey(string path) {
            if (path.Length < 4) return null;
            string first4 = path.Substring(0, 4).ToUpper();
            RegistryKey master = regRoot(first4);
            path = path.Substring(5);
            return master.OpenSubKey(path);
        }
        public static Utils utils = new Utils();
    }
    class GZipWriter : IDisposable {
        private GZipStream gzs;
        private TextWriter sw;
        public GZipWriter(string outFile) {
            gzs = new GZipStream(File.OpenWrite(outFile), CompressionMode.Compress);
            sw = new StreamWriter(gzs);
        }
        public TextWriter Writer {
            get { return sw; }
        }
        public void Dispose() {
            sw.Close();
            gzs.Close();
        }
    }
}
