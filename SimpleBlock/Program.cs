using SimpleBlock.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace SimpleBlock {
    internal static class Program {
        internal static Mutex Mutex;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Mutex = new Mutex(true, Assembly.GetExecutingAssembly().GetCustomAttribute<GuidAttribute>().Value.ToUpper());
            if (IsRunning()) {
                MessageBox.Show("ERROR Multiple Instances!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) {
                MessageBox.Show("ERROR Run Program as Administrator!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            if (!File.Exists(Utils.HostPath)) {
                MessageBox.Show("Host File Missing :(", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(Utils.FindHosts()));
        }

        //Snippet from ORCUS Rat
        static bool IsRunning() {
            for (int i = 0; i < 10; i++) {
                try {
                    if (Mutex.WaitOne(TimeSpan.Zero, true)) 
                        return false;
                }
                catch  { }
                if (i == 9) 
                    return true;
                Thread.Sleep(200);
            }

            return true;
        }
    }
}
