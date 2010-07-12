using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace MyUninstaller7 {
    static class Program {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            using (Mutex mut = new Mutex(false, "My Uninstaller 7")) {
                if (!mut.WaitOne(0, false))
                    MessageBox.Show("My Uninstaller 7 is already running. Multiple copies of this application cannot be run simulteneously.",
                        "Uninstaller 7",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                else {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
                }
            }
        }
    }
}
