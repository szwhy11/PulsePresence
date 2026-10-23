using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nowcast
{
    // The icon by the clock and its menu. Seen only on this computer.
    class TrayApp : ApplicationContext
    {
        readonly Engine engine = new Engine();
        readonly NotifyIcon icon = new NotifyIcon();
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        // A window-less control to get back to the UI thread from the engine.
        readonly Control ui = new Control();
        readonly Timer updateTimer = new Timer { Interval = 6 * 60 * 60 * 1000 };

        readonly ToolStripMenuItem statusItem = new ToolStripMenuItem { AutoToolTip = true };
        readonly ToolStripMenuItem hideItem = new ToolStripMenuItem(Text.HideHour);
        readonly ToolStripMenuItem sourcesItem = new ToolStripMenuItem(Text.Sources);
        readonly ToolStripMenuItem sessionsItem = new ToolStripMenuItem(Text.WhatWindowsSees);
        readonly ToolStripMenuItem autostartItem = new ToolStripMenuItem(Text.Autostart) { CheckOnClick = true };
        readonly ToolStripMenuItem settingsItem = new ToolStripMenuItem(Text.Settings);
        readonly ToolStripMenuItem logItem = new ToolStripMenuItem(Text.OpenLog);
        readonly ToolStripMenuItem updateItem = new ToolStripMenuItem(Text.CheckUpdates);
        readonly ToolStripMenuItem githubItem = new ToolStripMenuItem(Text.GitHub);
        readonly ToolStripMenuItem exitItem = new ToolStripMenuItem(Text.Exit);
        Release release;
        WpfSettingsWindow settingsWindow;

        public TrayApp(bool isUpdate)
        {
            ui.CreateControl();

            statusItem.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold);
            statusItem.Click += delegate { ShowSettings(); };
            settingsItem.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold);
            settingsItem.Click += delegate { ShowSettings(); };
            logItem.Click += delegate { Open("notepad.exe", Log.FilePath); };
            githubItem.Click += delegate { Open("https://github.com/" + Program.Repo, null); };
            exitItem.Click += delegate { ExitThread(); };

            hideItem.Click += delegate
            {
                if (DateTime.Now < engine.HiddenUntil) engine.Show();
                else engine.Hide(TimeSpan.FromHours(1));
            };
            autostartItem.Click += delegate
            {
                if (autostartItem.Checked) Startup.Enable();
                else Startup.Disable();
            };
            updateItem.Click += delegate { CheckUpdates(true); };
            sessionsItem.DropDownItems.Add(new ToolStripMenuItem(Text.NoSessions) { Enabled = false });

            menu.Renderer = new DarkMenuRenderer();
            menu.BackColor = Color.FromArgb(24, 25, 28);
            menu.ForeColor = Color.FromArgb(242, 243, 245);
            sessionsItem.DropDown.Renderer = menu.Renderer;
            sourcesItem.DropDown.Renderer = menu.Renderer;

            menu.Items.Add(statusItem);
            menu.Items.Add(hideItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(sourcesItem);
            menu.Items.Add(sessionsItem);
            menu.Items.Add(autostartItem);
            menu.Items.Add(settingsItem);
            menu.Items.Add(logItem);
            menu.Items.Add(updateItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(githubItem);
            menu.Items.Add(exitItem);
            menu.Opening += delegate { RefreshMenu(); };
            menu.ShowItemToolTips = true;
            sessionsItem.DropDown.ShowItemToolTips = true;

            icon.Icon = LoadTrayIcon();
            icon.Text = Text.AppName;
            icon.ContextMenuStrip = menu;
            // A left click opens the same menu as a right one.
            icon.MouseUp += (sender, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(icon, null);
            };
            icon.BalloonTipClicked += delegate
            {
                if (release != null) InstallUpdate();
            };
            icon.Visible = true;

            engine.Changed += delegate
            {
                if (ui.IsHandleCreated) ui.BeginInvoke(new Action(RefreshStatus));
            };

            Startup.Refresh();
            if (isUpdate) icon.ShowBalloonTip(5000, Text.AppName, string.Format(Text.Updated, Program.Version), ToolTipIcon.Info);

            engine.Start();

            // First look ten seconds in, then every six hours.
            updateTimer.Tick += delegate { CheckUpdates(false); };
            var firstCheck = new Timer { Interval = 10000 };
            firstCheck.Tick += delegate
            {
                firstCheck.Dispose();
                CheckUpdates(false);
                updateTimer.Start();
            };
            firstCheck.Start();
        }

        void RefreshStatus()
        {
            string status = engine.Status;
            bool isPlaying = status != Text.DiscordOff && status != Text.NothingPlaying && !status.StartsWith(Text.Paused);
            string iconPrefix = isPlaying ? "● " : "○ ";
            statusItem.Text = iconPrefix + status;
            // The tooltip holds at most 63 characters.
            var tip = Text.AppName + "\n" + engine.Status;
            icon.Text = tip.Length > 63 ? tip.Substring(0, 62) + "…" : tip;
        }

        void RefreshMenu()
        {
            RefreshStatus();
            hideItem.Text = DateTime.Now < engine.HiddenUntil ? Text.ShowAgain : Text.HideHour;
            autostartItem.Checked = Startup.IsEnabled;
            if (release != null) updateItem.Text = string.Format(Text.UpdateTo, release.Tag);
            else updateItem.Text = Text.CheckUpdates;

            RefreshSources();

            sessionsItem.DropDownItems.Clear();
            foreach (var track in engine.Sessions)
            {
                var line = Engine.Describe(track) + " (" + (track.IsPlaying ? Text.Playing : Text.Paused) + ")";
                // A click copies the app ID, which a "player not recognized" report needs.
                var app = track.App;
                sessionsItem.DropDownItems.Add(new ToolStripMenuItem(line, null, delegate { Clipboard.SetText(app); }) { ToolTipText = app });
            }
            if (sessionsItem.DropDownItems.Count == 0)
            {
                sessionsItem.DropDownItems.Add(new ToolStripMenuItem(Text.NoSessions) { Enabled = false });
            }
        }

        // One window at a time: a second click brings the open one forward.
        // Created on open and fully Closed (not Hidden) to keep idle RAM minimal.
        void ShowSettings()
        {
            if (settingsWindow != null && settingsWindow.IsLoaded)
            {
                if (settingsWindow.WindowState == System.Windows.WindowState.Minimized)
                    settingsWindow.WindowState = System.Windows.WindowState.Normal;
                settingsWindow.Activate();
                return;
            }

            settingsWindow = new WpfSettingsWindow(engine);
            settingsWindow.Closed += delegate
            {
                settingsWindow = null;
                GC.Collect(0, GCCollectionMode.Optimized);
            };
            settingsWindow.Show();
            settingsWindow.Activate();
        }

        // A checkbox per app playing now, plus the hidden ones, so they can come
        // back: a cleared box keeps that app out of the status (the "hide" setting).
        void RefreshSources()
        {
            sourcesItem.DropDownItems.Clear();
            var settings = engine.Settings;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var track in engine.Sessions)
            {
                if (!seen.Add(track.Source.Name)) continue;
                AddSource(track.Source.Name, track.App, !settings.Hides(track.App, track.Source.Name));
            }
            foreach (var word in settings.Hide)
            {
                if (seen.Add(word)) AddSource(word, word, false);
            }
            if (sourcesItem.DropDownItems.Count == 0)
            {
                sourcesItem.DropDownItems.Add(new ToolStripMenuItem(Text.NoSessions) { Enabled = false });
            }
        }

        void AddSource(string name, string app, bool isShown)
        {
            var item = new ToolStripMenuItem(name) { Checked = isShown, CheckOnClick = true };
            item.Click += delegate
            {
                // From the file as it is now, so edits made by hand are kept.
                var settings = Settings.Load();
                settings.SetHidden(app, name, !item.Checked);
                settings.Save();
            };
            sourcesItem.DropDownItems.Add(item);
        }

        // byHand: from the menu, so say the result either way and install
        // what was found; on the timer, only announce a new version.
        void CheckUpdates(bool byHand)
        {
            if (!byHand && !engine.Settings.CheckUpdates) return;
            if (byHand && release != null)
            {
                InstallUpdate();
                return;
            }
            Task.Factory.StartNew(() => Updater.FindNewer()).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Log.Write("Update check failed: " + task.Exception.GetBaseException().Message);
                    if (byHand) Say(string.Format(Text.UpdateFailed, task.Exception.GetBaseException().Message));
                    return;
                }
                if (task.Result == null)
                {
                    if (byHand) Say(string.Format(Text.UpToDate, Program.Version));
                    return;
                }
                release = task.Result;
                Log.Write("Found " + release.Tag);
                if (byHand) InstallUpdate();
                else icon.ShowBalloonTip(10000, Text.AppName, string.Format(Text.UpdateAvailable, release.Tag), ToolTipIcon.Info);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        void InstallUpdate()
        {
            var target = release;
            Task.Factory.StartNew(() => Updater.Install(target)).ContinueWith(task =>
            {
                if (!task.IsFaulted)
                {
                    ExitThread();
                    return;
                }
                var message = task.Exception.GetBaseException().Message;
                Log.Write("Update failed: " + message);
                Say(string.Format(Text.UpdateFailed, message));
                // Perhaps no rights to the folder: the release page still works.
                if (target.PageUrl != null) Open(target.PageUrl, null);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        void Say(string text)
        {
            icon.ShowBalloonTip(6000, Text.AppName, text, ToolTipIcon.Info);
        }

        static void Open(string file, string argument)
        {
            try
            {
                if (argument == null) Process.Start(file);
                else Process.Start(file, "\"" + argument + "\"");
            }
            catch (Exception e)
            {
                Log.Write("Could not open " + file + ": " + e.Message);
            }
        }

        static Icon LoadTrayIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath, SystemInformation.SmallIconSize);
                }
            }
            catch { }

            try
            {
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        protected override void ExitThreadCore()
        {
            icon.Visible = false;
            engine.Stop();
            icon.Dispose();
            updateTimer.Dispose();
            base.ExitThreadCore();
        }
    }

    class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Color.FromArgb(242, 243, 245) : Color.FromArgb(148, 155, 164);
            base.OnRenderItemText(e);
        }
    }

    class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(24, 25, 28); } }
        public override Color ImageMarginGradientBegin { get { return Color.FromArgb(24, 25, 28); } }
        public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(24, 25, 28); } }
        public override Color ImageMarginGradientEnd { get { return Color.FromArgb(24, 25, 28); } }
        public override Color MenuBorder { get { return Color.FromArgb(43, 45, 49); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(88, 101, 242); } }
        public override Color MenuItemSelected { get { return Color.FromArgb(43, 45, 49); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(43, 45, 49); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(43, 45, 49); } }
        public override Color SeparatorDark { get { return Color.FromArgb(43, 45, 49); } }
        public override Color SeparatorLight { get { return Color.FromArgb(30, 31, 34); } }
        public override Color CheckBackground { get { return Color.FromArgb(88, 101, 242); } }
        public override Color CheckSelectedBackground { get { return Color.FromArgb(71, 82, 196); } }
    }
}
