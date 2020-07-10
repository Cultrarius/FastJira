using System;
using System.Collections.Generic;

namespace Fast_Jira.core
{
    public class Issue
    {
        /// <summary>Internal Jira ID</summary>
        public string ID { get; set; }

        /// <summary>Issue key as shown in the browser</summary>
        public string Key { get; set; }

        /// <summary>Issue description in markdown format</summary>
        public string Description { get; set; }

        /// <summary>Last known scrollbar position of the description text</summary>
        public int DescriptionScrollPosition { get; set; }

        /// <summary>Issue summary displayed on top as the heading</summary>
        public string Summary { get; set; }

        public string Resolution { get; set; }

        public DateTime Updated { get; set; }

        public DateTime Created { get; set; }

        public List<string> IssueLinks { get; } = new List<string>();

        public List<string> Subtasks { get; } = new List<string>();

        public List<Comment> Comments { get; } = new List<Comment>();

        public List<Attachment> Attachments { get; } = new List<Attachment>();

        public Person Assignee { get; set; }

        public Person Reporter { get; set; }

        public IssueType Type { get; set; }

        public IssueType Priority { get; set; }

        public IssueStatus Status { get; set; }

        public Project Project { get; set; }

        public Dictionary<string, string> CustomFields { get; } = new Dictionary<string, string>();
    }

    public class Person
    {
        public string Name { get; set; }

        public string Key { get; set; }

        public string Email { get; set; }

        // TODO: get icon
        //public string AvatarUrl { get; set; }

        public string DisplayName { get; set; }
    }

    public class IssueType
    {
        public string ID { get; set; }

        public string Name { get; set; }

        // TODO: get icon
        //public string IconUrl { get; set; }
    }

    public class IssueStatus
    {
        public string ID { get; set; }

        public string Name { get; set; }

        public string Color { get; set; }
    }

    public class Project
    {
        public string ID { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        // TODO: get icon
        //public string AvatarUrl { get; set; }
    }

    public class Comment
    {
        public string ID { get; set; }

        public Person Author { get; set; }

        public string Body { get; set; }

        public DateTime Created { get; set; }
    }

    public class Attachment
    {
        public string ID { get; set; }

        public Person Author { get; set; }

        public string FileName { get; set; }

        public string Content { get; set; }

        public string Thumbnail { get; set; }

        public int Size { get; set; }

        public DateTime Created { get; set; }
    }
}
