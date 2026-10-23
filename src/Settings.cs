using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Nowcast
{
    // %APPDATA%\discord-nowcast\config.json. Written with the defaults on the
    // first run; a broken file falls back to the defaults and says so in the log.
    class Settings
    {
        public static readonly string FilePath = Path.Combine(Log.Folder, "config.json");

        public string ClientId = "1562901782579912804";
        // What the member list shows after "Listening to": artist_title, title_artist, artist, title, source, app.
        public string InList = "artist_title";
        public int ActivityType = 2; // 2 = Listening, 0 = Playing, 3 = Watching
        public bool ShowPaused = false;
        public bool Covers = true;
        public bool UploadCovers = true;
        public bool Buttons = true;
        public bool CheckUpdates = true;
        public static readonly string[] DefaultMusicSources = new[]
        {
            "Яндекс Музыка", "Yandex Music", "Spotify", "VK Музыка", "VK Music", "SoundCloud", "YouTube Music"
        };

        public string Image = "";
        public List<string> Enabled = new List<string>();
        public List<string> Hide = new List<string>();
        public Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ClientIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static Settings Load()
        {
            var settings = new Settings();
            try
            {
                Directory.CreateDirectory(Log.Folder);
                if (!File.Exists(FilePath))
                {
                    settings.Save();
                }
                var values = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(FilePath, Encoding.UTF8));
                if (values != null) settings.Apply(values);
            }
            catch (Exception e)
            {
                Log.Write("config.json could not be read, using the defaults: " + e.Message);
            }
            return settings;
        }

        // Writes every setting, one per line, so the file stays easy to edit by hand.
        public void Save()
        {
            var json = new JavaScriptSerializer();
            var names = new List<string>();
            foreach (var pair in Names) names.Add(json.Serialize(pair.Key) + ": " + json.Serialize(pair.Value));
            var cids = new List<string>();
            foreach (var pair in ClientIds) cids.Add(json.Serialize(pair.Key) + ": " + json.Serialize(pair.Value));
            var lines = new[]
            {
                "  \"clientId\": " + json.Serialize(ClientId),
                "  \"activityType\": " + ActivityType,
                "  \"inList\": " + json.Serialize(InList),
                "  \"showPaused\": " + json.Serialize(ShowPaused),
                "  \"covers\": " + json.Serialize(Covers),
                "  \"uploadCovers\": " + json.Serialize(UploadCovers),
                "  \"buttons\": " + json.Serialize(Buttons),
                "  \"checkUpdates\": " + json.Serialize(CheckUpdates),
                "  \"image\": " + json.Serialize(Image),
                "  \"enabled\": " + json.Serialize(Enabled),
                "  \"hide\": " + json.Serialize(Hide),
                "  \"names\": {" + string.Join(", ", names) + "}",
                "  \"clientIds\": {" + string.Join(", ", cids) + "}",
            };
            Directory.CreateDirectory(Log.Folder);
            File.WriteAllText(FilePath, "{\r\n" + string.Join(",\r\n", lines) + "\r\n}\r\n", new UTF8Encoding(false));
        }

        // Checks whether a player/source is enabled.
        // Default music players or explicitly enabled sources are ON.
        // Any newly added or unknown player/source is OFF by default.
        public bool IsSourceEnabled(string app, string source)
        {
            if (string.IsNullOrEmpty(source)) return false;
            string cleanApp = app ?? "";

            // 1. Explicitly hidden?
            foreach (var word in Hide)
            {
                if (cleanApp.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return false;
                if (source.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return false;
            }

            // 2. Explicitly enabled by the user?
            foreach (var word in Enabled)
            {
                if (cleanApp.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (source.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            // 3. Known default music players are ON out of the box
            foreach (var def in DefaultMusicSources)
            {
                if (source.IndexOf(def, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    cleanApp.IndexOf(def, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            // 4. Any newly discovered player/source (browsers, apps, raw IDs) is OFF by default!
            return false;
        }

        public bool Hides(string app, string source)
        {
            return !IsSourceEnabled(app, source);
        }

        public void SetSourceEnabled(string app, string source, bool isEnabled)
        {
            string cleanApp = app ?? "";
            if (isEnabled)
            {
                Hide.RemoveAll(word =>
                    cleanApp.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    source.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                bool alreadyInEnabled = false;
                foreach (var word in Enabled)
                {
                    if (source.Equals(word, StringComparison.OrdinalIgnoreCase))
                    {
                        alreadyInEnabled = true;
                        break;
                    }
                }
                if (!alreadyInEnabled)
                {
                    Enabled.Add(source);
                }
            }
            else
            {
                Enabled.RemoveAll(word =>
                    cleanApp.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    source.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                bool alreadyInHide = false;
                foreach (var word in Hide)
                {
                    if (source.Equals(word, StringComparison.OrdinalIgnoreCase))
                    {
                        alreadyInHide = true;
                        break;
                    }
                }
                if (!alreadyInHide)
                {
                    Hide.Add(source);
                }
            }
        }

        // Shows or hides a source, as the tray's Sources menu does.
        public void SetHidden(string app, string source, bool isHidden)
        {
            SetSourceEnabled(app, source, !isHidden);
        }

        void Apply(Dictionary<string, object> values)
        {
            var clientId = Get<string>(values, "clientId");
            if (clientId != null && clientId.Trim() != "") ClientId = clientId.Trim();
            InList = Get<string>(values, "inList") ?? InList;
            if (values.ContainsKey("activityType"))
            {
                int at;
                if (int.TryParse(values["activityType"].ToString(), out at)) ActivityType = at;
            }
            ShowPaused = Get(values, "showPaused", ShowPaused);
            Covers = Get(values, "covers", Covers);
            UploadCovers = Get(values, "uploadCovers", UploadCovers);
            Buttons = Get(values, "buttons", Buttons);
            CheckUpdates = Get(values, "checkUpdates", CheckUpdates);
            Image = Get<string>(values, "image") ?? Image;

            var enabled = Get<ArrayList>(values, "enabled");
            if (enabled != null)
            {
                foreach (var word in enabled)
                {
                    if (word is string && (string)word != "") Enabled.Add((string)word);
                }
            }
            var hide = Get<ArrayList>(values, "hide");
            if (hide != null)
            {
                foreach (var word in hide)
                {
                    if (word is string && (string)word != "") Hide.Add((string)word);
                }
            }
            var names = Get<Dictionary<string, object>>(values, "names");
            if (names != null)
            {
                foreach (var pair in names)
                {
                    if (pair.Value is string) Names[pair.Key] = (string)pair.Value;
                }
            }
            var cids = Get<Dictionary<string, object>>(values, "clientIds");
            if (cids != null)
            {
                foreach (var pair in cids)
                {
                    if (pair.Value is string) ClientIds[pair.Key] = (string)pair.Value;
                }
            }
        }

        static T Get<T>(Dictionary<string, object> values, string key) where T : class
        {
            object value;
            return values.TryGetValue(key, out value) ? value as T : null;
        }

        static bool Get(Dictionary<string, object> values, string key, bool fallback)
        {
            object value;
            return values.TryGetValue(key, out value) && value is bool ? (bool)value : fallback;
        }
    }
}
