using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace SmartPulseTask.App
{
    public static class AuthService
    {
        private const string TgtUrl = "https://giris.epias.com.tr/cas/v1/tickets";
        private const string CacheFile = ".tgt_cache.json";

        public static async Task<string?> GetTGTAsync(string username, string password)
        {
            // Try to get a cached TGT
            string? cachedTgt = GetCachedTGT();
            if (!string.IsNullOrEmpty(cachedTgt))
            {
                Console.WriteLine("Using cached TGT (still valid, <2 hours old).");
                return cachedTgt;
            }

            // Otherwise, request a new TGT from EPİAŞ
            using var client = new HttpClient();

            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            try
            {
                var response = await client.PostAsync(TgtUrl, data);
                var html = await response.Content.ReadAsStringAsync();

                const string marker = "/cas/v1/tickets/";
                var start = html.IndexOf(marker, StringComparison.Ordinal);
                if (start == -1)
                {
                    Console.WriteLine("Could not parse TGT.");
                    return null;
                }

                start += marker.Length;
                var end = html.IndexOf("\"", start, StringComparison.Ordinal);
                var tgt = html.Substring(start, end - start);

                // Save new TGT to cache with timestamp
                SaveTGTToCache(tgt);

                return tgt;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception while retrieving TGT: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        private static string? GetCachedTGT()
        {
            try
            {
                if (!File.Exists(CacheFile))
                    return null;

                string json = File.ReadAllText(CacheFile);
                var cache = JsonSerializer.Deserialize<TgtCache>(json);

                if (cache == null || string.IsNullOrEmpty(cache.TGT))
                    return null;

                var age = DateTime.Now - cache.CreatedAt;
                if (age.TotalHours < 2)
                    return cache.TGT;

                // expired, remove old cache
                File.Delete(CacheFile);
                return null;
            }
            catch
            {
                // If any parsing or IO error, just skip cache
                return null;
            }
        }

        private static void SaveTGTToCache(string tgt)
        {
            try
            {
                var cache = new TgtCache
                {
                    TGT = tgt,
                    CreatedAt = DateTime.Now
                };

                string json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CacheFile, json);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Cached new TGT for 2 hours.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Could not save TGT cache: {ex.Message}");
                Console.ResetColor();
            }
        }

        private class TgtCache
        {
            public string? TGT { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
