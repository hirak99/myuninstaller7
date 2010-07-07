﻿using System;
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
        // Changes things like %APPDATA% to what it is currently set to
        public string ResolveEnvironment(string path) {
            int lastPerc = -1, curPos = 0;
            while (true) {
                int perc = path.IndexOf('%', curPos);
                if (perc == -1) return path;
                curPos = perc;
                if (lastPerc == -1) lastPerc = perc;
                else {
                    string envVar = path.Substring(lastPerc + 1, perc - lastPerc - 1);
                    string result = System.Environment.GetEnvironmentVariable(envVar);
                    if (result != null) {
                        path = path.Substring(0, lastPerc) + result + path.Substring(perc + 1);
                        perc = perc - (envVar.Length + 2) + result.Length;
                    }
                    lastPerc = -1;
                }
                curPos = perc + 1;
            }
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
