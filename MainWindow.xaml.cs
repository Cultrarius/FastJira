using Fast_Jira.core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Fast_Jira
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Issue DisplayedIssue;        

        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));

            ProgressBar.IsIndeterminate = false;

            SetExampleIssue();
            RefreshDisplayedIssue();
        }

        public void RefreshDisplayedIssue()
        {
            DetailsStatus.Text = DisplayedIssue?.Status?.Name;
            DetailsPriority.Text = DisplayedIssue?.Priority?.Name;
            DetailsResolution.Text = DisplayedIssue?.Resolution;
            DetailsType.Text = DisplayedIssue?.Type?.Name;
            DetailsAssignee.Text = DisplayedIssue?.Assignee?.DisplayName;
            DetailsReporter.Text = DisplayedIssue?.Reporter?.DisplayName;
            DetailsCreated.Text = DisplayedIssue?.Created == null ? "" : DisplayedIssue.Created.ToLocalTime().ToString();
            DetailsUpdated.Text = DisplayedIssue?.Updated == null ? "" : DisplayedIssue.Updated.ToLocalTime().ToString();

            SubtaskGroup.Visibility = DisplayedIssue?.Subtasks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            AttachmentsGroup.Visibility = DisplayedIssue?.Attachments.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            SummaryText.Text = DisplayedIssue?.Summary == null ? "- issue summary missing -" : DisplayedIssue.Key + " - " + DisplayedIssue.Summary;
            MarkdownViewer.Markdown = DisplayedIssue?.Description;
        }

        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            MessageBox.Show($"URL: {e.Parameter}");
        }

        private void ClickOnImage(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            MessageBox.Show($"Image: {e.Parameter}");
        }

        private void SetExampleIssue()
        {
            DisplayedIssue = new Issue
            {
                ID = "123",
                Key = "DXFAA-339",
                Description = "Test it here:\r\n [Link](https://some-link/abc)\r\n\r\n**Some bold stuff!**",
                Summary = "Test issue for all purposes :)",
                Created = DateTime.Now,
                Updated = DateTime.Now,
                Status = new IssueStatus
                {
                    Name = "TO DO"
                }
            };
        }
    }

    public class DetailEntry
    {
        public string Title { get; set; }
        public string Value { get; set; }
    }
}
