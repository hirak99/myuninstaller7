using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Principal;

namespace MyUninstaller7 {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            string text = Utils.utils.ResolveEnvironment("%HOMEDRIVE%%HOMEPATH%Arnab%abc%def%gh");
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            //AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            SaveStateForm.SaveStateWithProgress(Utils.utils.ExeFolder() + @"state1.txt.gz");
            //Uninstaller unins = new Uninstaller();
            //unins.SaveState(Utils.utils.ExeFolder()+@"state1.txt.gz");
        }
    }
}
