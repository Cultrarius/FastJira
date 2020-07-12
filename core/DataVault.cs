using Fast_Jira.api;
using Fast_Jira.Models;
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

namespace Fast_Jira.core
{
    public class DataVault
    {
        const string CacheFolder = "./Cache";
        const string SessionFolder = "./Cache/Session";
        const string AvatarFolder = "./Cache/Avatars";
        const string ThumbnailFolder = "./Cache/Thumbnails";

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly JiraAPI JiraAPI = new JiraAPI(new ClientCredentials(), new HttpClientHandler(), new DelegatingHandler[0]);

        private Issue displayedIssue;

        private readonly ConcurrentDictionary<string, CachedImage> CachedImages = new ConcurrentDictionary<string, CachedImage>();
        private readonly ConcurrentDictionary<string, Issue> CachedIssues = new ConcurrentDictionary<string, Issue>();

        public Issue DisplayedIssue
        {
            get => displayedIssue;
            set
            {
                if (displayedIssue != value)
                {
                    displayedIssue = value;
                    WriteDisplayedIssue();
                }
            }
        }

        public DataVault()
        {
            Directory.CreateDirectory(CacheFolder);
            Directory.CreateDirectory(SessionFolder);
            Directory.CreateDirectory(AvatarFolder);
            Directory.CreateDirectory(ThumbnailFolder);
        }

        private void ReadCachedIssues()
        {
            string issuesFile = CacheFolder + "/issues.json";
            if (File.Exists(issuesFile))
            {
                string jsonString = File.ReadAllText(issuesFile);
                var References = JsonSerializer.Deserialize<Dictionary<string, Issue>>(jsonString);
                foreach (var Pair in References)
                {
                    if (CachedIssues.ContainsKey(Pair.Key))
                    {
                        continue;
                    }

                    CachedIssues[Pair.Key] = Pair.Value;
                }
            }
        }

        public IEnumerable<Issue> GetAllIssuesSorted()
        {
            return CachedIssues.Values.OrderByDescending(i => i.LastAccess);
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
            ReadCachedImages();
            ReadCachedIssues();
            DisplayedIssue = ReadDisplayedIssue();
            logger.Debug("Data cache initialized.");
        }

        public void WriteCacheMetadataToDisk()
        {
            lock (this)
            {
                File.WriteAllText(CacheFolder + "/images.json", JsonSerializer.Serialize(CachedImages));
                File.WriteAllText(CacheFolder + "/issues.json", JsonSerializer.Serialize(CachedIssues));
                WriteDisplayedIssue();
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

        private void WriteDisplayedIssue()
        {
            if (DisplayedIssue != null)
            {
                string jsonString = JsonSerializer.Serialize(DisplayedIssue);
                lock (this)
                {
                    File.WriteAllText(SessionFolder + "/displayedIssue.json", jsonString);
                }
            }
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

        public Issue LoadIssue(string IssueID)
        {
            Stopwatch Watch = Stopwatch.StartNew();
            var IssueTask = JiraAPI.GetIssueWithHttpMessagesAsync(IssueID, Issue.UsedFields);
            if (!IssueTask.Wait(3000))
            {
                throw new TimeoutException("Request did not finish in time");
            }
            HttpOperationResponse<IssueBean> Response = IssueTask.Result;
            if (!Response.Response.IsSuccessStatusCode)
            {
                logger.Error("Request for issue {0} failed. {1}", IssueID, Response.Response);
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
            CachedImages[URL] = new CachedImage()
            {
                Data = Result,
                Path = FilePath
            };
            Result.Save(FilePath, ImageFormat.Png);

            WriteCacheMetadataToDisk();
        }

        public void AddCachedIssue(string issueID, Issue issue)
        {
            issue.LastAccess = DateTime.Now;
            CachedIssues[issueID] = issue;
            WriteCacheMetadataToDisk();
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
    }

    class CachedImage
    {
        public string Path { get; set; }

        [JsonIgnore]
        public Image Data { get; set; }
    }
}
