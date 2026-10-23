using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Strings = Nowcast.Text;

namespace Nowcast
{
    internal partial class WpfSettingsWindow : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        const int DWMWCP_ROUND = 2; // Windows 11 rounded window corners

        static readonly string[] inListValues = { "artist_title", "title_artist", "artist", "title", "source", "app" };
        static readonly int[] activityTypeValues = { 2, 0, 3 }; // Listening, Playing, Watching

        readonly Engine engine;
        bool isLoading;
        string lastPreviewKey = "";

        internal WpfSettingsWindow(Engine engine)
        {
            this.engine = engine;
            if (System.Windows.Application.Current == null)
            {
                new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            InitializeComponent();

            txtVersion.Text = "v" + Program.Version;

            cmbActivityType.Items.Add(Strings.ActivityListening);
            cmbActivityType.Items.Add(Strings.ActivityPlaying);
            cmbActivityType.Items.Add(Strings.ActivityWatching);

            cmbInList.Items.Add(Strings.InListArtistTitle);
            cmbInList.Items.Add(Strings.InListTitleArtist);
            cmbInList.Items.Add(Strings.InListArtist);
            cmbInList.Items.Add(Strings.InListTitle);
            cmbInList.Items.Add(Strings.InListSource);
            cmbInList.Items.Add(Strings.InListApp);

            chkCovers.Content = Strings.CoversOption;
            chkUploadCovers.Content = Strings.UploadCoversOption;
            chkButtons.Content = Strings.ButtonsOption;
            chkShowPaused.Content = Strings.ShowPausedOption;
            chkAutostart.Content = Strings.Autostart;
            chkUpdates.Content = Strings.CheckUpdatesOption;

            Fill();
            WireEvents();

            // Event-driven preview updates: no continuous polling timers!
            if (engine != null)
            {
                engine.Changed += OnEngineChanged;
            }

            Closed += delegate
            {
                if (engine != null)
                {
                    engine.Changed -= OnEngineChanged;
                }
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyDwmStyling();
        }

        void ApplyDwmStyling()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                var os = Environment.OSVersion;
                if (os.Version.Major >= 10 && os.Version.Build >= 17763)
                {
                    int attr = os.Version.Build >= 18985 ? DWMWA_USE_IMMERSIVE_DARK_MODE : DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                    int enabled = 1;
                    DwmSetWindowAttribute(hwnd, attr, ref enabled, sizeof(int));

                    // Windows 11 rounded window corners
                    if (os.Version.Build >= 22000)
                    {
                        int cornerPref = DWMWCP_ROUND;
                        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
                    }
                }
            }
            catch { }
        }

        void OnEngineChanged()
        {
            Dispatcher.BeginInvoke(new Action(UpdatePreview));
        }

        void Fill()
        {
            isLoading = true;
            var settings = Settings.Load();

            var index = Array.IndexOf(inListValues, settings.InList);
            cmbInList.SelectedIndex = index >= 0 ? index : 0;

            var atIndex = Array.IndexOf(activityTypeValues, settings.ActivityType);
            cmbActivityType.SelectedIndex = atIndex >= 0 ? atIndex : 0;

            chkCovers.IsChecked = settings.Covers;
            chkUploadCovers.IsChecked = settings.UploadCovers;
            chkUploadCovers.IsEnabled = settings.Covers;
            chkButtons.IsChecked = settings.Buttons;
            chkShowPaused.IsChecked = settings.ShowPaused;
            chkUpdates.IsChecked = settings.CheckUpdates;
            chkAutostart.IsChecked = Startup.IsEnabled;

            PopulateSources(settings);

            isLoading = false;
            UpdatePreview();
        }

