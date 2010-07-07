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

        private string stateFile1 = Utils.utils.ExeFolder() + @"state1.txt.gz";
        private string stateFile2 = Utils.utils.ExeFolder() + @"state2.txt.gz";

        /***
         * State = 0 means ready to note changes
         *       = 1 means already noting changes
         ***/
        private int State = 0;
        protected void SetState(int newState) {
            State = newState;
            toolStripButton1.Enabled = (State == 0);
            toolStripButton2.Enabled = (State == 1);
            toolStripButton3.Enabled = (State == 1);
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
            if (SaveStateForm.SaveStateWithProgress(Utils.utils.ExeFolder() + @"state1.txt.gz"))
                SetState(1);
        }

        private void toolStripButton2_Click(object sender, EventArgs e) {
            if (SaveStateForm.SaveStateWithProgress(stateFile2)) {
                SetState(0);
                //TODO: Comparison of the states goes here
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e) {
            if (MessageBox.Show("Really cancel noting changes?", "Warning", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes) {
                File.Delete(stateFile1);
                SetState(0);
            }
        }
    }
}
