using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyUninstaller7 {
    public partial class SaveStateForm : Form {
        public bool result = false;
        private StateSaver saveState;
        private SaveStateForm() {
            InitializeComponent();
        }
        public static bool SaveStateWithProgress(string outFile) {
            SaveStateForm form = new SaveStateForm();
            form.saveState = new StateSaver(outFile);
            form.backgroundWorker1.RunWorkerAsync();
            form.ShowDialog();
            return form.result;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) {
            saveState.SaveState(backgroundWorker1.ReportProgress, 
                delegate {return backgroundWorker1.CancellationPending;}
                );
            result = true;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            Close();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            StateSaver.Status status = (StateSaver.Status)e.UserState;
            progressBar1.Value = status.current;
            progressBar1.Maximum = status.total;
            label1.Text = status.path;
        }

        private void SaveStateForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (backgroundWorker1.IsBusy) {
                backgroundWorker1.CancelAsync();
                button1.Enabled = false;
                button1.Text = "Cancelling...";
                e.Cancel = true;
            }
        }
    }
}
