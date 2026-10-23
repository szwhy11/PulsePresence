// PulsePresence: shows the track playing in any Windows app as your
// Discord Rich Presence status, with a modern tray icon and dark control panel.
//
// Built by build.ps1 with the C# compiler that ships with .NET Framework 4.8.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Nowcast
{
    static class Program
    {
        public const string Version = "2.0.0";
        public const string Repo = "callmainplz/PulsePresence";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            bool isUpdate = args.Contains("--updated");
            Updater.CleanUp();

            var mutex = new Mutex(false, "Local\\PulsePresence");
            bool isFirst;
            try
            {
                isFirst = mutex.WaitOne(isUpdate ? 15000 : 1000);
            }
            catch (AbandonedMutexException)
            {
                isFirst = true;
            }
            if (!isFirst)
            {
                MessageBox.Show(Text.AlreadyRunning, Text.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Log.Start();
            Log.Write(Text.AppName + " " + Version + " " + Application.ExecutablePath);
            Startup.RefreshShellIcons();
            try
            {
                Application.Run(new TrayApp(isUpdate));
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
