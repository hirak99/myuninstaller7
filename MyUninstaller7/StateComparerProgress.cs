using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyUninstaller7 {
    public partial class StateComparerProgress : Form {
        private StateComparerProgress(string _file1, string _file2) {
            InitializeComponent();
            file1 = _file1; file2 = _file2;
            backgroundWorker1.RunWorkerAsync();
        }
        private StateComparer sc = new StateComparer();
        private string file1, file2;
        public static StateComparer CompareStateWithProgress(string file1, string file2) {
            StateComparerProgress scp = new StateComparerProgress(file1, file2);
            scp.ShowDialog();
            return scp.sc;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) {
            sc.Compare(file1, file2, backgroundWorker1.ReportProgress);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            MyTuple<long, long> state = (MyTuple<long, long>)e.UserState;
            int value = (int)(state.Item1 * progressBar1.Maximum / state.Item2);
            progressBar1.Value = value;
            // Hack to disable animation introduced in Vista. The animation causes progressbar to update slowly
            //   causing it not to reach full value before it disappears.
            //   http://stackoverflow.com/questions/977278/how-can-i-make-the-progress-bar-update-fast-enough
            if (value > 0) {
                progressBar1.Value = value - 1;
                progressBar1.Value = value;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            Close();
        }

        private void StateComparerProgress_FormClosing(object sender, FormClosingEventArgs e) {
            if (backgroundWorker1.IsBusy) e.Cancel = true;
        }
    }
}
