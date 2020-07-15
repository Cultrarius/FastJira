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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog.Fluent;

namespace FastJira.core
{
    public class DataVault
    {
        private const string CacheFolder = "./Cache";
        private const string SessionFolder = "./Cache/Session";
        private const string AvatarFolder = "./Cache/Avatars";
        private const string ThumbnailFolder = "./Cache/Thumbnails";
        private const string IssuesFolder = "./Cache/Issues";
        private const string SearchFolder = "./Cache/Search";
        private const string AttachmentsFolder = "./Cache/Attachments";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public JiraAPI JiraApi { get; set; }

        private Issue _displayedIssue;
        private SearchIndexData _searchIndexData;

        private readonly ConcurrentDictionary<string, CachedImage> _cachedImages = new ConcurrentDictionary<string, CachedImage>();
        private readonly ConcurrentDictionary<string, Issue> _cachedIssues = new ConcurrentDictionary<string, Issue>();
        private readonly ConcurrentDictionary<string, Issue> _searchableIssues = new ConcurrentDictionary<string, Issue>();
        private readonly ConcurrentDictionary<string, Task<Issue>> _prefetchIssues = new ConcurrentDictionary<string, Task<Issue>>();
        private readonly ConcurrentDictionary<Uri, bool> _activeDownloads = new ConcurrentDictionary<Uri, bool>();
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
            Directory.CreateDirectory(SearchFolder);
            Directory.CreateDirectory(AttachmentsFolder);
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

            var clientCredentials = new ClientCredentials { Username = config.JiraUser, Password = config.JiraPassword };
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
            Logger.Info("Reading data cache from disk...");
            var watch = Stopwatch.StartNew();
            ReadCachedImages();
            ReadCachedIssues();
            _displayedIssue = ReadDisplayedIssue();
            _searchIndexData = ReadSearchIndexSessionData();
            Logger.Debug("Data cache initialized in {0}ms.", watch.ElapsedMilliseconds);
        }

        public async void IndexServerForSearch(Config config)
        {
            try
            {
                Logger.Info("Starting full-text search update from server...");
                var watch = Stopwatch.StartNew();

                var projects = new HashSet<string>();
                foreach (Issue issue in _cachedIssues.Values)
                {
                    if (issue.Project?.Key != null)
                    {
                        projects.Add("'" + issue.Project.Key + "'");
                    }
                }
                if (projects.Count == 0)
                {
                    Logger.Info("Skipping full-text search update since there are no known projects yet.");
                    return;
                }
                string projectsFilter = string.Join(",", projects);

                int daysSinceLastUpdate = 14; // TODO: make configurable
                if (_searchIndexData != null)
                {
                    daysSinceLastUpdate = Math.Clamp((int)Math.Ceiling(DateTime.Now.Subtract(_searchIndexData.LastUpdateTime).TotalDays), 0, daysSinceLastUpdate);
                }

                if (daysSinceLastUpdate <= 1)
                {
                    Logger.Info("Skipping full-text search update since the cached data is still fresh.");
                    return;
                }
                Logger.Info("Indexing issues updated in the last {0} days from projects: {1}", daysSinceLastUpdate, projectsFilter);

                string jql = "project in (" + projectsFilter + ") and updated >= -" + daysSinceLastUpdate + "d";
                using var response = SearchIssues(jql, 0);
                SearchResults initialResult = response.Body;
                Logger.Debug("{0} issues found for full-text indexing, starting batched download...", initialResult.Total);

                await AddToSearchIndex(initialResult.Issues);
                int seenIssues = initialResult.Issues.Count;
                int missing = (initialResult.Total ?? 0) - seenIssues;
                while (missing > 0)
                {
                    Logger.Debug("{0} issues to download...", missing);
                    using var pagedResponse = SearchIssues(jql, seenIssues);
                    SearchResults pagedResult = pagedResponse.Body;
                    if (pagedResult.Issues.Count == 0) break;
                    await AddToSearchIndex(pagedResult.Issues);
                    missing -= pagedResult.Issues.Count;
                    seenIssues += pagedResult.Issues.Count;
                }

                _searchIndexData = new SearchIndexData()
                {
                    LastUpdateTime = DateTime.Now
                };
                lock (this)
                {
                    File.WriteAllText(SessionFolder + "/searchIndex.json", JsonSerializer.Serialize(_searchIndexData));
                }
                Logger.Debug("Downloading search data completed in {0}s.", watch.ElapsedMilliseconds / 1000);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in search index task!");
            }
        }

