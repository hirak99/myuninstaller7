using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Principal;
using System.IO;

namespace MyUninstaller7 {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            EnsureCatalog();
        }

        // Creates the catalog if not already present
        private void EnsureCatalog() {
            string catfile = Utils.utils.ExeFolder()+"defcatalog.txt";
            if (File.Exists(catfile)) return;
            using (StreamWriter sw = new StreamWriter(catfile))
                sw.Write(MyUninstaller7.Properties.Resources.defcatalog);
            MessageBox.Show("The catalog file 'defcatalog.txt' was automatically created in" +
                "application folder. You can" +
                " edit this file to adjust which locations will be monitored during installation of" +
                " appliations.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            //AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            SaveStateForm.SaveStateWithProgress(Utils.utils.ExeFolder() + @"state1.txt.gz");
            //Uninstaller unins = new Uninstaller();
            //unins.SaveState(Utils.utils.ExeFolder()+@"state1.txt.gz");
        }
    }
}