        void PopulateSources(Settings settings)
        {
            panelSources.Children.Clear();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasAny = false;

            if (engine != null && engine.Sessions != null)
            {
                foreach (var track in engine.Sessions)
                {
                    if (track.Source == null || !seen.Add(track.Source.Name)) continue;
                    hasAny = true;
                    var app = track.App;
                    var name = track.Source.Name;
                    bool isShown = settings.IsSourceEnabled(app, name);

                    var toggle = new CheckBox
                    {
                        Content = name,
                        IsChecked = isShown,
                        Style = (Style)FindResource("ModernToggleStyle")
                    };
                    toggle.Click += delegate
                    {
                        Change(s => s.SetSourceEnabled(app, name, toggle.IsChecked == true));
                    };
                    panelSources.Children.Add(toggle);
                }
            }

            foreach (var word in settings.Enabled)
            {
                if (seen.Add(word))
                {
                    hasAny = true;
                    var enabledName = word;
                    var toggle = new CheckBox
                    {
                        Content = enabledName,
                        IsChecked = true,
                        Style = (Style)FindResource("ModernToggleStyle")
                    };
                    toggle.Click += delegate
                    {
                        Change(s => s.SetSourceEnabled(enabledName, enabledName, toggle.IsChecked == true));
                    };
                    panelSources.Children.Add(toggle);
                }
            }

            foreach (var word in settings.Hide)
            {
                if (seen.Add(word))
                {
                    hasAny = true;
                    var hiddenName = word;
                    var toggle = new CheckBox
                    {
                        Content = hiddenName,
                        IsChecked = false,
                        Style = (Style)FindResource("ModernToggleStyle")
                    };
                    toggle.Click += delegate
                    {
                        Change(s => s.SetSourceEnabled(hiddenName, hiddenName, toggle.IsChecked == true));
                    };
                    panelSources.Children.Add(toggle);
                }
            }

            if (!hasAny)
            {
                var emptyLbl = new TextBlock
                {
                    Text = "Нет активных медиа-сессий.\nЗапустите Яндекс Музыку или браузер.",
                    Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush"),
                    FontSize = 11,
                    Margin = new Thickness(4, 8, 0, 0)
                };
                panelSources.Children.Add(emptyLbl);
            }
        }

