using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Nowcast
{
    // Autostart via HKCU\Software\Microsoft\Windows\CurrentVersion\Run
    static class Startup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Name = "PulsePresence";

        static string Command
        {
            get { return "\"" + Application.ExecutablePath + "\" --autostart"; }
        }

        public static bool IsEnabled
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    return key != null && key.GetValue(Name) is string;
                }
            }
        }

        public static void Enable()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                key.SetValue(Name, Command);
            }
        }

        public static void Disable()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (key != null) key.DeleteValue(Name, false);
            }
        }

        // If the exe was moved, refresh the path in the registry if already enabled
        public static void Refresh()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                var value = key == null ? null : key.GetValue(Name) as string;
                if (value != null && value != Command) key.SetValue(Name, Command);
            }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public static void RefreshShellIcons()
        {
            try
            {
                SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); // SHCNE_ASSOCCHANGED
            }
            catch { }
        }

        public static bool CreateDesktopShortcut()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "PulsePresence.lnk");
            try
            {
                var dir = Path.GetDirectoryName(Application.ExecutablePath);
                var icoPath = Path.Combine(dir, "assets", "icon.ico");
                var iconLocation = File.Exists(icoPath) ? icoPath : Application.ExecutablePath + ",0";

                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                var shell = Activator.CreateInstance(shellType);
                var link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                var linkType = link.GetType();
                linkType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { Application.ExecutablePath });
                linkType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { dir });
                linkType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, new object[] { iconLocation });
                linkType.InvokeMember("Description", BindingFlags.SetProperty, null, link, new object[] { "PulsePresence - Discord Audio Presence" });
                linkType.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);

                RefreshShellIcons();
                return true;
            }
            catch (Exception e)
            {
                Log.Write("Desktop shortcut error: " + e.Message);
                return false;
            }
        }
    }
}
