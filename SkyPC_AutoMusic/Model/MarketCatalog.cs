using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SkyPC_AutoMusic.Model
{
    //在线曲库（GitHub: Ai-Vonie/Sky1984-Sheets-Collection）的清单与下载
    static class MarketCatalog
    {
        public const string Repository = "Ai-Vonie/Sky1984-Sheets-Collection";
        public const string Branch = "master";

        //只收这三个目录
        private static readonly string[] roots = { "Songs/", "Multi-Sheet Songs/", "VSRG/" };

        private static readonly HttpClient http = CreateHttp();

        //本地缓存目录与清单文件
        public static string MarketDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "market"); }
        }

        public static string CatalogFile
        {
            get { return Path.Combine(MarketDir, "catalog.json"); }
        }

        private static HttpClient CreateHttp()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(180);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SkyPC_AutoMusic");
            return client;
        }

        //从 GitHub 拉取整棵文件树并筛出曲谱
        public static async Task<List<MarketItem>> FetchAsync(string token)
        {
            string url = "https://api.github.com/repos/" + Repository + "/git/trees/" + Branch + "?recursive=1";
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplyToken(request, token);
                using (HttpResponseMessage response = await http.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new Exception("HTTP " + (int)response.StatusCode);

                    string json = await response.Content.ReadAsStringAsync();
                    //4MB 的树解析和写盘丢到后台，别卡界面
                    List<MarketItem> items = await Task.Run(() => ParseTree(json));
                    await Task.Run(() => SaveCache(items));
                    return items;
                }
            }
        }

        private static void ApplyToken(HttpRequestMessage request, string token)
        {
            if (!String.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("token", token.Trim());
        }

        private static List<MarketItem> ParseTree(string json)
        {
            List<MarketItem> items = new List<MarketItem>();
            JObject root = JObject.Parse(json);

            foreach (JToken node in root["tree"] ?? new JArray())
            {
                string type = (string)node["type"];
                string path = (string)node["path"];
                if (type != "blob" || path == null)
                    continue;
                if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!roots.Any(r => path.StartsWith(r, StringComparison.Ordinal)))
                    continue;
                //跳过占位文件
                if (Path.GetFileName(path).StartsWith("."))
                    continue;

                string[] segments = path.Split('/');
                items.Add(new MarketItem
                {
                    Path = path,
                    Name = Path.GetFileNameWithoutExtension(path),
                    Group = segments.Length > 0 ? segments[0] : String.Empty,
                    Sub = segments.Length > 1 ? segments[1] : String.Empty,
                    Size = (long?)node["size"] ?? 0
                });
            }

            return items;
        }

        //读取本地缓存清单
        public static List<MarketItem> LoadCache(out DateTime fetchedAt)
        {
            fetchedAt = DateTime.MinValue;
            if (!File.Exists(CatalogFile))
                return null;

            CatalogCache cache = JsonConvert.DeserializeObject<CatalogCache>(File.ReadAllText(CatalogFile));
            if (cache == null || cache.items == null)
                return null;

            fetchedAt = cache.fetchedAt;
            return cache.items.Select(x => new MarketItem
            {
                Path = x.path,
                Name = Path.GetFileNameWithoutExtension(x.path),
                Group = x.path.Split('/')[0],
                Sub = x.path.Split('/').Length > 1 ? x.path.Split('/')[1] : String.Empty,
                Size = x.size
            }).ToList();
        }

        private static void SaveCache(List<MarketItem> items)
        {
            Directory.CreateDirectory(MarketDir);
            CatalogCache cache = new CatalogCache
            {
                fetchedAt = DateTime.Now,
                items = items.Select(x => new CacheItem { path = x.Path, size = x.Size }).ToList()
            };
            File.WriteAllText(CatalogFile, JsonConvert.SerializeObject(cache));
        }

        //原文件下载地址（raw CDN，不计 API 限额）
        public static string RawUrl(string path)
        {
            string escaped = String.Join("/", path.Split('/').Select(Uri.EscapeDataString));
            return "https://raw.githubusercontent.com/" + Repository + "/" + Branch + "/" + escaped;
        }

        //仓库路径 -> 本地文件路径
        public static string LocalPath(string path)
        {
            string relative = path.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(MarketDir, relative);
        }

        //下载单曲到本地（已存在则跳过）
        public static async Task<string> DownloadAsync(MarketItem item, string token)
        {
            string local = LocalPath(item.Path);
            if (File.Exists(local))
            {
                item.IsDownloaded = true;
                return local;
            }

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, RawUrl(item.Path)))
            {
                ApplyToken(request, token);
                using (HttpResponseMessage response = await http.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                    Directory.CreateDirectory(Path.GetDirectoryName(local));
                    File.WriteAllBytes(local, bytes);
                }
            }

            item.IsDownloaded = true;
            return local;
        }

        //刷新本地文件的已下载标记
        public static void RefreshDownloadedFlags(IEnumerable<MarketItem> items)
        {
            foreach (MarketItem item in items)
            {
                item.LocalPath = LocalPath(item.Path);
                item.IsDownloaded = File.Exists(item.LocalPath);
            }
        }

        private class CatalogCache
        {
            public DateTime fetchedAt { get; set; }
            public List<CacheItem> items { get; set; }
        }

        private class CacheItem
        {
            public string path { get; set; }
            public long size { get; set; }
        }
    }
}
