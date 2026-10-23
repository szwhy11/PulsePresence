using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Nowcast
{
    // Uploads track thumbnails asynchronously so Discord can display them via Rich Presence.
    // Non-blocking: background worker never pauses or stutters the music polling loop.
    static class Covers
    {
        static readonly HttpClient http = CreateClient();
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        static readonly HashSet<string> inFlight = new HashSet<string>();
        static readonly object gate = new object();

        public static event Action<string, string> CoverReady;

        static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            return client;
        }

        // Returns cached URL if available; otherwise starts background upload and returns null.
        public static string Find(Track track, bool uploadAllowed)
        {
            if (track == null) return null;
            lock (gate)
            {
                string url;
                if (cache.TryGetValue(track.Key, out url) && !url.Contains("catbox.moe")) return url;
                if (!uploadAllowed || track.Thumbnail == null) return null;
                if (inFlight.Contains(track.Key)) return null;

                if (cache.Count > 500) cache.Clear();
                inFlight.Add(track.Key);
            }

            var key = track.Key;
            Task.Factory.StartNew(delegate
            {
                string uploadedUrl = null;
                try
                {
                    var image = Media.ReadThumbnail(track);
                    if (image != null && image.Length > 2)
                    {
                        uploadedUrl = Upload(image);
                    }
                }
                catch (Exception e)
                {
                    Log.Write("Cover upload failed: " + e.Message);
                }
                finally
                {
                    lock (gate)
                    {
                        inFlight.Remove(key);
                        if (uploadedUrl != null) cache[key] = uploadedUrl;
                    }
                }

                if (uploadedUrl != null)
                {
                    Log.Write("Cover uploaded: " + uploadedUrl);
                    var handler = CoverReady;
                    if (handler != null) handler(key, uploadedUrl);
                }
            });

            return null;
        }

        // Checks cache or waits briefly (up to maxWaitMs) for upload to finish.
        // Prevents Discord from showing fallback icon on first track update.
        public static string FindOrWait(Track track, bool uploadAllowed, int maxWaitMs)
        {
            if (track == null) return null;
            string cached;
            lock (gate)
            {
                if (cache.TryGetValue(track.Key, out cached) && !cached.Contains("catbox.moe")) return cached;
            }
            if (!uploadAllowed || track.Thumbnail == null) return null;

            Find(track, uploadAllowed);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < maxWaitMs)
            {
                lock (gate)
                {
                    if (cache.TryGetValue(track.Key, out cached)) return cached;
                    if (!inFlight.Contains(track.Key)) break;
                }
                System.Threading.Thread.Sleep(50);
            }

            lock (gate)
            {
                if (cache.TryGetValue(track.Key, out cached)) return cached;
            }
            return null;
        }

        static string Upload(byte[] image)
        {
            var isPng = image[0] == 0x89 && image[1] == 0x50;
            var ext = isPng ? "cover.png" : "cover.jpg";
            var mime = isPng ? "image/png" : "image/jpeg";

            // 1. Primary: FreeImage.host (hosted on iili.io global CDN, unblocked worldwide, flawless in Discord)
            try
            {
                var base64 = Convert.ToBase64String(image);
                using (var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("key", "6d207e02198a847aa98d0a2a901485a5"),
                    new KeyValuePair<string, string>("action", "upload"),
                    new KeyValuePair<string, string>("format", "json"),
                    new KeyValuePair<string, string>("source", base64)
                }))
                {
                    var response = http.PostAsync("https://freeimage.host/api/1/upload", content).Result;
                    var text = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(text, "\"url\"\\s*:\\s*\"(https:[^\"\\\\]*(?:\\\\.[^\"\\\\]*)*)\"");
                        if (match.Success)
                        {
                            var directUrl = match.Groups[1].Value.Replace("\\/", "/");
                            if (directUrl.StartsWith("https://"))
                            {
                                return directUrl;
                            }
                        }
                    }
                    else
                    {
                        Log.Write("FreeImage.host responded with status " + response.StatusCode);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write("FreeImage.host upload attempt failed: " + e.Message);
            }

            // 2. Fallback: Litterbox
            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StringContent("fileupload"), "reqtype");
                    form.Add(new StringContent("72h"), "time");
                    var file = new ByteArrayContent(image);
                    file.Headers.ContentType = new MediaTypeHeaderValue(mime);
                    form.Add(file, "fileToUpload", ext);
                    var response = http.PostAsync("https://litterbox.catbox.moe/resources/internals/api.php", form).Result;
                    var text = response.Content.ReadAsStringAsync().Result.Trim();
                    if (response.IsSuccessStatusCode && text.StartsWith("https://") && !text.Contains(" "))
                    {
                        return text;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write("Litterbox fallback upload attempt failed: " + e.Message);
            }

            // 3. Fallback: Catbox user API
            using (var form = new MultipartFormDataContent())
            {
                form.Add(new StringContent("fileupload"), "reqtype");
                var file = new ByteArrayContent(image);
                file.Headers.ContentType = new MediaTypeHeaderValue(mime);
                form.Add(file, "fileToUpload", ext);
                var response = http.PostAsync("https://catbox.moe/user/api.php", form).Result;
                var text = response.Content.ReadAsStringAsync().Result.Trim();
                if (!response.IsSuccessStatusCode || !text.StartsWith("https://") || text.Contains(" "))
                {
                    throw new InvalidOperationException("all cover hosts failed: " + text);
                }
                return text;
            }
        }
    }
}
