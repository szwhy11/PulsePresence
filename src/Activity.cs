using System;
using System.Collections.Generic;

namespace Nowcast
{
    // The Discord activity for a track: "Listening to …" (type 2).
    static class Activity
    {
        public static Dictionary<string, object> Build(Track track, long? startedAt, Settings settings)
        {
            var source = track.Source.Name;
            var artist = !string.IsNullOrEmpty(track.Artist) ? track.Artist : source;
            var title = !string.IsNullOrEmpty(track.Title) ? track.Title : (Text.Ru ? "Без названия" : "Unknown Track");
            var activity = new Dictionary<string, object> { { "type", settings.ActivityType }, { "instance", false } };

            string details;
            string state;
            int inListType;

            bool hasArtist = !string.IsNullOrEmpty(track.Artist);
            bool hasTitle = !string.IsNullOrEmpty(track.Title);

            string artistTitle = hasArtist && hasTitle
                ? string.Format("{0} — {1}", track.Artist, track.Title)
                : title;

            string titleArtist = hasArtist && hasTitle
                ? string.Format("{0} — {1}", track.Title, track.Artist)
                : title;

            bool hasDistinctAlbum = !string.IsNullOrEmpty(track.Album) &&
                                    !track.Album.Equals(source, StringComparison.OrdinalIgnoreCase) &&
                                    !track.Album.Equals(title, StringComparison.OrdinalIgnoreCase);

            switch (settings.InList)
            {
                case "artist_title":
                    details = artistTitle;
                    state = hasDistinctAlbum ? track.Album : artist;
                    inListType = 2; // Member list shows details: "Исполнитель — Песня"
                    break;

                case "title_artist":
                    details = titleArtist;
                    state = hasDistinctAlbum ? track.Album : artist;
                    inListType = 2; // Member list shows details: "Песня — Исполнитель"
                    break;

                case "title":
                    details = title;
                    state = artist;
                    inListType = 2; // Member list shows title
                    break;

                case "artist":
                    details = title;
                    state = artist;
                    inListType = 1; // Member list shows artist
                    break;

                case "app":
                case "source":
                    details = title;
                    state = artist;
                    inListType = 0; // Member list shows Discord app name
                    break;

                default:
                    details = artistTitle;
                    state = hasDistinctAlbum ? track.Album : artist;
                    inListType = 2;
                    break;
            }

            if (!track.IsPlaying)
            {
                if (inListType == 2)
                    details = Limit(details + " · " + Text.Paused);
                else
                    state = Limit(state + " · " + Text.Paused);
            }

            Lines(activity, details, state, inListType);

            var assets = new Dictionary<string, object>();
            var cover = settings.Covers ? Covers.FindOrWait(track, settings.UploadCovers, 1200) : null;
            var image = cover ?? (settings.Image != "" ? settings.Image : null);
            if (image != null)
            {
                assets["large_image"] = image;
                // Only show album if present, non-empty, and distinct from source and title.
                // Never repeat source/player name here (prevents redundant 3rd line in Discord).
                if (hasDistinctAlbum)
                {
                    assets["large_text"] = Limit(track.Album);
                }
            }
            if (assets.Count > 0) activity["assets"] = assets;

            if (startedAt.HasValue && track.IsPlaying)
            {
                activity["timestamps"] = new Dictionary<string, object>
                {
                    { "start", startedAt.Value },
                    { "end", startedAt.Value + (long)(track.Duration * 1000) },
                };
            }

            if (settings.Buttons) activity["buttons"] = new[] { Button(track) };
            return activity;
        }

        static void Lines(Dictionary<string, object> activity, string details, string state, int inList)
        {
            activity["details"] = Limit(details);
            activity["state"] = Limit(state);
            activity["status_display_type"] = inList;
        }

        // "Listen on SoundCloud": a search for the track in the same service,
        // since players do not tell Windows the track's link. YouTube otherwise.
        static Dictionary<string, object> Button(Track track)
        {
            var query = Uri.EscapeDataString((track.Artist + " " + track.Title).Trim());
            var hasOwn = track.Source.Search != null;
            var label = hasOwn ? string.Format(Text.ListenOn, track.Source.Name) : Text.FindOnYouTube;
            var url = (hasOwn ? track.Source.Search : "https://www.youtube.com/results?search_query=") + query;
            return new Dictionary<string, object> { { "label", Limit(label, 32) }, { "url", url } };
        }

        // Discord takes strings of 2 to 128 characters (button labels: 32).
        static string Limit(string text, int max = 128)
        {
            text = text.Trim();
            if (text.Length > max) text = text.Substring(0, max - 1) + "…";
            while (text.Length < 2) text += "⠀";
            return text;
        }
    }
}