        private HttpOperationResponse<SearchResults> SearchIssues(string jql, int startAt)
        {
            // TODO: make configurable
            var issueTask = JiraApi.SearchIssuesWithHttpMessagesAsync(jql, startAt, 100, Issue.SearchableFields);
            if (!issueTask.Wait(5000))
            {
                throw new TimeoutException("Request did not finish in time");
            }

            if (issueTask.Result.Response.IsSuccessStatusCode)
            {
                return issueTask.Result;
            }
            Logger.Error("Request for server indexing failed. {1}", issueTask.Result.Response);
            throw new HttpOperationException("Issue search request failed");
        }

        private async ValueTask AddToSearchIndex(IList<IssueBean> issueBeans)
        {
            List<Issue> issues = new List<Issue>();
            foreach (var issueBean in issueBeans)
            {
                var issue = Issue.FromBean(issueBean);
                if (_cachedIssues.ContainsKey(issue.Key)) continue; // the cached issue data is probably newer
                issues.Add(issue);
                lock (this)
                {
                    File.WriteAllText(SearchFolder + "/" + issue.Key + ".json", JsonSerializer.Serialize(issue));
                }
                _searchableIssues[issue.Key] = issue;
            }

            await _searchEngine.AddToIndex(issues);
        }

        public async ValueTask InitSearchEngine()
        {
            try
            {
                Logger.Info("Initializing full-text search...");
                var watch = Stopwatch.StartNew();

                List<Issue> issuesToIndex = new List<Issue>();
                Stopwatch sw = Stopwatch.StartNew();
                foreach (string filename in Directory.EnumerateFiles(SearchFolder))
                {
                    if (!filename.EndsWith(".json")) continue;
                    string searchJson = File.ReadAllText(filename);
                    Issue issue = JsonSerializer.Deserialize<Issue>(searchJson);
                    issuesToIndex.Add(issue);
                    _searchableIssues[issue.Key] = issue;
                }
                Logger.Debug("Deserialized {0} search documents in {1}ms.", issuesToIndex.Count, sw.ElapsedMilliseconds);
                issuesToIndex.AddRange(_cachedIssues.Values);

                await _searchEngine.AddToIndex(issuesToIndex);
                Logger.Debug("Full-text search initialized in {0}ms.", watch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in full-text search initialization!");
            }
        }

        private static Issue ReadDisplayedIssue()
        {
            string fileName = SessionFolder + "/displayedIssue.json";
            if (File.Exists(fileName))
            {
                string jsonString = File.ReadAllText(fileName);
                return JsonSerializer.Deserialize<Issue>(jsonString);
            }
            return null;
        }

        private static SearchIndexData ReadSearchIndexSessionData()
        {
            string fileName = SessionFolder + "/searchIndex.json";
            if (File.Exists(fileName))
            {
                string jsonString = File.ReadAllText(fileName);
                return JsonSerializer.Deserialize<SearchIndexData>(jsonString);
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

            var stopwatch = Stopwatch.StartNew();
            foreach (SearchResult<string> result in _searchEngine.Search(searchText))
            {
                if (HasIssueCached(result.Key))
                {
                    issues.Add(GetCachedIssue(result.Key));
                }
                else if (_searchableIssues.ContainsKey(result.Key))
                {
                    issues.Add(_searchableIssues[result.Key]);
                }
                else
                {
                    Logger.Warn("Inconsistent cache! Issue " + result.Key +
                                " was found in a search, but is nowhere to be found in the cached data!");
                }
            }
            issues.Sort((a, b) =>
            {
                int compared = b.LastAccess.CompareTo(a.LastAccess);
                if (compared == 0)
                {
                    return string.Compare(a.Key, b.Key, StringComparison.Ordinal);
                }
                return compared;
            });
            Logger.Trace("Full-text search for '{0}' performed in {1}ms.", searchText, stopwatch.ElapsedMilliseconds);
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
            if (!issueTask.Wait(5000))
            {
                throw new TimeoutException("Request did not finish in time");
            }

            using HttpOperationResponse<IssueBean> response = issueTask.Result;
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

        private void LoadFile(Uri uri, string targetPath)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Logger.Debug("Starting to load file from {0}", uri);
            using HttpRequestMessage httpRequest = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = uri
            };
            if (uri.AbsolutePath.StartsWith(JiraApi.BaseUri.AbsolutePath))
            {
                JiraApi.Credentials?.ProcessHttpRequestAsync(httpRequest, CancellationToken.None).Wait();
            }
            using var result = JiraApi.HttpClient.SendAsync(httpRequest).Result;
            
            if (!result.IsSuccessStatusCode)
            {
                Logger.Error("Unable to load file {0}: {1}", uri, result);
                throw new HttpOperationException("Unable to download file (" + result.StatusCode + ")");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            using var content = result.Content.ReadAsStreamAsync().Result;
            using var fs = File.Create(targetPath);
            content.CopyTo(fs);
            Logger.Debug("Downloaded file {0} in {1}ms", uri, sw.ElapsedMilliseconds);
        }

        private void LoadImage(string url, string fileName, string targetFolder)
        {
            Logger.Debug("Starting to load image from {0}", url);

            var asyncTask = JiraApi.HttpClient.GetAsync(new Uri(url));
            if (!asyncTask.Wait(3500))
            {
                // TODO: add to config
                Logger.Error("Timeout while trying to load image from {0}", url);
                throw new TimeoutException("Unable to load image " + fileName);
            }

            using HttpResponseMessage response = asyncTask.Result;

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

        private void AddCachedIssue(string issueId, Issue issue)
        {
            issue.LastAccess = DateTime.Now;
            if (_cachedIssues.ContainsKey(issueId))
            {
                // we're just updating the issue, so use some existing values
                issue.DisplayInHistory = _cachedIssues[issueId].DisplayInHistory;
                issue.DescriptionScrollPosition = _cachedIssues[issueId].DescriptionScrollPosition;
            }
            _cachedIssues[issueId] = issue;
            WriteIssueToDisk(issue);
            Task.Run(() => _searchEngine.AddToIndex(issue));
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
            LoadProjectAvatar(newIssue.Project);
        }

        private void LoadProjectAvatar(Project project)
        {
            if (string.IsNullOrWhiteSpace(project?.AvatarUrl))
            {
                return;
            }

            if (!HasImageCached(project.AvatarUrl))
            {
                LoadImage(project.AvatarUrl, project.Key + "-" + project.Id, AvatarFolder);
            }
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

        public string LoadAttachment(string issueKey, Uri uri)
        {
            var attachmentPath = ToAttachmentPath(issueKey, uri);
            if (!File.Exists(attachmentPath))
            {
                if (_activeDownloads.ContainsKey(uri))
                {
                    throw new OperationCanceledException("Chill out, it's already downloading!");
                }

                _activeDownloads[uri] = true;

                try
                {
                    LoadFile(uri, attachmentPath);
                }
                finally
                {
                    bool tmp;
                    _activeDownloads.Remove(uri, out tmp);
                }
            }

            return attachmentPath;
        }

        private static string ToAttachmentPath(string issueKey, Uri uri)
        {
            return AttachmentsFolder + "/" + issueKey + "/" + uri.Segments[^1];
        }

        private class CachedImage
        {
            public string Path { get; set; }

            [JsonIgnore]
            public Image Data { get; set; }
        }
    }

    public class SearchIndexData
    {
        public DateTime LastUpdateTime { get; set; }
    }
}
