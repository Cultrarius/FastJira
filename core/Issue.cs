using Fast_Jira.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fast_Jira.core
{
    public class Issue
    {
        public const string UsedFields = "resolution,assignee,reporter,issuetype,status,comment,priority,project,attachment,updated,created,description,summary,issuelinks";

        // -------- Set by UI ---------------

        /// <summary>Last known scrollbar position of the description text</summary>
        public double DescriptionScrollPosition { get; set; }

        /// <summary>Last known scrollbar position of the description text</summary>
        public DateTime LastAccess { get; set; }

        /// <summary>This is true only for issues explicitly opened by the user (so prefetched issues are not displayed)</summary>
        public bool DisplayInHistory { get; set; }

        // -------- Values from Jira --------


        /// <summary>Internal Jira ID</summary>
        public string Id { get; set; }

        /// <summary>Issue key as shown in the browser</summary>
        public string Key { get; set; }

        /// <summary>Issue description in markdown format</summary>
        public string Description { get; set; }

        /// <summary>Issue summary displayed on top as the heading</summary>
        public string Summary { get; set; }

        public string Resolution { get; set; }

        public DateTime Updated { get; set; }

        public DateTime Created { get; set; }

        public List<string> IssueLinks { get; set; }

        public List<string> Subtasks { get; set; }

        public List<Comment> Comments { get; set; }

        public List<Attachment> Attachments { get; set; }

        public Person Assignee { get; set; }

        public Person Reporter { get; set; }

        public IssueType Type { get; set; }

        public IssueType Priority { get; set; }

        public IssueStatus Status { get; set; }

        public Project Project { get; set; }

        public Dictionary<string, string> CustomFields { get; set; }

        public static Issue FromBean(IssueBean bean)
        {
            if (bean == null)
            {
                return null;
            }

            JObject resolution = (JObject)bean.Fields["resolution"];
            JObject assignee = (JObject)bean.Fields["assignee"];
            JObject reporter = (JObject)bean.Fields["reporter"];
            JObject issuetype = (JObject)bean.Fields["issuetype"];
            JObject status = (JObject)bean.Fields["status"];
            JObject comment = (JObject)bean.Fields["comment"];
            JObject priority = (JObject)bean.Fields["priority"];
            JObject project = (JObject)bean.Fields["project"];
            JArray attachment = (JArray)bean.Fields["attachment"];
            DateTime updated = (DateTime)bean.Fields["updated"];
            DateTime created = (DateTime)bean.Fields["created"];
            string description = (string)bean.Fields["description"];
            string summary = (string)bean.Fields["summary"];

            Issue issue = new Issue()
            {
                Id = bean.Id,
                Key = bean.Key,
                Description = FixMarkdown(description),
                Summary = summary,
                Created = created,
                Updated = updated,
                Status = new IssueStatus(status),
                Resolution = resolution == null ? "Unresolved" : resolution.Value<string>("name"),
                Assignee = new Person(assignee),
                Reporter = new Person(reporter),
                Type = new IssueType(issuetype),
                Priority = new IssueType(priority),
                Project = new Project(project),
            };

            //TODO: issuelinks, subtasks, CustomFields?

            issue.Attachments = new List<Attachment>();
            foreach (JToken attachmentField in attachment)
            {
                Attachment newAttachment = new Attachment(attachmentField);
                issue.Attachments.Add(newAttachment);
            }

            JArray commentList = comment?.Value<JArray>("comments");
            if (commentList != null)
            {
                issue.Comments = new List<Comment>();
                foreach (JToken commentField in commentList)
                {
                    Comment newComment = new Comment(commentField);
                    newComment.Body = FixMarkdown(newComment.Body);
                    issue.Comments.Add(newComment);
                }
            }

            return issue;
        }

        private static string FixMarkdown(string markdown)
        {
            // jira markdown is a piece of shit that doesn't adhere to any common specification
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "";
            }
            string fixedMarkdown = Regex.Replace(markdown, @"\[([^|]+)\|([^|]+)\]", match => "[" + match.Groups[1] + "](" + match.Groups[2] + ")");
            fixedMarkdown = Regex.Replace(fixedMarkdown, @"([#*]+) ", delegate (Match match)
            {
                // fix lists
                string m = match.Groups[1].Value;
                return string.Concat(Enumerable.Repeat("    ", m.Length - 1)) + (m[^1] == '#' ? "1." : "*") + " ";
            });
            fixedMarkdown = Regex.Replace(fixedMarkdown, @"\.h([123456]) ", delegate (Match match)
            {
                // fix headings
                int count = int.Parse(match.Groups[1].Value);
                return new string('#', count) + " ";
            });
            return fixedMarkdown;
        }

        public string ToFulltextDocument()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Join("\n", Key, Description, Summary, Assignee.DisplayName, Reporter.DisplayName));
            if (Comments != null)
            {
                foreach (Comment comment in Comments)
                {
                    sb.Append('\n');
                    sb.Append(string.Join("\n", comment.Author.DisplayName, comment.Body));
                }
            }
            return sb.ToString();
        }
    }

    public class Person
    {
        public Person() { }

        public Person(JObject input)
        {
            if (input == null) return;

            Key = input.Value<string>("key");
            Name = input.Value<string>("name");
            Email = input.Value<string>("emailAddress");
            DisplayName = input.Value<string>("displayName");
            AvatarUrl = input.Value<JObject>("avatarUrls").Value<string>("48x48");
        }

        public string Name { get; set; }

        public string Key { get; set; }

        public string Email { get; set; }

        public string AvatarUrl { get; set; }

        public string DisplayName { get; set; }
    }

    public class IssueType
    {
        public IssueType() { }

        public IssueType(JObject input)
        {
            if (input == null) return;

            Id = input.Value<string>("id");
            Name = input.Value<string>("name");
            IconUrl = input.Value<string>("iconUrl");
            IconId = input.Value<int>("avatarId");
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public int IconId { get; set; }
    }

    public class IssueStatus
    {
        public IssueStatus() { }

        public IssueStatus(JObject input)
        {
            if (input == null) return;

            Id = input.Value<string>("id");
            Name = input.Value<string>("name");
            Color = input.Value<JObject>("statusCategory").Value<string>("colorName");
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Color { get; set; }
    }

    public class Project
    {
        public Project() { }

        public Project(JObject input)
        {
            if (input != null)
            {
                Id = input.Value<string>("id");
                Name = input.Value<string>("name");
                Key = input.Value<string>("key");
                AvatarUrl = input.Value<JObject>("avatarUrls").Value<string>("48x48");
            }
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public string AvatarUrl { get; set; }
    }

    public class Comment
    {
        public Comment() { }

        public Comment(JToken input)
        {
            Id = input.Value<string>("id");
            Body = input.Value<string>("body");
            Created = input.Value<DateTime>("created");
            Author = new Person(input.Value<JObject>("author"));
        }

        public string Id { get; set; }

        public Person Author { get; set; }

        public string Body { get; set; }

        public DateTime Created { get; set; }
    }

    public class Attachment
    {
        public Attachment() { }

        public Attachment(JToken input)
        {
            if (input != null)
            {
                Id = input.Value<string>("id");
                FileName = input.Value<string>("filename");
                Content = input.Value<string>("content");
                Thumbnail = input.Value<string>("thumbnail");
                Size = input.Value<int>("size");
                Created = input.Value<DateTime>("created");
                Author = new Person(input.Value<JObject>("author"));
            }
        }

        public string Id { get; set; }

        public Person Author { get; set; }

        public string FileName { get; set; }

        public string Content { get; set; }

        public string Thumbnail { get; set; }

        public int Size { get; set; }

        public DateTime Created { get; set; }
    }
}
