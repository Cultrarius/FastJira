using FastJira.api;
using FastJira.Models;
using Lifti;
using Microsoft.Rest;
using Svg;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastJira.core
{
    public class DataVault
    {
        private const string CacheFolder = "./Cache";
        private const string SessionFolder = "./Cache/Session";
        private const string AvatarFolder = "./Cache/Avatars";
        private const string ThumbnailFolder = "./Cache/Thumbnails";
        private const string IssuesFolder = "./Cache/Issues";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public JiraAPI JiraApi;

        private Issue _displayedIssue;

        private readonly ConcurrentDictionary<string, CachedImage> _cachedImages = new ConcurrentDictionary<string, CachedImage>();
        private readonly ConcurrentDictionary<string, Issue> _cachedIssues = new ConcurrentDictionary<string, Issue>();
        private readonly ConcurrentDictionary<string, Task<Issue>> _prefetchIssues = new ConcurrentDictionary<string, Task<Issue>>();
        private readonly SearchEngine _searchEngine = new SearchEngine();

        public Issue DisplayedIssue
        {
            get => _displayedIssue;
            set
            {
                if (_displayedIssue == value)
                {
                    return;
                }
                _displayedIssue = value;
                if (value == null)
                {
                    return;
                }
                if (!_displayedIssue.DisplayInHistory)
                {
                    _displayedIssue.DisplayInHistory = true;
                    WriteIssueToDisk(_displayedIssue);
                }

                string jsonString = JsonSerializer.Serialize(_displayedIssue);
                lock (this)
                {
                    File.WriteAllText(SessionFolder + "/displayedIssue.json", jsonString);
                }
            }
        }

        public DataVault()
        {
            Directory.CreateDirectory(CacheFolder);
            Directory.CreateDirectory(SessionFolder);
            Directory.CreateDirectory(AvatarFolder);
            Directory.CreateDirectory(ThumbnailFolder);
            Directory.CreateDirectory(IssuesFolder);
        }

        public void ConfigureApi(Config config)
        {
            var clientHandler = new HttpClientHandler();
            if (!string.IsNullOrWhiteSpace(config.ProxyServer))
            {
                var proxy = new WebProxy(new Uri(config.ProxyServer));
                if (!string.IsNullOrWhiteSpace(config.ProxyUser))
                {
                    proxy.Credentials = new NetworkCredential(config.ProxyUser, config.ProxyPassword);
                }
                clientHandler.Proxy = proxy;
            }

            var clientCredentials = new ClientCredentials {Username = config.JiraUser, Password = config.JiraPassword};
            JiraApi = new JiraAPI(new Uri(config.JiraServer), clientCredentials, clientHandler);
        }

        private void ReadCachedIssues()
        {
            foreach (string filename in Directory.EnumerateFiles(IssuesFolder))
            {
                string jsonString = File.ReadAllText(filename);
                Issue issue = JsonSerializer.Deserialize<Issue>(jsonString);
                if (_cachedIssues.ContainsKey(issue.Key))
                {
                    continue;
                }

                _cachedIssues[issue.Key] = issue;
            }
        }

        public IEnumerable<Issue> GetAllIssuesSorted(bool historyOnly = true)
        {
            return _cachedIssues.Values.Where(i => !historyOnly || i.DisplayInHistory).OrderByDescending(i => i.LastAccess);
        }

        private void ReadCachedImages()
        {
            string imagesFile = CacheFolder + "/images.json";
            if (File.Exists(imagesFile))
            {
                string jsonString = File.ReadAllText(imagesFile);
                var references = JsonSerializer.Deserialize<Dictionary<string, CachedImage>>(jsonString);
                foreach (var pair in references)
                {
                    if (_cachedImages.ContainsKey(pair.Key) || !File.Exists(pair.Value.Path))
                    {
                        continue;
                    }

                    _cachedImages[pair.Key] = new CachedImage()
                    {
                        Data = null,
                        Path = pair.Value.Path
                    };
                }
            }
        }

        public void InitFromDisk()
        {
            Logger.Debug("Reading data cache from disk...");
            var watch = Stopwatch.StartNew();
            ReadCachedImages();
            ReadCachedIssues();
            _displayedIssue = ReadDisplayedIssue();
            Task.Run(InitSearchEngine);
            Logger.Debug("Data cache initialized in {0}ms.", watch.ElapsedMilliseconds);
        }

        private async ValueTask InitSearchEngine()
        {
            foreach (var issue in _cachedIssues.Values)
            {
                string text = issue.ToFulltextDocument();
                await _searchEngine.AddToIndex(issue.Key, text);
            }
        }

        private Issue ReadDisplayedIssue()
        {
            string fileName = SessionFolder + "/displayedIssue.json";
            if (File.Exists(fileName))
            {
                string jsonString = File.ReadAllText(fileName);
                return JsonSerializer.Deserialize<Issue>(jsonString);
            }
            return null;
        }

        public List<Issue> SearchIssues(string searchText)
        {
            var issues = new List<Issue>();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return issues;
            }

            foreach (SearchResult<string> result in _searchEngine.Search(searchText))
            {
                if (HasIssueCached(result.Key))
                {
                    issues.Add(GetCachedIssue(result.Key));
                }
            }
            return issues;
        }

        public bool HasImageCached(string url)
        {
            return string.IsNullOrWhiteSpace(url) || _cachedImages.ContainsKey(url);
        }

        public bool HasIssueCached(string id)
        {
            return _cachedIssues.ContainsKey(id);
        }

        public Issue GetCachedIssue(string id)
        {
            return _cachedIssues[id];
        }

        public Image GetCachedImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !_cachedImages.ContainsKey(url) || !File.Exists(_cachedImages[url].Path))
            {
                return null;
            }

            return _cachedImages[url].Data ?? (_cachedImages[url].Data = Image.FromFile(_cachedImages[url].Path));
        }

        public ImageSource GetWrappedImage(string url)
        {
            Image input = GetCachedImage(url);
            if (input == null)
            {
                return null;
            }

            // must be done in the ui thread, because otherwise the image source is not accessible from the ui thread
            using var ms = new MemoryStream();
            input.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        public void PrefetchIssue(string issueName)
        {
            if (string.IsNullOrWhiteSpace(issueName) || issueName.Length > 30 || HasIssueCached(issueName) || _prefetchIssues.ContainsKey(issueName))
            {
                return;
            }
            // there is a chance for a race condition here, but I don't care.
            Task<Issue> loadingTask = new Task<Issue>(() => LoadIssue(issueName, true));
            _prefetchIssues[issueName] = loadingTask;
            Logger.Debug("Trying to prefetch issue {0}", issueName);

            try
            {
                loadingTask.RunSynchronously();
            }
            finally
            {
                _prefetchIssues.Remove(issueName, out loadingTask);
            }
        }

        public Issue LoadIssue(string issueId, bool isPrefetch = false)
        {
            if (!isPrefetch && _prefetchIssues.ContainsKey(issueId))
            {
                Task<Issue> prefetchTask = _prefetchIssues[issueId];
                prefetchTask.Wait();
                if (prefetchTask.IsCompletedSuccessfully)
                {
                    return prefetchTask.Result;
                }

                throw prefetchTask.Exception;
            }

            var watch = Stopwatch.StartNew();
            Logger.Debug("Starting to load issue {0}...", issueId);
            var issueTask = JiraApi.GetIssueWithHttpMessagesAsync(issueId, Issue.UsedFields);
            if (!issueTask.Wait(3000))
            {
                throw new TimeoutException("Request did not finish in time");
            }
            var response = issueTask.Result;
            if (!response.Response.IsSuccessStatusCode)
            {
                if (!isPrefetch)
                {
                    Logger.Error("Request for issue {0} failed. {1}", issueId, response.Response);
                }
                throw new HttpRequestException("Request failed. StatusCode: " + response.Response.StatusCode);
            }
            long coreTime = watch.ElapsedMilliseconds;
            watch.Restart();
            IssueBean bean = response.Body;
            Issue newIssue = Issue.FromBean(bean);
            LoadAllResourcesForIssue(newIssue);
            AddCachedIssue(issueId, newIssue);

            long resourceTime = watch.ElapsedMilliseconds;
            Logger.Debug("Issue {0} loaded in {1}ms ({2}ms base request + {3}ms resources)", issueId, coreTime + resourceTime, coreTime, resourceTime);
            return newIssue;
        }

        private void LoadImage(string url, string fileName, string targetFolder)
        {
            Logger.Error("Starting to load image from {0}", url);
            var asyncTask = JiraApi.HttpClient.GetAsync(url);
            if (!asyncTask.Wait(3500))
            {
                // TODO: add to config
                Logger.Error("Timeout while trying to load image from {0}", url);
                throw new TimeoutException("Unable to load image " + fileName);
            }
            HttpResponseMessage response = asyncTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                Logger.Error("Unable to load image {0}: {1}", url, response);
                return;
            }

            Image result;
            var typeList = response.Content.Headers.GetValues("Content-Type").ToList();
            if (typeList.Any(type => Regex.IsMatch(type, "image/svg.*")))
            {
                string svg = response.Content.ReadAsStringAsync().Result;
                SvgDocument document = SvgDocument.FromSvg<SvgDocument>(svg);
                result = document.Draw();
            }
            else if (typeList.Any(type => Regex.IsMatch(type, "image/(png|jpg|jpeg|bmp).*")))
            {
                Stream body = response.Content.ReadAsStreamAsync().Result;
                result = Image.FromStream(body);
            }
            else
            {
                Logger.Warn("Unknown content type {0} for image from {1}", typeList, url);
                return;
            }

            string filePath = targetFolder + "/" + fileName + ".png";
            result.Save(filePath, ImageFormat.Png);

            _cachedImages[url] = new CachedImage()
            {
                Data = result,
                Path = filePath
            };
            lock (this)
            {
                File.WriteAllText(CacheFolder + "/images.json", JsonSerializer.Serialize(_cachedImages));
            }
        }

        public void AddCachedIssue(string issueId, Issue issue)
        {
            issue.LastAccess = DateTime.Now;
            _cachedIssues[issueId] = issue;
            WriteIssueToDisk(issue);
        }

        private void WriteIssueToDisk(Issue issue)
        {
            lock (this)
            {
                File.WriteAllText(IssuesFolder + "/" + issue.Key + ".json", JsonSerializer.Serialize(issue));
            }
        }

        private void LoadAllResourcesForIssue(Issue newIssue)
        {
            LoadIssueTypeAvatar(newIssue.Type);
            LoadIssueTypeAvatar(newIssue.Priority);
            LoadPersonAvatar(newIssue.Assignee);
            LoadPersonAvatar(newIssue.Reporter);
            LoadCommentAvatars(newIssue.Comments);
            LoadAttachmentThumbnails(newIssue.Attachments);
        }

        private void LoadAttachmentThumbnails(List<Attachment> attachments)
        {
            if (attachments == null)
            {
                return;
            }
            foreach (Attachment attachment in attachments)
            {
                LoadThumbnail(attachment.Thumbnail);
            }
        }

        private void LoadCommentAvatars(List<Comment> comments)
        {
            if (comments == null)
            {
                return;
            }
            foreach (Comment c in comments)
            {
                LoadPersonAvatar(c.Author);
            }
        }

        private void LoadPersonAvatar(Person input)
        {
            string avatarUrl = input?.AvatarUrl;
            if (!HasImageCached(avatarUrl))
            {
                LoadImage(avatarUrl, input?.Key, AvatarFolder);
            }
        }

        private void LoadThumbnail(string url)
        {
            if (url == null)
            {
                return;
            }
            string fileName = new Uri(url).Segments[^1];
            if (!HasImageCached(url))
            {
                LoadImage(url, fileName, ThumbnailFolder);
            }
        }

        private void LoadIssueTypeAvatar(IssueType type)
        {
            string iconUrl = type?.IconUrl;
            if (!HasImageCached(iconUrl))
            {
                LoadImage(iconUrl, type?.Name + "-" + type?.IconId, AvatarFolder);
            }
        }

        private class CachedImage
        {
            public string Path { get; set; }

            [JsonIgnore]
            public Image Data { get; set; }
        }
    }
}
