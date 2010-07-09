using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.IO.Compression;
using System.Security.Principal;
using System.Drawing;

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

        /****
         * The method below was recreated unnecessarily, as it is exactly equivalent
         *   to Environment.ExpandEnvironmentVariables. I feel relactant to delete it
         *   so I will comment it out for now. This may come handy later, if I want
         *   to do more fancy stuff with the paths.
         ****/
        /*
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
        }*/

        public bool IsUserAdministrator() {
            //bool value to hold our return value
            bool isAdmin;
            try {
                //get the currently logged in user
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            } catch (UnauthorizedAccessException ex) {
                isAdmin = false;
                MessageBox.Show(ex.Message);
            } catch (Exception ex) {
                isAdmin = false;
                MessageBox.Show(ex.Message);
            }
            return isAdmin;
        }

        public DialogResult InputBox(string title, string promptText, ref string value) {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }
        
        public static Utils utils = new Utils();
    }

    class GZipWriter : IDisposable {
        private GZipStream gzs;
        private TextWriter sw;
        public GZipWriter(string outFile) {
            gzs = new GZipStream(File.Create(outFile), CompressionMode.Compress);
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
    class GZipReader : IDisposable {
        private GZipStream gzs;
        private TextReader sr;
        public GZipReader(string inFile) {
            gzs = new GZipStream(File.OpenRead(inFile), CompressionMode.Decompress);
            sr = new StreamReader(gzs);
        }
        public TextReader Reader {
            get { return sr; }
        }
        public void Dispose() {
            sr.Close();
            gzs.Close();
        }
    }
}