        void WireEvents()
        {
            cmbActivityType.SelectionChanged += delegate
            {
                if (isLoading) return;
                Change(s => s.ActivityType = activityTypeValues[cmbActivityType.SelectedIndex]);
                UpdatePreview();
            };

            cmbInList.SelectionChanged += delegate
            {
                if (isLoading) return;
                Change(s => s.InList = inListValues[cmbInList.SelectedIndex]);
                UpdatePreview();
            };

            chkCovers.Click += delegate
            {
                bool isCov = chkCovers.IsChecked == true;
                chkUploadCovers.IsEnabled = isCov;
                Change(s => s.Covers = isCov);
                UpdatePreview();
            };

            chkUploadCovers.Click += delegate
            {
                Change(s => s.UploadCovers = chkUploadCovers.IsChecked == true);
            };

            chkButtons.Click += delegate
            {
                Change(s => s.Buttons = chkButtons.IsChecked == true);
                UpdatePreview();
            };

            chkShowPaused.Click += delegate
            {
                Change(s => s.ShowPaused = chkShowPaused.IsChecked == true);
            };

            chkUpdates.Click += delegate
            {
                Change(s => s.CheckUpdates = chkUpdates.IsChecked == true);
            };

            chkAutostart.Click += delegate
            {
                if (isLoading) return;
                if (chkAutostart.IsChecked == true) Startup.Enable();
                else Startup.Disable();
            };

            btnDesktopShortcut.Click += delegate
            {
                if (Startup.CreateDesktopShortcut())
                {
                    MessageBox.Show(Strings.ShortcutCreated, "PulsePresence", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            btnLog.Click += delegate { OpenFile(Log.FilePath); };
            btnConfig.Click += delegate { OpenFile(Settings.FilePath); };
            btnClose.Click += delegate { Close(); };
        }

        void UpdatePreview()
        {
            var settings = Settings.Load();

            // 1. Connection status
            bool isDiscordOn = engine != null && engine.Status != Strings.DiscordOff;
            txtDiscordStatus.Text = isDiscordOn ? (Strings.Ru ? "Discord активен" : "Discord connected") : (Strings.Ru ? "Discord не запущен" : "Discord not running");
            var statusBrush = (System.Windows.Media.Brush)FindResource(isDiscordOn ? "OnlineGreenBrush" : "TextMutedBrush");
            txtDiscordStatus.Foreground = statusBrush;
            dotDiscordStatus.Fill = statusBrush;
            dotDiscordStatus.Effect = isDiscordOn ? new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 6,
                ShadowDepth = 0,
                Color = (System.Windows.Media.Color)FindResource("OnlineGreenColor"),
                Opacity = 0.8
            } : null;

            // 2. Active track
            Track track = null;
            if (engine != null && engine.Sessions != null && engine.Sessions.Count > 0)
            {
                foreach (var s in engine.Sessions)
                {
                    if (s.IsPlaying && settings.IsSourceEnabled(s.App, s.Source != null ? s.Source.Name : "")) { track = s; break; }
                }
                if (track == null)
                {
                    foreach (var s in engine.Sessions)
                    {
                        if (settings.IsSourceEnabled(s.App, s.Source != null ? s.Source.Name : "")) { track = s; break; }
                    }
                }
            }

            string title = track != null && !string.IsNullOrEmpty(track.Title) ? track.Title : Strings.PreviewSampleTitle;
            string artist = track != null && !string.IsNullOrEmpty(track.Artist) ? track.Artist : Strings.PreviewSampleArtist;
            string source = track != null && track.Source != null ? track.Source.Name : "Яндекс Музыка";

            string verb;
            switch (settings.ActivityType)
            {
                case 0: verb = Strings.Ru ? "ИГРАЕТ В" : "PLAYING"; break;
                case 3: verb = Strings.Ru ? "СМОТРИТ" : "WATCHING"; break;
                default: verb = Strings.Ru ? "СЛУШАЕТ" : "LISTENING TO"; break;
            }
            txtPreviewHeader.Text = (verb + " " + source).ToUpper();
            if (settings.InList == "artist_title")
            {
                txtPreviewTitle.Text = string.Format("{0} — {1}", artist, title);
                txtPreviewArtist.Text = artist;
            }
            else if (settings.InList == "title_artist")
            {
                txtPreviewTitle.Text = string.Format("{0} — {1}", title, artist);
                txtPreviewArtist.Text = artist;
            }
            else
            {
                txtPreviewTitle.Text = title;
                txtPreviewArtist.Text = artist;
            }

            borderPreviewButton.Visibility = settings.Buttons ? Visibility.Visible : Visibility.Collapsed;
            txtPreviewButton.Text = string.Format(Strings.ListenOn, source);

            imgPreviewCover.Visibility = settings.Covers ? Visibility.Visible : Visibility.Hidden;

            // Load actual album art from Media
            string currentKey = track != null ? track.Key : "";
            if (currentKey != lastPreviewKey)
            {
                lastPreviewKey = currentKey;
                if (track != null)
                {
                    byte[] thumbnailBytes = Media.ReadThumbnail(track);
                    if (thumbnailBytes != null && thumbnailBytes.Length > 4)
                    {
                        try
                        {
                            imgPreviewCover.Source = LoadBitmap(thumbnailBytes);
                        }
                        catch
                        {
                            imgPreviewCover.Source = null;
                        }
                    }
                    else
                    {
                        imgPreviewCover.Source = null;
                    }
                }
                else
                {
                    imgPreviewCover.Source = null;
                }
            }

            // Timeline
            if (track != null && track.Duration > 0)
            {
                int curSec = (int)track.Position;
                int durSec = (int)track.Duration;
                txtPreviewTimeCurrent.Text = string.Format("{0:D2}:{1:D2}", curSec / 60, curSec % 60);
                txtPreviewTimeEnd.Text = string.Format("{0:D2}:{1:D2}", durSec / 60, durSec % 60);
                double ratio = Math.Max(0, Math.Min(1.0, track.Position / track.Duration));
                borderProgressFill.Width = Math.Max(4, ratio * 200.0);
            }
            else
            {
                txtPreviewTimeCurrent.Text = "00:24";
                txtPreviewTimeEnd.Text = "02:13";
                borderProgressFill.Width = 70;
            }
        }

        static BitmapImage LoadBitmap(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var image = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }
            image.Freeze(); // Immutable, thread-safe, and zero GC overhead
            return image;
        }

        void Change(Action<Settings> change)
        {
            if (isLoading) return;
            var settings = Settings.Load();
            change(settings);
            settings.Save();
        }

        static void OpenFile(string file)
        {
            try
            {
                Process.Start("notepad.exe", "\"" + file + "\"");
            }
            catch (Exception e)
            {
                Log.Write("Could not open " + file + ": " + e.Message);
            }
        }
    }
}
