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
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace MyUninstaller7 {
    // This will not be required in dotNet 4.0
    class MyTuple<T1, T2> {
        public T1 Item1;
        public T2 Item2;
        public MyTuple(T1 _Item1, T2 _Item2) { Item1=_Item1; Item2=_Item2;}
    }
    public enum EItemType { RegistryKey, RegistryValue, Folder, File }
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
        public string PathSlash(string path) {
            path = path.Trim();
            if (path[path.Length - 1] != '\\') path = path + @"\";
            return path;
        }
        private RegistryKey RegRoot(string first4) {
            if (first4 == "HKLM") return Registry.LocalMachine;
            else if (first4 == "HKCR") return Registry.ClassesRoot;
            else if (first4 == "HKCU") return Registry.CurrentUser;
            else return null;
        }
        public bool IsRegistry(string path) {
            if (path.Length < 4) return false;
            string first4 = path.Substring(0, 4);
            RegistryKey master = RegRoot(first4);
            return master != null;
        }
        public RegistryKey OpenRegKey(string path, bool writable) {
            if (path.Length < 4) return null;
            string first4 = path.Substring(0, 4).ToUpper();
            RegistryKey master = RegRoot(first4);
            path = path.Substring(5);
            return master.OpenSubKey(path, writable);
        }
        public RegistryKey OpenRegKey(string path) {
            return OpenRegKey(path, false);
        }

        public EItemType GetType(string item) {
            EItemType type;
            string path = item;
            if (Utils.utils.IsRegistry(path)) {
                if (path.IndexOf(@"\\") > -1) type = EItemType.RegistryValue;
                else type = EItemType.RegistryKey;
            }
            else {
                if (path[path.Length - 1] != '\\') type = EItemType.File;
                else type = EItemType.Folder;
            }
            return type;
        }
        public bool Exists(string item) {
            EItemType type = GetType(item);
            if (type == EItemType.RegistryKey) {
                using (RegistryKey rk = OpenRegKey(item)) {
                    if (rk != null) return true;
                    else return false;
                }
            }
            else if (type == EItemType.RegistryValue) {
                MyTuple<string, string> split = Utils.utils.SplitParent(item);
                using (RegistryKey rk = OpenRegKey(split.Item1)) {
                    if (rk != null)
                        return rk.GetValueNames().Contains(split.Item2);
                    else return false;
                }
            }
            if (type == EItemType.Folder)
                return Directory.Exists(item);
            else if (type == EItemType.File)
                return File.Exists(item);
            else {
                Debug.Fail("Unknown EItemType encountered.");
                return false;
            }
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

        public Image FadeImage(Image img) {
            Bitmap bmp = new Bitmap(img);
            // A tutorial on locking bits can be found here -
            //   http://www.bobpowell.net/lockingbits.htm
            //BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            for (int x = 0; x < bmp.Width; ++x)
                for (int y = 0; y < bmp.Height; ++y) {
                    Color c = bmp.GetPixel(x, y);
                    c = Color.FromArgb(c.A / 2, c);
                    bmp.SetPixel(x, y, c);
                }
            //bmp.UnlockBits(bmpdata);
            return bmp;
        }
        public Color ColorBlend(Color color1, Color color2, double t) {
            return Color.FromArgb(
                (int)(color1.R * (1 - t) + color2.R * t),
                (int)(color1.G * (1 - t) + color2.G * t),
                (int)(color1.B * (1 - t) + color2.B * t));
        }
        public Image ColorIcon(Color color) {
            const int size = 16;
            Color maxDark = ColorBlend(color, Color.Black, 0.1);
            color = ColorBlend(color, Color.White, 0.05);
            Bitmap bmp = new Bitmap(size, size);
            int margin = 2;
            for (int i = 0; i < bmp.Width; ++i)
                for (int j = 0; j < bmp.Height; ++j) {
                    Color c;
                    if (i < margin || i >= size - margin || j < margin || j >= size - margin)
                        c = Color.Magenta;
                    else {
                        double dist = (double)(i + j - size + 1) / (size - 1);
                        // dist varies from -1 to 1 from topleft to bottomright
                        double dark = (dist + 1)/2;
                        if (dark < 0) dark = 0;
                        else if (dark > 1) dark = 1;
                        c = ColorBlend(color, maxDark, dark);
                    }
                    bmp.SetPixel(i, j, c);
                }
            return bmp;
        }

        public static Utils utils = new Utils();

        internal MyTuple<string,string> SplitParent(string p) {
            int sep = p.IndexOf(@"\\");
            if (sep == -1) {
                // determine the last @"\", ignoring the ending \ if any
                string temp = p;
                while (temp.Length > 0 && temp[temp.Length - 1] == '\\') temp = temp.Substring(0, temp.Length - 1);
                sep = temp.LastIndexOf('\\');
                return new MyTuple<string, string>(p.Substring(0, sep + 1), p.Substring(sep + 1));
            }
            return new MyTuple<string, string>(p.Substring(0, sep + 1), p.Substring(sep + 2));
        }
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
        private FileStream fstream;
        public GZipReader(string inFile) {
            fstream = File.OpenRead(inFile);
            gzs = new GZipStream(fstream, CompressionMode.Decompress);
            sr = new StreamReader(gzs);
        }
        public long Position {
            get { return fstream.Position; }
        }
        public long Length {
            get { return fstream.Length; }
        }
        public TextReader Reader {
            get { return sr; }
        }
        public void Dispose() {
            sr.Close();
            gzs.Close();
            fstream.Close();
        }
    }
}
