using Fast_Jira.api;
using Fast_Jira.Models;
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
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fast_Jira.core
{
    public class DataVault
    {
        const string CacheFolder = "./Cache";
        const string SessionFolder = "./Cache/Session";
        const string AvatarFolder = "./Cache/Avatars";
        const string ThumbnailFolder = "./Cache/Thumbnails";
        const string IssuesFolder = "./Cache/Issues";

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly JiraAPI JiraAPI = new JiraAPI(new ClientCredentials(), new HttpClientHandler(), new DelegatingHandler[0]);

        private Issue displayedIssue;

        private readonly ConcurrentDictionary<string, CachedImage> CachedImages = new ConcurrentDictionary<string, CachedImage>();
        private readonly ConcurrentDictionary<string, Issue> CachedIssues = new ConcurrentDictionary<string, Issue>();
        private readonly ConcurrentDictionary<string, Task<Issue>> PrefetchIssues = new ConcurrentDictionary<string, Task<Issue>>();
        private readonly SearchEngine SearchEngine = new SearchEngine();

        public Issue DisplayedIssue
        {
            get => displayedIssue;
            set
            {
                if (displayedIssue == value)
                {
                    return;
                }
                displayedIssue = value;
                if (value == null)
                {
                    return;
                }
                if (!displayedIssue.DisplayInHistory)
                {
                    displayedIssue.DisplayInHistory = true;
                    WriteIssueToDisk(displayedIssue);
                }

                string jsonString = JsonSerializer.Serialize(displayedIssue);
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

        private void ReadCachedIssues()
        {
            foreach (string Filename in Directory.EnumerateFiles(IssuesFolder))
            {
                string jsonString = File.ReadAllText(Filename);
                Issue Issue = JsonSerializer.Deserialize<Issue>(jsonString);
                if (CachedIssues.ContainsKey(Issue.Key))
                {
                    continue;
                }

                CachedIssues[Issue.Key] = Issue;
            }
        }

        public IEnumerable<Issue> GetAllIssuesSorted(bool HistoryOnly = true)
        {
            return CachedIssues.Values.Where(i => !HistoryOnly || i.DisplayInHistory).OrderByDescending(i => i.LastAccess);
        }

        private void ReadCachedImages()
        {
            string imagesFile = CacheFolder + "/images.json";
            if (File.Exists(imagesFile))
            {
                string jsonString = File.ReadAllText(imagesFile);
                var References = JsonSerializer.Deserialize<Dictionary<string, CachedImage>>(jsonString);
                foreach (var Pair in References)
                {
                    if (CachedImages.ContainsKey(Pair.Key) || !File.Exists(Pair.Value.Path))
                    {
                        continue;
                    }

                    CachedImages[Pair.Key] = new CachedImage()
                    {
                        Data = null,
                        Path = Pair.Value.Path
                    };
                }
            }
        }

        public void InitFromDisk()
        {
            logger.Debug("Reading data cache from disk...");
            Stopwatch Watch = Stopwatch.StartNew();
            ReadCachedImages();
            ReadCachedIssues();
            displayedIssue = ReadDisplayedIssue();
            Task.Run(() => InitSearchEngine());
            logger.Debug("Data cache initialized in {0}ms.", Watch.ElapsedMilliseconds);
        }

        private async ValueTask InitSearchEngine()
        {
            foreach (var Issue in CachedIssues.Values)
            {
                string text = Issue.ToFulltextDocument();
                await SearchEngine.AddToIndex(Issue.Key, text);
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

        public List<Issue> SearchIssues(string SearchText)
        {
            var issues = new List<Issue>();
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return issues;
            }
            foreach (SearchResult<string> result in SearchEngine.Search(SearchText))
            {
                if (HasIssueCached(result.Key))
                {
                    issues.Add(GetCachedIssue(result.Key));
                }
            }
            return issues;
        }

        public bool HasImageCached(string URL)
        {
            return string.IsNullOrWhiteSpace(URL) || CachedImages.ContainsKey(URL);
        }

        public bool HasIssueCached(string ID)
        {
            return CachedIssues.ContainsKey(ID);
        }

        public Issue GetCachedIssue(string ID)
        {
            return CachedIssues[ID];
        }

        public Image GetCachedImage(string URL)
        {
            if (string.IsNullOrWhiteSpace(URL) || !CachedImages.ContainsKey(URL) || !File.Exists(CachedImages[URL].Path))
            {
                return null;
            }

            if (CachedImages[URL].Data == null)
            {
                CachedImages[URL].Data = Image.FromFile(CachedImages[URL].Path);
            }
            return CachedImages[URL].Data;
        }

        public ImageSource GetWrappedImage(string URL)
        {
            Image Input = GetCachedImage(URL);
            if (Input == null)
            {
                return null;
            }

            // must be done in the ui thread, because otherwise the image source is not accessible from the ui thread
            using var ms = new MemoryStream();
            Input.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            var BitmapImage = new BitmapImage();
            BitmapImage.BeginInit();
            BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            BitmapImage.StreamSource = ms;
            BitmapImage.EndInit();

            return BitmapImage;
        }

        public void PrefetchIssue(string issueName)
        {
            if (string.IsNullOrWhiteSpace(issueName) || issueName.Length > 30 || HasIssueCached(issueName) || PrefetchIssues.ContainsKey(issueName))
            {
                return;
            }
            // there is a chance for a race condition here, but I don't care.
            Task<Issue> LoadingTask = new Task<Issue>(() => LoadIssue(issueName, true));
            PrefetchIssues[issueName] = LoadingTask;
            logger.Debug("Trying to prefetch issue {0}", issueName);

            try
            {
                LoadingTask.RunSynchronously();
            }
            finally
            {
                PrefetchIssues.Remove(issueName, out LoadingTask);
            }
        }

        public Issue LoadIssue(string IssueID, bool IsPrefetch = false)
        {
            if (!IsPrefetch && PrefetchIssues.ContainsKey(IssueID))
            {
                Task<Issue> PrefetchTask = PrefetchIssues[IssueID];
                PrefetchTask.Wait();
                if (PrefetchTask.IsCompletedSuccessfully)
                {
                    return PrefetchTask.Result;
                }
                else
                {
                    throw PrefetchTask.Exception;
                }
            }

            Stopwatch Watch = Stopwatch.StartNew();
            logger.Debug("Starting to load issue {0}...", IssueID);
            var IssueTask = JiraAPI.GetIssueWithHttpMessagesAsync(IssueID, Issue.UsedFields);
            if (!IssueTask.Wait(3000))
            {
                throw new TimeoutException("Request did not finish in time");
            }
            HttpOperationResponse<IssueBean> Response = IssueTask.Result;
            if (!Response.Response.IsSuccessStatusCode)
            {
                if (!IsPrefetch)
                {
                    logger.Error("Request for issue {0} failed. {1}", IssueID, Response.Response);
                }
                throw new HttpRequestException("Request failed. StatusCode: " + Response.Response.StatusCode);
            }
            long CoreTime = Watch.ElapsedMilliseconds;
            Watch.Restart();
            IssueBean Bean = Response.Body;
            Issue NewIssue = Issue.FromBean(Bean);
            LoadAllResourcesForIssue(NewIssue);
            AddCachedIssue(IssueID, NewIssue);

            long ResourceTime = Watch.ElapsedMilliseconds;
            logger.Debug("Issue {0} loaded in {1}ms ({2}ms base request + {3}ms resources)", IssueID, CoreTime + ResourceTime, CoreTime, ResourceTime);
            return NewIssue;
        }

        private void LoadImage(string URL, string FileName, string TargetFolder)
        {
            logger.Error("Starting to load image from {0}", URL);
            var AsyncTask = JiraAPI.HttpClient.GetAsync(URL);
            if (!AsyncTask.Wait(3500))
            {
                // TODO: add to config
                logger.Error("Timeout while trying to load image from {0}", URL);
                throw new TimeoutException("Unable to load image " + FileName);
            }
            HttpResponseMessage Response = AsyncTask.Result;
            if (!Response.IsSuccessStatusCode)
            {
                logger.Error("Unable to load image {0}: {1}", URL, Response);
                return;
            }

            Image Result;
            var ContentType = Response.Content.Headers.GetValues("Content-Type");
            if (ContentType.Any(Type => Regex.IsMatch(Type, "image/svg.*")))
            {
                string SVG = Response.Content.ReadAsStringAsync().Result;
                SvgDocument Document = SvgDocument.FromSvg<SvgDocument>(SVG);
                Result = Document.Draw();
            }
            else if (ContentType.Any(Type => Regex.IsMatch(Type, "image/(png|jpg|jpeg|bmp).*")))
            {
                Stream Body = Response.Content.ReadAsStreamAsync().Result;
                Result = Image.FromStream(Body);
            }
            else
            {
                logger.Warn("Unknown content type {0} for image from {1}", ContentType, URL);
                return;
            }

            string FilePath = TargetFolder + "/" + FileName + ".png";
            Result.Save(FilePath, ImageFormat.Png);

            CachedImages[URL] = new CachedImage()
            {
                Data = Result,
                Path = FilePath
            };
            lock (this)
            {
                File.WriteAllText(CacheFolder + "/images.json", JsonSerializer.Serialize(CachedImages));
            }
        }

        public void AddCachedIssue(string issueID, Issue issue)
        {
            issue.LastAccess = DateTime.Now;
            CachedIssues[issueID] = issue;
            WriteIssueToDisk(issue);
        }

        private void WriteIssueToDisk(Issue issue)
        {
            lock (this)
            {
                File.WriteAllText(IssuesFolder + "/" + issue.Key + ".json", JsonSerializer.Serialize(issue));
            }
        }

        private void LoadAllResourcesForIssue(Issue NewIssue)
        {
            LoadIssueTypeAvatar(NewIssue.Type);
            LoadIssueTypeAvatar(NewIssue.Priority);
            LoadPersonAvatar(NewIssue.Assignee);
            LoadPersonAvatar(NewIssue.Reporter);
            LoadCommentAvatars(NewIssue.Comments);
            LoadAttachmentThumbnails(NewIssue.Attachments);
        }

        private void LoadAttachmentThumbnails(List<Attachment> Attachments)
        {
            if (Attachments == null)
            {
                return;
            }
            foreach (Attachment attachment in Attachments)
            {
                LoadThumbnail(attachment.Thumbnail);
            }
        }

        private void LoadCommentAvatars(List<Comment> Comments)
        {
            if (Comments == null)
            {
                return;
            }
            foreach (Comment c in Comments)
            {
                LoadPersonAvatar(c.Author);
            }
        }

        private void LoadPersonAvatar(Person Input)
        {
            string AvatarUrl = Input?.AvatarUrl;
            if (!HasImageCached(AvatarUrl))
            {
                LoadImage(AvatarUrl, Input?.Key, AvatarFolder);
            }
        }

        private void LoadThumbnail(string Url)
        {
            if (Url == null)
            {
                return;
            }
            string FileName = new Uri(Url).Segments[^1];
            if (!HasImageCached(Url))
            {
                LoadImage(Url, FileName, ThumbnailFolder);
            }
        }

        private void LoadIssueTypeAvatar(IssueType Type)
        {
            string IconUrl = Type?.IconUrl;
            if (!HasImageCached(IconUrl))
            {
                LoadImage(IconUrl, Type?.Name + "-" + Type?.IconID, AvatarFolder);
            }
        }

        class CachedImage
        {
            public string Path { get; set; }

            [JsonIgnore]
            public Image Data { get; set; }
        }
    }
}
